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
        (int chatterCount, ChattersInfo? chatters) = await GraphQLClient.GetChattersCount(message.ChannelID);

        string chatter = Random.Shared.Next(100) switch
        {
            <= 3 => PickFromVips(),
            <= 10 => PickFromBots(),
            > 10 => PickFromViewers()
        };
        
        return new($"{chatterCount} lurkers{chatter} uuh");
        
        string PickFromBots()
        {
            if (chatters?.Chatbots is null || chatters.Chatbots.Length == 0) return PickFromViewers();
            return $", and clanker @{chatters.Chatbots[Random.Shared.Next(chatters.Chatbots.Length)]?.Login} is one of them";
        }

        string PickFromVips()
        {
            if (chatters?.Vips is null || chatters.Vips.Length == 0) return PickFromViewers();
            return $", and @{chatters.Vips[Random.Shared.Next(chatters.Vips.Length)]?.Login} is one of the VIPs";
        }

        string PickFromViewers()
        {
            if (chatters?.Viewers is null || chatters.Viewers.Length == 0) return "";
            return $", and @{chatters.Viewers[Random.Shared.Next(chatters.Viewers.Length)]?.Login} is one of them";
        }
    }
}
