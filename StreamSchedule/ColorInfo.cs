﻿using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace StreamSchedule;

internal static class ColorInfo
{
    private class Name
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    private class ColorsResponse
    {
        [JsonPropertyName("name")]
        public Name Name { get; set; }
    }

    public static async Task<string> GetColor(string colorHex)
    {
        colorHex = colorHex.Replace("#", "");

        int r = Convert.ToInt32(colorHex[..2], 16);
        int g = Convert.ToInt32(colorHex[2..4], 16);
        int b = Convert.ToInt32(colorHex[4..6], 16);

        string rgb = $" ({r}R {g}G {b}B) ";

        try
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync($"https://www.thecolorapi.com/id?hex={colorHex}&format=json");
            var cc = JsonConvert.DeserializeObject<ColorsResponse>(response);

            return cc?.Name.Value + rgb;
        }
        catch
        {
            return rgb;
        }
    }
}
