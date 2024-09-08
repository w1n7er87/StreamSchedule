using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class SetPrivileges : Command
{
    internal override string Call => "setp";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "set other user's privileges (who's privileges are < yours) to <= your privileges: [privilege](ban<ok<trusted<mod) [target](username, must be known user).";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override Task<string> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');

        Privileges p = Utils.ParsePrivilege(split[0]);
        User? target = Body.dbContext.Users.SingleOrDefault(u => u.Username == split[1].ToLower().Replace("@", string.Empty));

        if (target != null)
        {
            if (target.privileges >= message.Privileges) { return Task.FromResult(Utils.Responses.Fail); }
            Body.dbContext.Users.Update(target);
            target.privileges = p;
            Body.dbContext.SaveChanges();
            return Task.FromResult(Utils.Responses.Ok + $"{target.Username} is now {Utils.PrivilegeToString(p)}");
        }
        else
        {
            return Task.FromResult(Utils.Responses.Surprise);
        }
    }
}
