using Agent.Configuration;
using Agent.Core;

var appSettings = AppSettings.LoadConfiguration();
var agents = AgentFactory.Load(appSettings);
await agents.First().Value.RunAsync("Get the latest emails from my Gmail inbox and summarize them for me.");