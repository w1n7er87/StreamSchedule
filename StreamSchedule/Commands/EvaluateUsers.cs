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
        string text = Utils.RetrieveArguments(Arguments!, message.Message, out List<string> usedArgs);

        string[] split = text.Split(' ');
        float scoreCutoff = DefaultCutoffScore;
        string targetUsername;
        CommandResult result = new("");

        try
        {
            if (string.IsNullOrWhiteSpace(split[0])) // nothing provided - run on all with default cutoff
            {
                result += UpdateAll().ToString();
            }
            else if (split.Length >= 2) // had two things provided
            {
                targetUsername = split[0]; //assume first was username

                if (usedArgs.Contains("s"))
                {
                    _ = float.TryParse(split[1], out scoreCutoff); //if the second thing actually was an argument - try parse the second thing as value
                }
                result += TryUpdateSingle(targetUsername, scoreCutoff) ? "1" : "0";
            }
            else // but if had less then 2 things provided
            {
                if (usedArgs.Contains("s")) // if an argument was used - the only thing that's left should be the number
                {
                    _ = float.TryParse(split[0], out scoreCutoff); // try parse it
                    result += UpdateAll().ToString();
                }
                else
                {
                    targetUsername = split[0]; // if there was not argument used, assume it's a username
                    result += TryUpdateSingle(targetUsername, scoreCutoff) ? "1" : "0";
                }
            }
            return Task.FromResult(result + " user('s) updated");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Task.FromResult(Utils.Responses.Surprise);
        }
    }

    private int UpdateAll(float? cutoff = null)
    {
        cutoff ??= DefaultCutoffScore;
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

    private bool TryUpdateSingle(string username, float? cutoff = null)
    {
        cutoff ??= DefaultCutoffScore;

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
