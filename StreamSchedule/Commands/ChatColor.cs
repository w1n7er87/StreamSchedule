using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class ChatColor : Command
{
    public override string Call => "chatcolor";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "average color of last 25 messages' senders";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Longer);
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];
    
    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        List<Color> colors = BotCore.MessageCache.TakeLast(25).Select(m => Color.FromHex(m.ColorHex)).ToList();
        
        Color average = new Color();
        foreach (Color c in colors)
            average += c;
        average /= colors.Count;
        average = await ColorInfo.GetColorName(average);
        return new CommandResult($"uuh chat is looking kinda {average.name} with this #{average.ToHex()} color");
    }
}
