using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Roll : Command
{
    internal override string Call => "roll";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get random number [0 - n( = 100)], -flip for 50/50";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["flip"];

    private static readonly string[] neuros = ["nwero", "hiyori", "eliv", "nuero", "newero", "wuero", "cleliv", "cluero", "weliv"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        CommandResult result = new($"{
            (Random.Shared.Next(100) < 50 ? neuros[Random.Shared.Next(neuros.Length)] : BotCore.MessageCache[Random.Shared.Next(BotCore.MessageCache.Count)].Username)
            } says: ");
        
        string[] split = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs).Split(' ');

        if (usedArgs.TryGetValue("flip", out _))
        {
            return Task.FromResult(result + (Random.Shared.Next(100) < 50 ? "YES " : "NO "));
        }

        int maxValue = int.TryParse(split[0], out int c) ? int.Clamp(c + 1, 0, int.MaxValue) : 101;

        return Task.FromResult(result + Random.Shared.Next(maxValue).ToString());
    }
}
