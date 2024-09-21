using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class EvaluateUsers : Command
{
    internal override string Call => "evaluate";
    internal override Privileges MinPrivilege => Privileges.Uuh;
    internal override string Help => "evaluate user's scores and assign privileges accordingly: [username](optional) ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["s"];

    internal float DefaultCutoffScore => 3.5f;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> usedArgs);

        string[] split = text.Split(' ');

        float scoreCutoff = usedArgs.TryGetValue("s", out string? cutoff) ? float.TryParse(cutoff, out scoreCutoff) ? scoreCutoff : DefaultCutoffScore : DefaultCutoffScore;

        string targetUsername;
        CommandResult result = new("");

        try
        {
            if (string.IsNullOrWhiteSpace(split[0])) // nothing provided - run on all with default cutoff
            {
                result += UpdateAll(scoreCutoff).ToString();
            }
            else // had something? assume it's a username
            {
                targetUsername = split[0];

                result += TryUpdateSingle(targetUsername, scoreCutoff) ? "1" : "0";
            }
            return Task.FromResult(result + " user('s) updated");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Task.FromResult(Utils.Responses.Surprise);
        }
    }

    private int UpdateAll(float cutoff)
    {
        int count = 0;
        List<User> users = [.. Body.dbContext.Users];
        foreach (var user in users)
        {
            float score = Userscore.GetRatioAndScore(user).score;
            if (user.privileges < Privileges.Trusted && score >= cutoff)
            {
                Body.dbContext.Update(user);
                user.privileges = Privileges.Trusted;
                count++;
            }
            else if (user.privileges == Privileges.Trusted && score <= cutoff)
            {
                Body.dbContext.Update(user);
                user.privileges = Privileges.None;
                count++;
            }
        }
        Body.dbContext.SaveChanges();
        return count;
    }

    private bool TryUpdateSingle(string username, float cutoff)
    {
        if (!Utils.TryGetUser(username, out User user)) { return false; }

        float score = Userscore.GetRatioAndScore(user).score;
        if (user.privileges < Privileges.Trusted && score >= cutoff)
        {
            Body.dbContext.Update(user);
            user.privileges = Privileges.Trusted;
        }
        else if (user.privileges == Privileges.Trusted && score < cutoff)
        {
            Body.dbContext.Update(user);
            user.privileges = Privileges.None;
        }
        else
        {
            return false;
        }

        Body.dbContext.SaveChanges();
        return true;
    }
}
