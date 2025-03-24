using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.EmoteMonitors;

public static class Monitoring
{
    private static readonly TimeSpan monitorCycleTimeout = TimeSpan.FromSeconds(30);

    private static Dictionary<int, List<Emote>> Emotes = [];
    private static List<EmoteMonitorChannel> Channels = [];
    private static List<string> GlobalEmoteTokens = [];

    public static void Init()
    {
        Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => x.Deleted == false).AsNoTracking()];

        foreach (EmoteMonitorChannel channel in Channels) Emotes.Add(channel.ChannelID, []);

        Scheduler();
    }

    private static async Task Scheduler()
    {
        while (true)
        {
            foreach (EmoteMonitorChannel channel in Channels)
            {
                Emotes[channel.ChannelID] = await GetEmotes(channel);
            }
            await UpdateGlobalEmotes();
            await Task.Delay(monitorCycleTimeout);
        }
    }

    private static async Task<List<Emote>> GetEmotes(EmoteMonitorChannel channel)
    {
        List<Emote> oldEmotes = Emotes[channel.ChannelID];
        try
        {
            List<Emote> loadedEmotes =
            [
                .. (await BotCore.API.Helix.Chat.GetChannelEmotesAsync(channel.ChannelID.ToString())).ChannelEmotes
                .Select(x => (Emote)x)
            ];

            if (oldEmotes.Count == 0)
            {
                BotCore.Nlog.Info($"first run for {channel.ChannelName} emote monitor");
                return loadedEmotes;
            }

            if (oldEmotes.Count != loadedEmotes.Count) BotCore.Nlog.Info($"{oldEmotes.Count} > {loadedEmotes.Count} > {channel.ChannelName} emotes updated !!! ");

            List<Emote> removed = [.. oldEmotes.Except(loadedEmotes)];
            List<Emote> added = [.. loadedEmotes.Except(oldEmotes)];

            if (added.Count == 0 && removed.Count == 0) return oldEmotes;

            string result = $"{channel.ChannelName} emotes ";
            if (removed.Count != 0) result += $"removed {removed.Count} 📤 : {string.Join(", ", removed)} ";
            if (added.Count != 0) result += $"added {added.Count} 📥 : {string.Join(", ", added)} ";

            result += string.Join(" @", channel.UpdateSubscribers);

            BotCore.SendLongMessage(channel.OutputChannelName, null, result);
            return loadedEmotes;
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error($"failed to get emotes for {channel.ChannelName}");
            BotCore.Nlog.Error(e);
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
            if (removed.Count != 0) response += $"{removed.Count} added: {string.Join(" ", removed)} ";
            if (added.Count != 0) response += $"{added.Count} removed: {string.Join(" ", added)} ";

            BotCore.SendLongMessage("w1n7er", null, response);
            BotCore.SendLongMessage("vedal987", null, response);

            GlobalEmoteTokens = newGlobalEmoteTokens;
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error("Failed to get global emotes");
            BotCore.Nlog.Error(e);
        }
    }

    public static void AddMonitor(EmoteMonitorChannel channel)
    {
        Channels.Add(channel);
        _ = Emotes.TryAdd(channel.ChannelID, []);
    }

    public static void RemoveMonitor(EmoteMonitorChannel channel)
    {
        Emotes.Remove(channel.ChannelID);
        Channels.Remove(channel);
    }
}
