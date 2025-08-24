using Newtonsoft.Json;

namespace StreamSchedule;

internal static class ColorInfo
{
    private static readonly HttpClient _colorClient = new HttpClient(new SocketsHttpHandler()
        { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });

    internal class Name
    {
        [JsonProperty("value")]
        internal string? Value { get; set; }
    }

    internal class ColorsResponse
    {
        [JsonProperty("name")]
        internal Name? Name { get; set; }
    }

    public static async Task<string> GetColor(string colorHex)
    {
        colorHex = colorHex.Replace("#", "");
        string rgb = $" ({Convert.ToInt32(colorHex[..2], 16)}R {Convert.ToInt32(colorHex[2..4], 16)}G {Convert.ToInt32(colorHex[4..6], 16)}B) ";
        try
        {
            string response = await _colorClient.GetStringAsync($"https://www.thecolorapi.com/id?hex={colorHex}&format=json");
            return JsonConvert.DeserializeObject<ColorsResponse>(response)?.Name?.Value + rgb;
        }
        catch
        {
            return rgb;
        }
    }
}
