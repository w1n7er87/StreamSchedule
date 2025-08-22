using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class CheckPrivileges : Command
{
    public override string Call => "checkp";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "check bot privileges: [username](optional)";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.content.Split(' ');
        User target = message.sender;

        if (!string.IsNullOrWhiteSpace(split[0]) && message.sender.Privileges >= Privileges.Trusted)
        {
            target = User.TryGetUser(split[0], out User t) ? t : target;
        }
        return Task.FromResult(new CommandResult($"{target.Username} is {PrivilegeUtils.PrivilegeToString(target.Privileges)}", false));
    }
}
