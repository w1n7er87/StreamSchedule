using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.EmoteMonitors;

public static class Monitoring
{
    private static readonly TimeSpan monitorCycleTimeout = TimeSpan.FromSeconds(180);
    private static readonly Dictionary<int, List<Emote>> Emotes = [];
    public static List<EmoteMonitorChannel> Channels {private get; set;}
    private static List<string> GlobalEmoteTokens = [];
    public static bool Start => true;
    
    static Monitoring()
    {
        Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => !x.Deleted)];
        Task.Run(Scheduler);
    }

    private static async Task Scheduler()
    {
        try
        {
            while (true)
            {
                List<Task<(int, List<Emote>)>> emonTasks = [.. Channels.Select(UpdateEmotes)];

                foreach ((int, List<Emote>) result in await Task.WhenAll(emonTasks))
                    Emotes[result.Item1] = result.Item2;

                BotCore.Nlog.Info($"Emon cycle {Channels.Count} channels");

                await UpdateGlobalEmotes();
                await Task.Delay(monitorCycleTimeout);
            }
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error($"emon died lol");
            await Task.Delay(monitorCycleTimeout);
            Task.Run(Scheduler);
        }
    }

    private static async Task<(int ,List<Emote>)> UpdateEmotes(EmoteMonitorChannel channel)
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
                return (channel.ChannelID, loadedEmotes);
            }
            
            List<Emote> removed = [.. oldEmotes.Except(loadedEmotes)];
            List<Emote> added = [.. loadedEmotes.Except(oldEmotes)];

            if (added.Count == 0 && removed.Count == 0) return (channel.ChannelID, oldEmotes);
            
            await new Exporter().ExportEmotes(removed, added, oldEmotes.Count, channel);
            
            return (channel.ChannelID, loadedEmotes);
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error($"emon for {channel.ChannelName} died" + e);
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
            BotCore.Nlog.Error("Failed to get global emotes" + e);
            throw;
        }
    }
}
