using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.Commands;

internal class ChannelRules : Command
{
    public override string Call => "rules";
    public override Privileges Privileges => Privileges.Banned;
    public override string Help => "channel rules";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        ChatSettings? settings = await GraphQLClient.GetChatSettings(message.channelID);
        string links = settings?.BlockLinks switch
        {
            true => "links are not allowed, ",
            _ => ""
        };

        return new($"{links}chat rules: {string.Join(", ", settings?.Rules?.Index().Select(x => $"{x.Index + 1} - {x.Item}") ?? ["no channel rules set (or failed to fetch idk."])}", true, true);
    }
}
