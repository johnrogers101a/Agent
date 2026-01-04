using AgentFramework.Tools;
using GoogleTools.Tools;

namespace PersonalAgent.Tools;

public class ToolRegistry : AgentFramework.Tools.ToolRegistry
{
    public ToolRegistry()
    {
        // Register Weather tools
        Register("GetWeatherByZip", WeatherTool.CreateGetWeatherByZip());
        Register("GetWeatherByCityState", WeatherTool.CreateGetWeatherByCityState());
        
        // Register Gmail tools
        Register("GetMail", GmailTool.CreateGetMail());
        Register("SearchMail", GmailTool.CreateSearchMail());
        Register("GetMailContents", GmailTool.CreateGetMailContents());
    }
}
