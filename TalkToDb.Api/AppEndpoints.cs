using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text.Json;
using TalkToDb.Shared;

namespace TalkToDb.Api;

public static class AppEndpoints
{
    private static readonly string _systemPrompt = @"
           You are a precise NL→SQL assistant for SQL Server.
        - Never guess schema. Use provided schema discovery tools if available.
        - Always return EXACTLY this JSON object (no Markdown, no code fences, no extra text):

        {
          ""sqlQuery"": string | null,
          ""errorMessage"": string | null,
          ""isSuccess"": boolean,
          ""result"": null | string | number | object | array,
          ""message"": string | null,
          ""resultType"": ""None"" | ""Scalar"" | ""List"" | ""Grid""
        }

        Rules:
        1. `sqlQuery` must always be a SELECT (with JOINs/WHERE if needed). Never return INSERT/UPDATE/DELETE/DDL. 
           - If asked for those, return:
             { ""sqlQuery"": null, ""errorMessage"": ""Only SELECT queries are allowed."", ""isSuccess"": false, ""result"": null, ""message"": ""Only SELECT queries are allowed."", ""resultType"": ""None"" }

        2. `resultType` definitions:
           - ""None"" → no result
           - ""Scalar"" → single value (result = number/string)
           - ""List"" → 1D list of values or objects
           - ""Grid"" → tabular result (result MUST be { ""columns"": string[], ""rows"": object[] })

        3. `message` → short, user-friendly explanation for UI display.
           - If `resultType` = ""Scalar"", the `message` MUST explicitly include the scalar value.
             Example: if result=4200 and query was ""How much we spent in 2025?"", message should be:
             ""You have spent 4200 in 2025.""

        4. `errorMessage` → technical description for debugging.

        5. On schema missing or unknown tables/columns:
           - Return: { ""sqlQuery"": null, ""errorMessage"": ""Schema information missing."", ""isSuccess"": false, ""result"": null, ""message"": ""Schema information is missing."", ""resultType"": ""None"" }

    ";
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder app, McpClient mcpClient)
    {
        app.MapGet("/ask", async (string q, IChatClient chatClient) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest("Quesion is required");

            var tools = await mcpClient.ListToolsAsync();

            var chatOptions = new ChatOptions
            {
                Tools = [.. tools]
            };

            var system = new ChatMessage(ChatRole.System, _systemPrompt);
            var question = new ChatMessage(ChatRole.User, q);

            var result = await chatClient.GetResponseAsync([system, question], chatOptions);

            QueryResult queryResult;

            if(string.IsNullOrWhiteSpace(result.Text))
            {
                queryResult = new()
                {
                    IsSuccess = false,
                    ErrorMessage = "Could not get any response"
                };
            }
            else
            {
                try
                {
                    queryResult = JsonSerializer.Deserialize<QueryResult>(result.Text, Utils.JsonSerializerOptions) 
                    ?? new()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid response"
                    };
                }
                catch (Exception ex)
                {
                    queryResult = new()
                    {
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                }
            }

            return Results.Ok(queryResult);
        });
        return app;
    }
}
