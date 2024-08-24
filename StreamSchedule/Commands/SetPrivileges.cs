﻿using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class SetPrivileges : Command
{
    internal override string Call => "setp";

    internal override Privileges MinPrivilege => Privileges.Mod;

    internal override string Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        Privileges p = Utils.ParsePrivilege(split[1]);
        User? target = Body.dbContext.Users.SingleOrDefault(u => u.Username == split[2].ToLower().Replace("@", string.Empty));

        Console.WriteLine(split[2]);

        if (target != null)
        {
            if(target.privileges >= message.Privileges) { return Utils.Responses.Fail; }
            Body.dbContext.Users.Update(target);
            target.privileges = p;
            Body.dbContext.SaveChanges();
            return Utils.Responses.Ok + $"{target.Username} is now {Utils.PrivilegeToString(p)}";
        }
        else
        {
            return Utils.Responses.Surprise;
        } 
    }
}
