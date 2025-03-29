using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Roll : Command
{
    internal override string Call => "roll";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "roll a die [amount(max 100_000)]d[sides(max 100_000)], -v for details (up to 50 results)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["v"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string content = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);
            
        bool verbose = usedArgs.TryGetValue("v", out _);

        List<long> results = [];
        List<string> rolled = [];

        foreach (string s in content.Split(' ').Where(x => x.Contains('d', StringComparison.InvariantCultureIgnoreCase)))
        {
            string[] split = s.Split(['d', 'D'], StringSplitOptions.TrimEntries);

            if (split.Length < 2) continue;

            int sides = 0;

            if (split.Length < 3)
            {
                if (!int.TryParse(split[1], out sides)) continue;
            }
            else
            {
                if (!int.TryParse(split[2], out sides)) continue;
            }

            sides = int.Clamp(sides + 1, 1, 100_000);
            int amount = int.TryParse(split[0], out int n) ? int.Clamp(n, 1, 100_000) : 1;

            results.AddRange(RollInternal(amount, sides));
            rolled.Add($"{amount}d{sides - 1}");
        }

        switch (results.Count)
        {
            case < 1:
                return Task.FromResult(new CommandResult($"rolled {RollInternal(1, 7)[0]} (1d6) "));
            case > 50:
                verbose = false;
                break;
        }

        return Task.FromResult(new CommandResult(verbose
            ? $"rolled: {string.Join(", ", results)} ({results.Sum()} total; {string.Join(", ", rolled)})"
            : results.Sum().ToString()));
    }

    private static long[] RollInternal(int amount, int sides)
    {
        long[] results = new long[amount];
        for (int i = 0; i < amount; i++) results[i] = Random.Shared.Next(1, sides);
        return results;
    }
}
