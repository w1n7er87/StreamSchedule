using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class SetPrivileges : Command
{
    internal override string Call => "setp";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "set other user's privileges (who's privileges are < yours) to < your privileges: [privilege](ban<ok<trusted<mod) [target](username, must be known user).";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');

        Privileges p = PrivilegeUtils.ParsePrivilege(split[0]);

        if (!User.TryGetUser(split[1], out User target))
        {
            return Task.FromResult(Utils.Responses.Surprise);
        }

        if (target.Privileges >= message.Sender.Privileges) { return Task.FromResult(Utils.Responses.Fail); }

        BotCore.DBContext.Users.Update(target);
        target.Privileges = p;
        BotCore.DBContext.SaveChanges();

        return Task.FromResult(Utils.Responses.Ok + $"{target.Username} is now {PrivilegeUtils.PrivilegeToString(p)}");
    }
}
