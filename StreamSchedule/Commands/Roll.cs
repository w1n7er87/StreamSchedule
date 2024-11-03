using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Roll : Command
{
    internal override string Call => "roll";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get random number [0 - 100], -max[n] for [0 - n], -flip for 50/50";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["flip", "max"];
    
    static readonly string[] neuros = ["nwero", "hiyori", "eliv", "nuero"];
    
    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        CommandResult result = new($"{neuros[Random.Shared.Next(neuros.Length)]} says: ");
        Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string,string> usedArgs);
        int maxValue = usedArgs.TryGetValue("max", out string? a) ? int.TryParse(a, out int b) ? b > 0 ? b + 1 : 101 : 101 : 101;
        
        if (usedArgs.TryGetValue("flip", out _))
        {
            return Task.FromResult(result + (Random.Shared.Next(100) < 50? "YES " : "NO "));
        }
        return Task.FromResult(result + Random.Shared.Next(maxValue).ToString());
    }
}
