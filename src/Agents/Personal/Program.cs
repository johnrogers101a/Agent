using AgentFramework.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureAgent();
var app = builder.Build();
app.UseAgents();
app.Run();
