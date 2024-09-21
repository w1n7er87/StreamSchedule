using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class CheckPrivileges : Command
{
    internal override string Call => "checkp";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "check bot privileges: [username](optional)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        string targetUsername = message.Username;

        if (!string.IsNullOrWhiteSpace(split[0]) && message.Privileges >= Privileges.Trusted)
        {
            targetUsername = split[0];
        }

        if (!Utils.TryGetUser(targetUsername, out User u))
        {
            targetUsername = message.Username;
        }

        return Task.FromResult(new CommandResult($"{targetUsername} is {PrivilegesConversion.PrivilegeToString(u.privileges)}", false));

    }
}
