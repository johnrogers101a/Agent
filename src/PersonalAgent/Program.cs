using AgentFramework.Extensions;
using AgentFramework.Configuration;
using System.Runtime.CompilerServices;

// Force loading of tool assemblies before discovery runs
// RuntimeHelpers.RunClassConstructor forces the type to be fully loaded
RuntimeHelpers.RunClassConstructor(typeof(Weather.Tools.GetWeatherByZip).TypeHandle);
RuntimeHelpers.RunClassConstructor(typeof(Gmail.Tools.GetMail).TypeHandle);

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureAgent();

// Register tool dependencies
builder.Services.AddSingleton<Gmail.AuthService>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var settings = sp.GetRequiredService<AppSettings>();
    return new Gmail.AuthService(
        http,
        settings.Clients.Gmail.ClientId,
        settings.Clients.Gmail.ClientSecret);
});

var app = builder.Build();
app.UseAgents();
app.Run();