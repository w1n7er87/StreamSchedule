using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.Commands;

internal class Pinned : Command
{
    public override string Call => "pinned";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "show pinned message";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        PinnedChatMessageConnection? pinnedMessages = await GraphQLClient.GetPinnedMessage(message.ChannelName);
        if (pinnedMessages?.Edges is null || pinnedMessages.Edges.Length == 0) return new CommandResult("nothing pinned");
        return new($"{string.Join(";", pinnedMessages.Edges.Select(m => $"{m?.Node?.PinnedMessage?.Sender?.Login}: {m?.Node?.PinnedMessage?.Content?.Text} . pinned by: {m?.Node?.PinnedBy?.Login} "))}", requiresFilter: true);
    }
}
