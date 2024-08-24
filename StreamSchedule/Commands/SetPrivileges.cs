using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using TwitchLib.Client.Models;

namespace StreamSchedule.Commands;

internal class SetPrivileges : Command
{
    internal override string Call => "setp";

    internal override Privileges MinPrivilege => Privileges.Mod;

    internal override string? Handle(ChatMessage message)
    {
        string[] split = message.Message.Split(' ');
        Privileges p = Utils.ParsePrivilege(split[1]);
        User? target = Body.dbContext.Users.SingleOrDefault(u => u.Username == split[2].ToLower());

        Console.WriteLine(split[2]);

        if (target != null)
        {
            Body.dbContext.Users.Update(target);
            target.privileges = p;
            Body.dbContext.SaveChanges();
            return Utils.Responses.Ok + $"{target.Username} is now {split[1]}";
        }
        else
        {
            return Utils.Responses.Surprise;
        }
    }
}
