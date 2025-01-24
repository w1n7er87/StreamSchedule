using System.Diagnostics;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace StreamSchedule.Commands;

public class EmoteFeed : Command
{
    internal override string Call => "emon";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "add channel to monitor emote changes in my chat.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int) Cooldowns.Medium);
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

        if (usedArs.TryGetValue("rm", out _))
        {
            EmoteMonitorChannel? toDelete = BotCore.DBContext.EmoteMonitorChannels.SingleOrDefault(x => x.ChannelName == targetUsername);
            if (toDelete is null) return Utils.Responses.Fail;
            BotCore.DBContext.Remove(toDelete);
            await BotCore.DBContext.SaveChangesAsync();
            return Utils.Responses.Ok + $"removed emote monitor for {targetUsername}";
        }

        try
        {
            GetUsersResponse? a = idProvided
                ? await BotCore.API.Helix.Users.GetUsersAsync(ids: [userIDNumber.ToString()])
                : await BotCore.API.Helix.Users.GetUsersAsync(logins: [targetUsername]);
            TwitchLib.Api.Helix.Models.Users.GetUsers.User u = a.Users[0];

            if (u.BroadcasterType is not "partner" or "affiliate") return Utils.Responses.Surprise + "user is not a partner or affiliate";

            EmoteMonitorChannel emc = new()
            {
                ChannelID = int.Parse(u.Id),
                ChannelName = u.Login,
                OutputChannelName = targetOutputChannelName,
            };

            if (BotCore.DBContext.EmoteMonitorChannels.SingleOrDefault(x => x.ChannelID == emc.ChannelID) is not null) return Utils.Responses.Ok + $"already monitoring {emc.ChannelName}";
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
}
