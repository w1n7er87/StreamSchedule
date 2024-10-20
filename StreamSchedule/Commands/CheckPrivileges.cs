﻿using StreamSchedule.Data;
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
        User target = message.Sender;

        if (!string.IsNullOrWhiteSpace(split[0]) && message.Sender.privileges >= Privileges.Trusted)
        {
            target = User.TryGetUser(split[0], out User t) ? t : target;
        }
        return Task.FromResult(new CommandResult($"{target.Username} is {PrivilegeUtils.PrivilegeToString(target.privileges)}", false));
    }
}
