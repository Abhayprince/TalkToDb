using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using System.ClientModel;
using TalkToDb.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var aiApiKey = builder.Configuration.GetValue<string>("OpenAi:ApiKey");
var apiKeyCreds = new ApiKeyCredential(aiApiKey);

IChatClient baseChatClient = new OpenAIClient(apiKeyCreds)
                            .GetChatClient("gpt-4o-mini")
                            .AsIChatClient();

IChatClient chatClient = new ChatClientBuilder(baseChatClient).UseFunctionInvocation().Build();

builder.Services.AddSingleton(chatClient);

var app = builder.Build();

var clientOptions = new StdioClientTransportOptions
{
    Name = "TalkToDb MCP Server",
    Command = "dotnet",
    Arguments = ["run", "--project", "../TalkToDb.MCPServer/TalkToDb.MCPServer.csproj"]
};

var clientTransport = new StdioClientTransport(clientOptions);
await using var mcpClient = await McpClient.CreateAsync(clientTransport);

//var tools = await mcpClient.ListToolsAsync();

//var result = await mcpClient.CallToolAsync("List-Db-Table-Schema");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//app.MapGet("/abc", async (IChatClient chatClient) =>
//{
//    var msg = new ChatMessage(ChatRole.User, "What is 2 plus two");
//    var response = await chatClient.GetResponseAsync(msg);

//    return response.Text;
//});

app.MapAppEndpoints(mcpClient); 

app.Run();
