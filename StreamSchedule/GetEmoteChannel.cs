﻿namespace StreamSchedule;

internal static class GetEmoteChannel
{
    public static async Task<string> GetEmoteChannelByEmoteID(string emoteID)
    {
        using var client = new HttpClient(
            new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                UseDefaultCredentials = true,
                MaxConnectionsPerServer = 1
            });

        client.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            var response = await client.GetAsync($"https://twitch-tools.rootonline.de/emotes_content_id.php?ttv_emote={emoteID}");

            string result = "";

            if (response.StatusCode >= System.Net.HttpStatusCode.Redirect)
            {
                result = response.Headers.Location?.Segments[^1] ?? "";
            }

            return result;
        }
        catch (Exception ex)
        {
            BotCore.Nlog.Error(ex.ToString());
            return " " + Utils.Responses.Surprise.ToString();
        }
    }
}
