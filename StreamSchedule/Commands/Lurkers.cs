using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.Commands;

internal class Lurkers : Command
{
    public override string Call => "lurkers";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "get channel lurkers";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.ThreeMinutes);
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];
    
    
    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        (int chatterCount, ChattersInfo? chatterGroups) = await GraphQLClient.GetChattersCount(message.ChannelID);
        
        string chatter;
        
        if (chatterGroups?.Viewers is null || chatterGroups.Viewers.Length == 0) chatter = "";
        else
        {
            string ch = chatterGroups.Viewers[Random.Shared.Next(0, chatterGroups.Viewers.Length)]?.Login ?? "";
            chatter = $", and @{ch} is one of them";
        }

        return new($"{chatterCount} lurkers{chatter} uuh");
    }
}
