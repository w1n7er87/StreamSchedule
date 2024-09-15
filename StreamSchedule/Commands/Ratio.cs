using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class Ratio : Command
{
    internal override string Call => "ratio";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show user offline/online chat ratio and offliner score: [username] ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        string targetUsername = message.Username;
        CommandResult result = new("Your ");

        if (!string.IsNullOrWhiteSpace(split[0]))
        {
            targetUsername = split[0].Replace("@", "");
            result = new(targetUsername + "'s ");
        }

        User? u = Body.dbContext.Users.SingleOrDefault(x => x.Username == targetUsername);
        if (u is null)
        {
            return Task.FromResult(Utils.Responses.Fail + " unknown user ");
        }

        RatioScore ratioScore = Userscore.GetRatioAndScore(u);

        return Task.FromResult(result + $"offliner ratio: {ratioScore.ratio} , offliner score: {ratioScore.score} ");
    }
}
