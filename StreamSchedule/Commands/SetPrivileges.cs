using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class SetPrivileges : Command
{
    internal override string Call => "setp";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "set other user's privileges (who's privileges are < yours) to <= your privileges: [privilege](ban<ok<trusted<mod) [target](username, must be known user).";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');

        Privileges p = PrivilegesConversion.ParsePrivilege(split[0]);

        if (!Utils.TryGetUser(split[1], out User target))
        {
            return Task.FromResult(Utils.Responses.Surprise);
        }

        if (target.privileges >= message.Privileges) { return Task.FromResult(Utils.Responses.Fail); }

        Body.dbContext.Users.Update(target);
        target.privileges = p;
        Body.dbContext.SaveChanges();

        return Task.FromResult(Utils.Responses.Ok + $"{target.Username} is now {PrivilegesConversion.PrivilegeToString(p)}");
    }
}
