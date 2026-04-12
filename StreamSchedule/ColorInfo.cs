using Newtonsoft.Json;
using StreamSchedule.Data;

namespace StreamSchedule;

internal static class ColorInfo
{
    private static readonly HttpClient _colorClient = new(new SocketsHttpHandler() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });
    
    public static async Task<string> GetColorName(string colorHex)
    {
        colorHex = colorHex.Replace("#", "");
        try
        {
            string response = await _colorClient.GetStringAsync($"https://www.thecolorapi.com/id?hex={colorHex}&format=json");
            return JsonConvert.DeserializeObject<ColorsResponse>(response)?.Name?.Value ?? "";
        }
        catch
        {
            return "";
        }
    }
    
    public static async Task<Color> GetColorName(Color color)
    {
        string name;
        try
        {
            string response = await _colorClient.GetStringAsync($"https://www.thecolorapi.com/id?hex={color.ToHex()}&format=json");
            name = JsonConvert.DeserializeObject<ColorsResponse>(response)?.Name?.Value ?? "";
        }
        catch(Exception e)
        {
            BotCore.Nlog.Error(e);
            name = "";
        }
        
        return color with {name = name};
    }
}



internal class Name
{
    [JsonProperty("value")] internal string? Value { get; set; }
    [JsonProperty("distance")] internal float Distance { get; set; }
}

internal class ColorsResponse
{
    [JsonProperty("name")] internal Name? Name { get; set; }
}
