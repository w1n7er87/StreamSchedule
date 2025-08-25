using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class EvaluateUsers : Command
{
    public override string Call => "evaluate";
    public override Privileges Privileges => Privileges.Uuh;
    public override string Help => "evaluate user's scores and assign privileges accordingly: [username](optional)";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["s"];
    public override List<string> Aliases { get; set; } = [];

    private static float DefaultCutoffScore => 3.2f;

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> usedArgs);

        string[] split = text.Split(' ');

        float scoreCutoff = usedArgs.TryGetValue("s", out string? cutoff)
            ? float.TryParse(cutoff, out scoreCutoff) ? scoreCutoff : DefaultCutoffScore
            : DefaultCutoffScore;

        CommandResult result = new();

        if (string.IsNullOrWhiteSpace(split[0]))
        {
            (int, int) promotedDemotedCounts = await UpdateAll(scoreCutoff);
            result += $"{promotedDemotedCounts.Item1} users promoted, {promotedDemotedCounts.Item2} users demoted ({scoreCutoff})";
        }
        else
        {
            string targetUsername = split[0];
            result += await TryUpdateSingle(targetUsername, scoreCutoff)
                ? $"{targetUsername}'s privileges have been updated ({scoreCutoff})"
                : $"{targetUsername}'s privileges nave not been updated ({scoreCutoff})";
        }
        return result;
    }

    private static async Task<(int, int)> UpdateAll(float cutoff)
    {
        int promoted = 0;
        int demoted = 0;

        await foreach (User? user in BotCore.DBContext.Users.AsAsyncEnumerable())
        {
            if (user.Privileges == Privileges.Banned) continue;

            float score = Userscore.Score(user);

            switch (user.Privileges)
            {
                case < Privileges.Trusted when score >= cutoff:
                    user.Privileges = Privileges.Trusted;
                    promoted++;
                    continue;
                case Privileges.Trusted when score < cutoff:
                    user.Privileges = Privileges.None;
                    demoted++;
                    break;
            }
        }

        await BotCore.DBContext.SaveChangesAsync();
        return (promoted, demoted);
    }

    private static async Task<bool> TryUpdateSingle(string username, float cutoff)
    {
        if (!User.TryGetUser(username, out User user)) return false;

        if (user.Privileges is Privileges.Banned or >= Privileges.Mod) return false;

        float score = Userscore.Score(user);

        switch (user.Privileges)
        {
            case < Privileges.Trusted when score >= cutoff:
                user.Privileges = Privileges.Trusted;
                await BotCore.DBContext.SaveChangesAsync();
                return true;

            case Privileges.Trusted when score < cutoff:
                user.Privileges = Privileges.None;
                await BotCore.DBContext.SaveChangesAsync();
                return true;

            default:
                return false;
        }
    }
}
