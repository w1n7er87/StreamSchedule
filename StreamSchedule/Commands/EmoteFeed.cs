using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace StreamSchedule.Commands;

internal class EmoteFeed : Command
{
    internal override string Call => "emon";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "add channel to monitor emote changes in my chat.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["rm", "channel"];

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        CommandResult response = new();
        string text = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArs);
        string[] split = text.Split(' ');

        int userIDNumber = 0;
        bool idProvided = false;
        string targetUsername = message.sender.Username!;

        string targetOutputChannelName = message.sender.Privileges == Privileges.Uuh ? usedArs.TryGetValue("channel", out string? channelName) ? channelName.ToLower() : "w1n7er" : "w1n7er";

        if (!string.IsNullOrWhiteSpace(split[0]))
        {
            if (split[0].StartsWith('#'))
            {
                idProvided = int.TryParse(split[0].Replace("#", "").Replace("@", ""), out userIDNumber);
            }
            if (!idProvided) { targetUsername = split[0].Replace("#", "").Replace("@", ""); }
        }

        targetUsername = targetUsername.ToLower();

        try
        {
            if (usedArs.TryGetValue("rm", out _))
            {
                EmoteMonitorChannel? toDelete = BotCore.DBContext.EmoteMonitorChannels.SingleOrDefault(x => x.ChannelName == targetUsername);
                if (toDelete is null) return Utils.Responses.Fail + $"not monitoring {targetUsername}";

                if (message.sender.Privileges == Privileges.Uuh)
                {
                    toDelete.Deleted = true;
                    await BotCore.DBContext.SaveChangesAsync();
                    Scheduling.RemoveEmoteMonitor(toDelete);
                    return Utils.Responses.Ok + $"removed emote monitor for {toDelete.ChannelName}";
                }
                else
                {
                    if (toDelete.UpdateSubscribers.Contains(message.sender.Username!)) return Utils.Responses.Ok + "removed you from ping list";
                    RestartMonitor(toDelete);
                    return Utils.Responses.Fail + $"you are not in the ping list for {toDelete.ChannelName}";
                }
            }

            GetUsersResponse? a = idProvided
                ? await BotCore.API.Helix.Users.GetUsersAsync(ids: [userIDNumber.ToString()])
                : await BotCore.API.Helix.Users.GetUsersAsync(logins: [targetUsername]);
            TwitchLib.Api.Helix.Models.Users.GetUsers.User u = a.Users[0];

            if (u.BroadcasterType is not ("partner" or "affiliate")) return Utils.Responses.Surprise + "user is not a partner or affiliate";

            EmoteMonitorChannel emc = new()
            {
                ChannelID = int.Parse(u.Id),
                ChannelName = u.Login,
                OutputChannelName = targetOutputChannelName,
                UpdateSubscribers = [message.sender.Username!]
            };

            EmoteMonitorChannel? existing = BotCore.DBContext.EmoteMonitorChannels.SingleOrDefault(x => x.ChannelID == emc.ChannelID);
            if (existing is not null)
            {
                CommandResult res = Utils.Responses.Ok + $"already monitoring {emc.ChannelName}";
                if (existing.Deleted) { res += " (restored)"; existing.Deleted = false; }
                if (existing.UpdateSubscribers.Contains(message.sender.Username!))
                {
                    res += " you are in the ping list.";
                }
                else
                {
                    res += " added you to the ping list.";
                    existing.UpdateSubscribers.Add(message.sender.Username!);
                }

                RestartMonitor(existing);
                await BotCore.DBContext.SaveChangesAsync();
                return res;
            }

            BotCore.DBContext.EmoteMonitorChannels.Add(emc);
            Scheduling.StartNewChannelMonitorJob(emc);
            await BotCore.DBContext.SaveChangesAsync();
            return Utils.Responses.Ok + $"added emote monitor for {emc.ChannelName} in {emc.OutputChannelName}";
        }
        catch (Exception ex)
        {
            BotCore.Nlog.Error(ex);
            return Utils.Responses.Fail;
        }
    }

    private static void RestartMonitor(EmoteMonitorChannel emc)
    {
        try
        {
            Scheduling.RemoveEmoteMonitor(emc);
        }
        catch (Exception ex)
        {
            BotCore.Nlog.Warn("error while trying to remove emote monitor:", ex);
        }

        Scheduling.StartNewChannelMonitorJob(emc);
    }
}
