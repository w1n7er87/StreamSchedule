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

    private enum ChatterType
    {
        Viewer,
        Vip,
        Bot,
    }
    
    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        (int chatterCount, ChattersInfo? chatterGroups) = await GraphQLClient.GetChattersCount(message.ChannelID);

        List<(ChatterType, string)> chatters = chatterGroups?.Viewers?.Select(c => (ChatterType.Viewer, c!.Login!)).ToList() ?? [];
        
        chatters.AddRange(chatterGroups?.Vips?.Select(c => (ChatterType.Vip, c!.Login!)) ?? []);
        chatters.AddRange(chatterGroups?.Chatbots?.Select(c => (ChatterType.Bot, c!.Login!)) ?? []);

        string chatter;
        
        if (chatters.Count == 0) chatter = "";
        else
        {
            (ChatterType, string) cc = chatters[Random.Shared.Next(0, chatters.Count)];
            chatter = $", and {cc.Item1 switch { ChatterType.Vip => "VIP ", ChatterType.Bot => "clanker ", _ => ""}}@{cc.Item2} is one them";
        }

        return new($"{chatterCount} lurkers{chatter} uuh");
    }
}
