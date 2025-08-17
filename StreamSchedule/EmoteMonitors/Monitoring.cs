using System.Text;
using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using StreamSchedule.Export;
using StreamSchedule.Export.Conversions;
using StreamSchedule.Export.Data;
using StreamSchedule.Export.Templates;
using StreamSchedule.GraphQL;

namespace StreamSchedule.EmoteMonitors;

public static class Monitoring
{
    private static readonly TimeSpan monitorCycleTimeout = TimeSpan.FromSeconds(180);
    private static readonly Dictionary<int, List<Emote>> Emotes = [];
    public static List<EmoteMonitorChannel> Channels {private get; set;} = [];
    private static List<string> GlobalEmoteTokens = [];

    public static bool Start => true;
    
    static Monitoring()
    {
        Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => !x.Deleted).AsNoTracking()];
        Task.Run(Scheduler);
    }

    private static async Task Scheduler()
    {
        try
        {
            while (true)
            {
                int channelCount = 0;

                foreach (EmoteMonitorChannel channel in Channels)
                {
                    channelCount++;
                    Emotes[channel.ChannelID] = await UpdateEmotes(channel);
                }

                BotCore.Nlog.Info($"Emon cycle {channelCount} channels");

                await UpdateGlobalEmotes();
                await Task.Delay(monitorCycleTimeout);
            }
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error($"emon died lol");
            BotCore.Nlog.Error(e);
            await Task.Delay(monitorCycleTimeout);
            if (e is TwitchLib.Api.Core.Exceptions.HttpResponseException) return;
            Task.Run(Scheduler);
        }
    }

    private static async Task<List<Emote>> UpdateEmotes(EmoteMonitorChannel channel)
    {
        try
        {
            if (!Emotes.TryGetValue(channel.ChannelID, out List<Emote>? oldEmotes))
            {
                Emotes.Add(channel.ChannelID, []);
                oldEmotes = [];
            }
            
            List<Emote> loadedEmotes =
            [
                .. (await BotCore.API.Helix.Chat.GetChannelEmotesAsync(channel.ChannelID.ToString())).ChannelEmotes
                .Select(x => (Emote)x)
            ];

            if (oldEmotes.Count == 0)
            {
                BotCore.Nlog.Info($"first run for {channel.ChannelName} emote monitor, {channel.UpdateSubscribersUsers.Count} subs");
                return loadedEmotes;
            }
            
            List<Emote> removed = [.. oldEmotes.Except(loadedEmotes).OrderBy(x => x.Token)];
            List<Emote> added = [.. loadedEmotes.Except(oldEmotes).OrderBy(x => x.Token)];

            if (added.Count == 0 && removed.Count == 0) return oldEmotes;
            
            BotCore.Nlog.Info($"{channel.ChannelName} emotes updated !!! removed { removed.Count} added {added.Count}");
            
            StringBuilder html = new();
            bool export = false;
            
            string actualUsername =
                (await BotCore.API.Helix.Users.GetUsersAsync(ids: [channel.ChannelID.ToString()])).Users
                .FirstOrDefault()?.Login ?? channel.ChannelName;
            
            string result = $"{actualUsername} emotes ";
            
            if (removed.Count == loadedEmotes.Count && oldEmotes.Count == added.Count)
            {
                GraphQL.Data.Emote? newEmote = await GraphQLClient.GetEmote(loadedEmotes[0].ID);
                string oldPrefix = oldEmotes[0].Token?[..^(newEmote?.Suffix?.Length ?? 0)] ?? "";
                string newPrefix = newEmote?.Token?[..^(newEmote.Suffix?.Length ?? 0)] ?? "";
                result += $"prefix changed \"{oldPrefix}\" > \"{newPrefix}\" ";
            }
            else
            {
                if (removed.Count != 0)
                {
                    export = true;
                    result += $"{removed.Count} removed 📤 : {string.Join(", ", removed)} ";
                    html.Append(string.Format(Templates.EmotesBlock, "Removed",
                        string.Join("\n", removed.Select(Conversions.EmoteToHtml))));
                }

                html.Append(Templates.Divider);

                if (added.Count != 0)
                {
                    export = true;
                    result += $"{added.Count} added 📥 : {string.Join(", ", added)} ";
                    html.Append(string.Format(Templates.EmotesBlock, "Added",
                        string.Join("\n", added.Select(Conversions.EmoteToHtml))));
                }
            }

            string exportLink = "";
            if(export){
                string slug = ExportUtils.GetSlug(channel.ChannelName);
                BotCore.PagesDB.PageContent.Add(new Content()
                {
                    EmbeddedStyleName = Templates.EmoteUpdatesStyleName,
                    EmbeddedStyleVersion = Templates.EmoteUpdatesStyleVersion,
                    CreatedAt = DateTime.UtcNow,
                    HtmlContent = html.ToString(),
                    Title = channel.ChannelName,
                    Summary = string.Format(Templates.EmoteUpdatesSummary, actualUsername),
                    Slug = slug
                });
                await BotCore.PagesDB.SaveChangesAsync();
                exportLink = ExportUtils.EmotesUrlBase + slug;
                BotCore.Nlog.Info(exportLink);
            }

            result += $" {exportLink} ";
            result += string.Join(" ", channel.UpdateSubscribersUsers.Select(x => "@" + BotCore.DBContext.Users.FirstOrDefault(u => u.Id == x)?.Username));
            BotCore.OutQueuePerChannel[channel.OutputChannelName].Enqueue(new CommandResult(result, reply: false));
            
            return loadedEmotes;
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error($"failed to get emotes for {channel.ChannelName}");
            BotCore.Nlog.Error(e);
            Task.Run(Scheduler);
            throw;
        }
    }

    private static async Task UpdateGlobalEmotes()
    {
        try
        {
            List<string> newGlobalEmoteTokens = [.. (await BotCore.API.Helix.Chat.GetGlobalEmotesAsync()).GlobalEmotes.Select(x => x.Name)];
            if (GlobalEmoteTokens.Count == 0)
            {
                BotCore.Nlog.Info("first run for Twitch Global emotes");
                GlobalEmoteTokens = newGlobalEmoteTokens;
            }

            List<string> removed = [.. GlobalEmoteTokens.Except(newGlobalEmoteTokens)];
            List<string> added = [.. newGlobalEmoteTokens.Except(GlobalEmoteTokens)];

            if (added.Count == 0 && removed.Count == 0) return;

            string response = "Twitch Emotes ";
            if (removed.Count != 0) response += $"{removed.Count} removed: {string.Join(" ", removed)} ";
            if (added.Count != 0) response += $"{added.Count} added: {string.Join(" ", added)} ";

            BotCore.OutQueuePerChannel["w1n7er"].Enqueue(new CommandResult(response, reply: false));
            BotCore.OutQueuePerChannel["vedal987"].Enqueue(new CommandResult(response, reply: false));

            GlobalEmoteTokens = newGlobalEmoteTokens;
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error("Failed to get global emotes");
            BotCore.Nlog.Error(e);
            Task.Run(Scheduler);
            throw;
        }
    }
}
