using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Randoms : Command
{
    internal override string Call => "random";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get random number [0 - n( = 100)]; -flip for 50/50; -w(x = 5) for words from recent messages (50 max)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["flip", "w"];

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

        int maxValue = int.TryParse(split[0], out int c) ? int.Clamp(c + 1, 1, int.MaxValue) : 101;

        if (usedArgs.TryGetValue("w", out string? wordCount))
        {
            int max = int.TryParse(wordCount, out int cc) ? int.Clamp(cc, 1, 50) : 5;
            List<string> results = [];
            int count = 0;

            while(count < max)
            {
                string[] splitMessage = [.. BotCore.MessageCache[Random.Shared.Next(BotCore.MessageCache.Count)].Message.Split(' ').Where(x => !x.StartsWith('@'))];
                results.Add(splitMessage[Random.Shared.Next(splitMessage.Length)]);
                count++;
            }
            return Task.FromResult(result + string.Join(" ", results));
        }

        return Task.FromResult(result + Random.Shared.Next(1, maxValue).ToString());
    }
}
