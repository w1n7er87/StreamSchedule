using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using StreamSchedule.EmoteMonitors;
using StreamSchedule.GraphQL;
using User = StreamSchedule.GraphQL.Data.User;

namespace StreamSchedule.Commands;

internal class EmoteFeed : Command
{
    public override string Call => "emon";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => "add channel to monitor emote changes in my chat";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["rm", "channel", "rmsub"];
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> usedArs);
        string[] split = text.Split(' ');

        int userIDNumber = 0;
        bool idProvided = false;
        string targetUsername = message.Sender.Username!;

        string targetOutputChannelName = message.Sender.Privileges == Privileges.Uuh
            ? usedArs.TryGetValue("channel", out string? channelName) ? channelName.ToLower() : "w1n7er"
            : "w1n7er";

        if (!string.IsNullOrWhiteSpace(split[0]))
        {
            if (split[0].StartsWith('#')) idProvided = int.TryParse(split[0].Replace("#", "").Replace("@", ""), out userIDNumber);
            if (!idProvided) targetUsername = split[0].Replace("#", "").Replace("@", "");
        }

        targetUsername = targetUsername.ToLower();

        try
        {
            if (usedArs.TryGetValue("rm", out _))
            {
                EmoteMonitorChannel? toDelete = BotCore.DBContext.EmoteMonitorChannels.SingleOrDefault(x => x.ChannelName == targetUsername);
                if (toDelete is null) return Utils.Responses.Fail + $"not monitoring {targetUsername}";

                if (message.Sender.Privileges == Privileges.Uuh)
                {
                    toDelete.Deleted = true;
                    await BotCore.DBContext.SaveChangesAsync();
                    Monitoring.Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => !x.Deleted).AsNoTracking()];
                    return Utils.Responses.Ok + $"removed emote monitor for {toDelete.ChannelName}";
                }

                if (!toDelete.UpdateSubscribersUsers.Contains(message.Sender.Id))
                    return Utils.Responses.Fail + $"you are not in the ping list for {toDelete.ChannelName}";

                toDelete.UpdateSubscribersUsers.Remove(message.Sender.Id);
                await BotCore.DBContext.SaveChangesAsync();
                Monitoring.Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => !x.Deleted).AsNoTracking()];
                return Utils.Responses.Ok + $"removed you from the ping list for {toDelete.ChannelName}";
            }

            User? uu = idProvided 
                ? await GraphQLClient.GetUserRolesByID(userIDNumber.ToString())
                : await GraphQLClient.GetUserRolesByLogin(targetUsername);

            if (uu?.Roles is null) return Utils.Responses.Surprise + "failed to get user status";
            if (!((uu.Roles.IsMonetized ?? false) || (uu.Roles.IsAffiliate ?? false) || (uu.Roles.IsPartner ?? false))) return Utils.Responses.Surprise + "user is not monetized and should not be able to have emotes ";
            
            EmoteMonitorChannel emc = new()
            {
                ChannelID = int.Parse(uu.Id ?? "1"),
                ChannelName = uu.Login ?? "",
                OutputChannelName = targetOutputChannelName,
                UpdateSubscribersUsers = [message.Sender.Id]
            };

            EmoteMonitorChannel? existing = BotCore.DBContext.EmoteMonitorChannels.SingleOrDefault(x => x.ChannelID == emc.ChannelID);
            if (existing is not null)
            {
                bool pingListChangeRequired = true;
                
                CommandResult res = Utils.Responses.Ok + $"already monitoring {emc.ChannelName}";
                if (existing.Deleted)
                {
                    res += " (restored)";
                    existing.Deleted = false;
                }

                if (usedArs.TryGetValue("channel", out string? newTargetChannel) && message.Sender.Privileges == Privileges.Uuh)
                {
                    pingListChangeRequired = false;
                    existing.OutputChannelName = newTargetChannel;
                    res += $" changed output channel to {newTargetChannel}";
                }

                if (usedArs.TryGetValue("rmsub", out string? login))
                {
                    pingListChangeRequired = false;
                    if (string.IsNullOrEmpty(login)) return Utils.Responses.Surprise + "no username provided";
                    Data.Models.User? u = BotCore.DBContext.Users.FirstOrDefault(u => u.Username == login);
                    if (u is null) return Utils.Responses.Surprise + "no such user";
                    existing.UpdateSubscribersUsers.Remove(u.Id);
                    res += $" removed {u.Username} from the ping list";
                }
                
                if(pingListChangeRequired)
                {
                    if (existing.UpdateSubscribersUsers.Contains(message.Sender.Id)) { res += " you are in the ping list."; }
                    else
                    {
                        res += " added you to the ping list.";
                        existing.UpdateSubscribersUsers.Add(message.Sender.Id);
                    }
                }

                await BotCore.DBContext.SaveChangesAsync();
                Monitoring.Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => !x.Deleted).AsNoTracking()];
                return res;
            }

            BotCore.DBContext.EmoteMonitorChannels.Add(emc);
            await BotCore.DBContext.SaveChangesAsync();
            Monitoring.Channels = [.. BotCore.DBContext.EmoteMonitorChannels.Where(x => !x.Deleted).AsNoTracking()];
            return Utils.Responses.Ok + $"added emote monitor for {emc.ChannelName} in {emc.OutputChannelName}";
        }
        catch (Exception ex)
        {
            BotCore.Nlog.Error(ex);
            return Utils.Responses.Fail;
        }
    }
}
