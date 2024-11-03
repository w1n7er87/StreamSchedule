using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class EvaluateUsers : Command
{
    internal override string Call => "evaluate";
    internal override Privileges MinPrivilege => Privileges.Uuh;
    internal override string Help => "evaluate user's scores and assign privileges accordingly: [username](optional) ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["s"];

    private static float DefaultCutoffScore => 3.5f;

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> usedArgs);

        string[] split = text.Split(' ');

        float scoreCutoff = usedArgs.TryGetValue("s", out string? cutoff) ? float.TryParse(cutoff, out scoreCutoff) ? scoreCutoff : DefaultCutoffScore : DefaultCutoffScore;

        CommandResult result = new();

        try
        {
            if (string.IsNullOrWhiteSpace(split[0])) // nothing provided - run on all with default cutoff
            {
                result += (await UpdateAll(scoreCutoff)) + $" user('s) updated ({scoreCutoff})";
            }
            else // had something? assume it's a username
            {
                string targetUsername = split[0];

                result += await TryUpdateSingle(targetUsername, scoreCutoff) ? $"{targetUsername}'s privileges have been updated ({scoreCutoff})" : $"{targetUsername}'s privileges nave not been updated ({scoreCutoff})";
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Utils.Responses.Surprise;
        }
    }

    private static async Task<int> UpdateAll(float cutoff)
    {
        int count = 0;
        var users = BotCore.DBContext.Users.AsAsyncEnumerable();
        await foreach (var user in users)
        {
            if (user.Privileges == Privileges.Banned) continue;

            float score = Userscore.GetRatioAndScore(user).score;

            switch (user.Privileges)
            {
                case < Privileges.Trusted when score >= cutoff:
                    user.Privileges = Privileges.Trusted;
                    count++;
                    continue;
                case Privileges.Trusted when score < cutoff:
                    user.Privileges = Privileges.None;
                    count++;
                    break;
            }
        }
        await BotCore.DBContext.SaveChangesAsync();
        return count;
    }

    private static async Task<bool> TryUpdateSingle(string username, float cutoff)
    {
        if (!User.TryGetUser(username, out User user)) return false;

        if (user.Privileges is Privileges.Banned or >= Privileges.Mod) return false;

        float score = Userscore.GetRatioAndScore(user).score;

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
