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

        if (!string.IsNullOrWhiteSpace(split[0]))
        {
            targetUsername = split[0];
        }

        if (!Utils.TryGetUser(targetUsername, out User u)) 
        {
            return Task.FromResult(Utils.Responses.Fail);
        }

        return Task.FromResult(new CommandResult($"{targetUsername} is {Utils.PrivilegeToString(u.privileges)}", false));
    }
}
