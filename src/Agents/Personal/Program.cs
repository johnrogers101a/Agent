// Copyright (c) 4JS. All rights reserved.

using AgentFramework.Configuration;
using AgentFramework.Extensions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

// Load settings to determine which mode to run in
var settings = AppSettings.LoadConfiguration();

if (settings.Provider.FunctionMode)
{
    // Azure Functions mode - use durable agents
    var builder = FunctionsApplication.CreateBuilder(args);
    builder.ConfigureDurableAgent();
    using var app = builder.Build();
    app.Run();
}
else
{
    // WebApplication mode - use DevUI or API endpoints
    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigureAgent();
    var app = builder.Build();
    app.UseAgents();
    app.Run();
}
