using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class Ratio : Command
{
    public override string Call => "score";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "show user offline/total chat ratio and chat score: [username]";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Content.Split(' ');
        User target = message.Sender;
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
