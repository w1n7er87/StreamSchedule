using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Randoms : Command
{
    public override string Call => "random";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "get random number [0 - n( = 100)]; -flip for 50/50";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["flip"];
    public override List<string> Aliases { get; set; } = [];
    
    private static readonly string[] neuros = ["nwero", "hiyori", "eliv", "nuero", "newero", "wuero", "cleliv", "cluero", "weliv", "newliv"];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        CommandResult result = new($"{(Random.Shared.Next(100) < 50 ? neuros[Random.Shared.Next(neuros.Length)] : BotCore.MessageCache[Random.Shared.Next(BotCore.MessageCache.Count)].Username)} says: ");

        string[] split = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> usedArgs).Split(' ');

        if (usedArgs.TryGetValue("flip", out _)) return Task.FromResult(result + (Random.Shared.Next(100) < 50 ? "YES " : "NO "));

        int maxValue = int.TryParse(split[0], out int c) ? int.Clamp(c + 1, 1, int.MaxValue) : 101;

        return Task.FromResult(result + Random.Shared.Next(1, maxValue).ToString());
    }
}
