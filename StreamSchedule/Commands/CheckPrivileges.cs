using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class CheckPrivileges : Command
{
    internal override string Call => "checkp";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "check bot privileges: [username](optional)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
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
