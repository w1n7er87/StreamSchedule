using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class Ratio : Command
{
    internal override string Call => "score";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show user offline/total chat ratio and chat score: [username]";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.content.Split(' ');
        User target = message.sender;
        CommandResult result = new("");

        if (!string.IsNullOrWhiteSpace(split[0]))
        {
            target = User.TryGetUser(split[0], out User t) ? t : target;
            result = new(target.Username + "'s ");
        }

        RatioScore ratioScore = Userscore.GetRatioAndScore(target);

        return Task.FromResult(result + $"messages: {target.MessagesOffline}/{target.MessagesOnline} ({MathF.Round(ratioScore.ratio, 3)}), chat score: {MathF.Round(ratioScore.score, 3)} ");
    }
}
