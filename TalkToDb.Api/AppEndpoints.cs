using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text.Json;
using TalkToDb.Shared;

namespace TalkToDb.Api;

public static class AppEndpoints
{
    private static readonly string _systemPrompt = @"
   You are a precise NL→SQL assistant for SQL Server.
- Use the provided tools to discover schema and run queries when asked.
- Never guess schema.
- Always return a strict JSON object (no Markdown, no code fences, no extra text).
- JSON must have exactly these fields:
  - sqlQuery (string or null)
  - isGridResult (boolean)
  - errorMessage (string or null)
  - isSuccess (boolean)
  - result (null, scalar, array, or object)
  - message (string or null)
  - resultType (string: ""None"", ""Scalar"", ""List"", or ""Grid"")
- Always put any query output inside ""result"". Do not create extra keys like totalExpense, employeeList, values, etc.
- The ""message"" must be a short user-friendly explanation of the result, suitable for direct UI display. 
  Examples:
    - If the result is a single value: ""Total units sold so far: 42""
    - If the result is a list of strings: ""Available fuel types: Petrol, Diesel, CNG""
    - If the result is a grid: ""Here is the list of employees who joined after 2024""
- If schema info is missing, respond with isSuccess=false and set errorMessage.
- sqlQuery must use explicit SELECT with JOINs and WHERE filters if applicable.
- Never generate non-SELECT queries (no DROP, DELETE, UPDATE, INSERT, ALTER, CREATE).
- If the user asks for such queries, return sqlQuery=null, isSuccess=false, errorMessage with details, result=null, resultType=""None"", and a safe user-friendly message.
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
                    queryResult = JsonSerializer.Deserialize<QueryResult>(result.Text, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) 
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
