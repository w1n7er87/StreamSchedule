using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;
using TwitchLib.Client.Models;
using Emote = StreamSchedule.GraphQL.Data.Emote;

namespace StreamSchedule.Commands;

internal class GetEmotesFromMessage : Command
{
    public override string Call => "emot";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "get emote owners from a reply, your message, message by messageID, or by emote id";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["messageid", "emoteid"];
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        _ = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> usedArgs);

        List<string?> emoteIDs;
        ChatMessage? reply = null;

        if (usedArgs.TryGetValue("emoteid", out string? passedEmoteID))
        {
            emoteIDs = [passedEmoteID];
        }
        else
        {
            if (!string.IsNullOrEmpty(message.ReplyID)) reply = BotCore.MessageCache.Find(x => x.Id == message.ReplyID);

            if (reply is null)
                emoteIDs = await GraphQLClient.GetEmoteIDsFromMessage(usedArgs.TryGetValue("messageid", out string? passedMessageID)
                        ? passedMessageID
                        : message.ReplyID ?? message.ID);
            else emoteIDs = [.. reply.EmoteSet.Emotes.Select(e => e.Id)];
            if (emoteIDs.Count == 0) return "no emotes found";
        }

        List<string> channels = [];

        List<Task<Emote?>> tasks = [];

        foreach (string? emoteID in emoteIDs.Distinct())
        {
            if (emoteID is null) continue;
            tasks.Add(GraphQLClient.GetEmote(emoteID));
        }

        await Task.WhenAll(tasks);
        foreach (Task<Emote?> task in tasks)
        {
            if (task.Result is null) { channels.Add("Erm"); continue; }

            if (task.Result.Owner is null) { channels.Add(Helpers.EmoteTypeToString(task.Result.Type)); continue; }

            string subTierOrBitPrice = task.Result.Type switch
            {
                EmoteType.BITS_BADGE_TIERS => task.Result.BitsBadgeTierSummary?.Threshold.ToString() ?? "",
                EmoteType.SUBSCRIPTIONS => Helpers.SubscriptionSummaryTierToString(task.Result.GetTier()),
                _ => ""
            };

            string artist = task.Result.Artist?.Login is null ? "" : $"By: {task.Result.Artist.Login}";

            channels.Add($"( {task.Result.Token} @{task.Result.Owner.Login} {subTierOrBitPrice} {Helpers.EmoteTypeToString(task.Result.Type)} {artist} )");
        }

        return string.Join(" ", channels);
    }
}
