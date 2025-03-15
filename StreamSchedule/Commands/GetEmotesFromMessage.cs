using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;
using TwitchLib.Client.Models;

namespace StreamSchedule.Commands;

internal class GetEmotesFromMessage : Command
{
    internal override string Call => "emot";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get emote owners from a reply, your message, message by messageID, or by emote id";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["messageid", "emoteid"];

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        CommandResult response = new();
        _ = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);

        List<string?> emoteIDs;
        ChatMessage? reply = null;

        if (usedArgs.TryGetValue("emoteid", out string? passedEmoteID))
        {
            emoteIDs = [passedEmoteID];
        }
        else
        {
            if (!string.IsNullOrEmpty(message.replyID))
            {
                reply = BotCore.MessageCache.Find(x => x.Id == message.replyID);
            }

            if (reply is null)
            {
                emoteIDs = await BotCore.GQLClient.GetEmoteIDsFromMessage(usedArgs.TryGetValue("messageid", out string? passedMessageID) ? passedMessageID : message.replyID ?? message.ID);
            }
            else
            {
                emoteIDs = [.. reply.EmoteSet.Emotes.Select(e => e.Id)];
            }

            if (emoteIDs.Count == 0) { return response + "no emotes found"; }
        }

        List<string> channels = [];

        List<Task<GraphQL.Data.Emote?>> tasks = [];

        foreach (string? emoteID in emoteIDs.Distinct())
        {
            if (emoteID is null) continue;
            tasks.Add(BotCore.GQLClient.GetEmote(emoteID));
        }

        await Task.WhenAll(tasks);
        foreach (var task in tasks)
        {
            if (task.Result is null) { channels.Add("Erm"); continue; }

            if (task.Result.Owner is null) { channels.Add(Helpers.EmoteTypeToString(task.Result.Type)); continue; }

            string subTierOrBitPrice = task.Result.Type switch
            {
                EmoteType.BITS_BADGE_TIERS => task.Result.BitsBadgeTierSummary?.Threshold.ToString() ?? "",
                EmoteType.SUBSCRIPTIONS => Helpers.SubscriptionSummaryTierToString(task.Result.SubscriptionTier),
                _ => ""
            };

            string artist = (task.Result.Artist?.Login is null) ? "" : $"By: {task.Result.Artist.Login}";

            channels.Add($"( {task.Result.Token} @{task.Result.Owner.Login} {subTierOrBitPrice} {Helpers.EmoteTypeToString(task.Result.Type)} {artist} )");
        }

        response += string.Join(" ", channels);

        return response;
    }
}
