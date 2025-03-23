using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.EmoteMonitors;

public static class Monitoring
{
    private static readonly TimeSpan monitorCycleTimeout = TimeSpan.FromSeconds(30);

    private static Dictionary<int, List<Emote>> Emotes = [];
    private static List<EmoteMonitorChannel> Channels = [];

    public static void Init()
    {
        Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => x.Deleted == false).AsNoTracking()];

        foreach (EmoteMonitorChannel channel in Channels)
        {
            Emotes.Add(channel.ChannelID, []);
        }

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

            BotCore.Nlog.Info($"{oldEmotes.Count} > {loadedEmotes.Count} > {channel.ChannelName}");
            if (oldEmotes.Count != loadedEmotes.Count) BotCore.Nlog.Info("!!!");

            List<Emote> removed = [.. oldEmotes.Except(loadedEmotes)];
            List<Emote> added = [.. loadedEmotes.Except(oldEmotes)];

            if (added.Count == 0 && removed.Count == 0) return oldEmotes;

            string result = $"{channel.ChannelName} Emotes ";
            if (removed.Count != 0) result += $"removed 📤 : {string.Join(", ", removed)} ";
            if (added.Count != 0) result += $"added 📥 : {string.Join(", ", added)} ";

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
