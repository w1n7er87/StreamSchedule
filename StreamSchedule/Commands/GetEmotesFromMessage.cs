using StreamSchedule.Data;
using TwitchLib.Client.Models;

namespace StreamSchedule.Commands;

internal class GetEmotesFromMessage : Command
{
    internal override string Call => "emot";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get emote owners from a message";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        CommandResult response = new("");

        if (string.IsNullOrEmpty(message.ReplyID))
        {
            return response;
        }

        ChatMessage? reply = BotCore.Instance.MessageCache.FirstOrDefault(x => x.Id == message.ReplyID);

        if (reply == null)
        {
            return Utils.Responses.Fail + " the message is too old. ";
        }

        var emotes = reply.EmoteSet.Emotes;

        if (emotes.Count == 0) { return response; }

        HashSet<string> channels = [];

        List<Task<string>> tasks = [];

        foreach (Emote? emote in emotes)
        {
            if ((BotCore.GlobalEmotes ?? []).Any(x => x.Id == emote.Id))
            {
                channels.Add("twitch");
                continue;
            }
            tasks.Add(GetEmoteChannel.GetEmoteChannelByEmoteID(emote.Id));
        }

        await Task.WhenAll(tasks);
        foreach (var task in tasks)
        {
            channels.Add(task.Result);
        }

        foreach (var channel in channels)
        {
            if (!string.IsNullOrWhiteSpace(channel)) response += "@" + channel + " ";
        }

        return response;
    }
}
