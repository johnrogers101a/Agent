using Microsoft.Extensions.AI;

namespace Agent.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, AITool> _tools = new()
    {
        ["GetWeatherByZip"] = WeatherTool.CreateGetWeatherByZip(),
        ["GetWeatherByCityState"] = WeatherTool.CreateGetWeatherByCityState(),
        ["GetMail"] = GmailTool.CreateGetMail(),
        ["SearchMail"] = GmailTool.CreateSearchMail(),
        ["GetMailContents"] = GmailTool.CreateGetMailContents()
    };

    public IList<AITool> GetTools(IEnumerable<string> toolNames)
    {
        return toolNames
            .Where(name => _tools.ContainsKey(name))
            .Select(name => _tools[name])
            .ToList();
    }
}
