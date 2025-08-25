using System.Text;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using StreamSchedule.GraphQL;
using StreamSchedule.WebExport;
using StreamSchedule.WebExport.Conversions;
using StreamSchedule.WebExport.Data;
using StreamSchedule.WebExport.Templates;

namespace StreamSchedule.EmoteMonitors;

internal class Exporter
{
    private const int maxAttempts = 5;
    private int attempts = 0;

    internal async Task ExportEmotes(List<Emote> removed, List<Emote> added, int totalCount,
        EmoteMonitorChannel channel
    )
    {
        try
        {
            BotCore.Nlog.Info($"{channel.ChannelName} emotes updated ( {added.Count} added, {removed.Count} removed ) ");

            channel.ChannelName = (await BotCore.API.Helix.Users.GetUsersAsync([channel.ChannelID.ToString()])).Users.FirstOrDefault()?.Login ?? channel.ChannelName;

            StringBuilder html = new();
            string chatResult = $"{channel.ChannelName} emotes ";

            List<GraphQL.Data.Emote?> removedEmoteDetails = [.. await Task.WhenAll(removed.Select(x => GraphQLClient.GetEmote(x.ID)))];
            List<GraphQL.Data.Emote?> addedEmoteDetails = [.. await Task.WhenAll(added.Select(x => GraphQLClient.GetEmote(x.ID)))];

            if (removed.Count == totalCount && added.Count == totalCount)
            {
                string oldPrefix = removedEmoteDetails[0]?.Prefix ?? "";
                string newPrefix = addedEmoteDetails.FirstOrDefault(x => x?.ID == removedEmoteDetails[0]?.ID)?.Prefix ?? "";

                chatResult += $"prefix changed \"{oldPrefix}\" > \"{newPrefix}\" ";
                ExportToChat(channel, chatResult);
                return;
            }

            List<Emote> removedDetails = [.. removedEmoteDetails.Select(x => (Emote)x).OrderBy(x => x.Token)];
            List<Emote> addedDetails = [.. addedEmoteDetails.Select(x => (Emote)x).OrderBy(x => x.Token)];

            if (removedDetails.Count != 0)
            {
                chatResult += $"{removedDetails.Count} removed 📤 : {string.Join(", ", removedDetails)} ";
                html.Append(string.Format(Templates.EmotesBlock, "Removed", string.Join("\n", removedDetails.Select(Conversions.EmoteToHtml))));
            }

            html.Append(Templates.Divider);

            if (addedDetails.Count != 0)
            {
                chatResult += $"{addedDetails.Count} added 📥 : {string.Join(", ", addedDetails)} ";
                html.Append(string.Format(Templates.EmotesBlock, "Added", string.Join("\n", addedDetails.Select(Conversions.EmoteToHtml))));
            }

            chatResult += $" {await ExportToWeb(channel, html.ToString())} ";
            ExportToChat(channel, chatResult);
        }
        catch (Exception e)
        {
            if (attempts > maxAttempts)
            {
                BotCore.Nlog.Error($"emon export for {channel.ChannelName} failed ");
                return;
            }

            BotCore.Nlog.Error($"emon export for {channel.ChannelName} failed {attempts}" + e);

            await Task.Delay(25 + attempts * 500);
            await ExportEmotes(removed, added, totalCount, channel);
            attempts++;
        }
    }

    private static void ExportToChat(EmoteMonitorChannel channelSettings, string content)
    {
        content += string.Join(" ", channelSettings.UpdateSubscribersUsers.Select(x => "@" + BotCore.DBContext.Users.FirstOrDefault(u => u.Id == x)?.Username));
        BotCore.OutQueuePerChannel[channelSettings.OutputChannelName].Enqueue(new CommandResult(content, false));
    }

    private static async Task<string> ExportToWeb(EmoteMonitorChannel channelSettings, string content)
    {
        string slug = ExportUtils.GetSlug(channelSettings.ChannelName);
        BotCore.PagesDB.PageContent.Add(new()
        {
            EmbeddedStyleName = Templates.EmoteUpdatesStyleName,
            EmbeddedStyleVersion = Templates.EmoteUpdatesStyleVersion,
            CreatedAt = DateTime.UtcNow,
            HtmlContent = content,
            Title = channelSettings.ChannelName,
            Summary = string.Format(Templates.EmoteUpdatesSummary, channelSettings.ChannelName),
            Slug = slug
        });
        await BotCore.PagesDB.SaveChangesAsync();
        return ExportUtils.EmotesUrlBase + slug;
    }
}
