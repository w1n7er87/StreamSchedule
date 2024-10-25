namespace StreamSchedule.Data;

public enum Privileges
{
    Banned = -1,
    None = 0,
    Trusted = 1,
    Mod = 2,
    Uuh = 3,
}

internal static class PrivilegeUtils
{
    internal static Privileges ParsePrivilege(string text)
    {
        return text.ToLower() switch
        {
            "ban" => Privileges.Banned,
            "ok" => Privileges.None,
            "trust" => Privileges.Trusted,
            "mod" => Privileges.Mod,
            "uuh" => Privileges.Uuh,
            _ => Privileges.None
        };
    }

    internal static string PrivilegeToString(Privileges p)
    {
        return p switch
        {
            Privileges.Banned => "banned",
            Privileges.None => "a regular",
            Privileges.Trusted => "a VIP",
            Privileges.Mod => "a mod MONKA ",
            Privileges.Uuh => "a uuh ",
            _ => "an alien "
        };
    }
}
