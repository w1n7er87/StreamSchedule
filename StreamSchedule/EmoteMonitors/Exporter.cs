using System.Text;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using StreamSchedule.GraphQL;
using StreamSchedule.WebExport;
using StreamSchedule.WebExport.Conversions;
using StreamSchedule.WebExport.Templates;

namespace StreamSchedule.EmoteMonitors;

internal class Exporter
{
    private const int maxAttempts = 5;
    private int attempts = 0;

    internal async Task ExportEmotes(List<Emote> removed, List<Emote> added, int totalCount, EmoteMonitorChannel channel)
    {
        try
        {
            BotCore.Nlog.Info($"{channel.ChannelName} emotes updated ( {added.Count} added, {removed.Count} removed ) ");

            channel.ChannelName = (await BotCore.API.Helix.Users.GetUsersAsync([channel.ChannelID.ToString()])).Users.FirstOrDefault()?.Login ?? channel.ChannelName;

            StringBuilder html = new();
            StringBuilder chatResult = new($"{channel.ChannelName} emotes ");

            List<GraphQL.Data.Emote?> addedEmoteDetails = [.. await Task.WhenAll(added.Select(x => GraphQLClient.GetEmote(x.ID)))];

            if (removed.Count == totalCount && added.Count == totalCount)
            {
                string? id = removed.FirstOrDefault()?.ID ?? "";
                Emote oldEmote = addedEmoteDetails.FirstOrDefault(e => e?.ID == id);
                GraphQL.Data.Emote? newEmote = addedEmoteDetails.FirstOrDefault(e => e?.ID == id);
                
                string newPrefix = newEmote?.Prefix ?? "";
                string oldPrefix = string.Join("", oldEmote?.Token.Take((oldEmote?.Token.Length ?? 0) - newEmote?.Suffix?.Length ?? 0) ?? "");
                
                chatResult.Append($"prefix changed \"{oldPrefix}\" > \"{newPrefix}\" ");
                ExportToChat(channel, chatResult);
                return;
            }

            List<Emote> addedDetails = [.. addedEmoteDetails.Select(x => (Emote)x).OrderBy(x => x.Token)];

            if (removed.Count != 0)
            {
                chatResult.Append($"{removed.Count} removed 📤 : {string.Join(", ", removed)} ");
                html.AppendFormat(Templates.EmotesBlock, "Removed", string.Join("\n", removed.Select(Conversions.EmoteToHtml)));
            }

            html.Append(Templates.Divider);

            if (addedDetails.Count != 0)
            {
                chatResult .Append($"{addedDetails.Count} added 📥 : {string.Join(", ", addedDetails)} ");
                html.AppendFormat(Templates.EmotesBlock, "Added", string.Join("\n", addedDetails.Select(Conversions.EmoteToHtml)));
            }

            chatResult.Append($" {await ExportToWeb(channel, html)} ");
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

    private static void ExportToChat(EmoteMonitorChannel channelSettings, StringBuilder content)
    {
        content.AppendJoin(" ", channelSettings.UpdateSubscribersUsers.Select(x => "@" + BotCore.DBContext.Users.FirstOrDefault(u => u.Id == x)?.Username));
        BotCore.OutQueuePerChannel[channelSettings.OutputChannelName].Enqueue(new CommandResult(content, false));
    }

    private static async Task<string> ExportToWeb(EmoteMonitorChannel channelSettings, StringBuilder content)
    {
        string slug = ExportUtils.GetSlug(channelSettings.ChannelName);
        BotCore.PagesDB.PageContent.Add(new()
        {
            EmbeddedStyleName = Templates.EmoteUpdatesStyleName,
            EmbeddedStyleVersion = Templates.EmoteUpdatesStyleVersion,
            CreatedAt = DateTime.UtcNow,
            HtmlContent = content.ToString(),
            Title = channelSettings.ChannelName,
            Summary = string.Format(Templates.EmoteUpdatesSummary, channelSettings.ChannelName),
            Slug = slug
        });
        await BotCore.PagesDB.SaveChangesAsync();
        return ExportUtils.EmotesUrlBase + slug;
    }
}
