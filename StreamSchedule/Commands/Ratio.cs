using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class Ratio : Command
{
    internal override string Call => "score";
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
            targetUsername = split[0];
            result = new(targetUsername + "'s ");
        }

        if (!User.TryGetUser(targetUsername, out User u))
        {
            return Task.FromResult(Utils.Responses.Fail + " unknown user ");
        }

        RatioScore ratioScore = Userscore.GetRatioAndScore(u);

        return Task.FromResult(result + $"offliner ratio: {MathF.Round(ratioScore.ratio, 3)}, off/n: ({u.MessagesOffline}/{u.MessagesOnline}), offliner score: {MathF.Round(ratioScore.score, 3)} ");
    }
}
