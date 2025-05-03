using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.EmoteMonitors;

public static class Monitoring
{
    private static readonly TimeSpan monitorCycleTimeout = TimeSpan.FromSeconds(300);
    private static readonly Dictionary<int, List<Emote>> Emotes = [];
    private static List<EmoteMonitorChannel> Channels = [];
    private static List<string> GlobalEmoteTokens = [];

    public static void Init() => Task.Run(Scheduler);

    private static async Task Scheduler()
    {
        while (true)
        {
            Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => !x.Deleted)];
            
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

    private static async Task<List<Emote>> UpdateEmotes(EmoteMonitorChannel channel)
    {
        try
        {
            if (!Emotes.TryGetValue(channel.ChannelID, out List<Emote>? oldEmotes))
            {
                Emotes.Add(channel.ChannelID, new List<Emote>());
                oldEmotes = new List<Emote>();
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
            
            List<Emote> removed = [.. oldEmotes.Except(loadedEmotes)];
            List<Emote> added = [.. loadedEmotes.Except(oldEmotes)];

            if (added.Count == 0 && removed.Count == 0) return oldEmotes;
            BotCore.Nlog.Info($"{channel.ChannelName} emotes updated !!! removed { removed.Count} added {added.Count}");
            
            string result = $"{channel.ChannelName} emotes ";
            if (removed.Count != 0) result += $"{removed.Count} removed 📤 : {string.Join(", ", removed)} ";
            if (added.Count != 0) result += $"{added.Count} added 📥 : {string.Join(", ", added)} ";

            result += string.Join(" ", channel.UpdateSubscribersUsers.Select(x => "@" + BotCore.DBContext.Users.FirstOrDefault(u => u.Id == x)?.Username));

            BotCore.OutQueuePerChannel[channel.OutputChannelName].Enqueue(new CommandResult(result, reply: false));
            return loadedEmotes;
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error($"failed to get emotes for {channel.ChannelName}");
            BotCore.Nlog.Error(e);
            Task.Run(Scheduler);
            return Emotes[channel.ID];
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
        }
    }
}
