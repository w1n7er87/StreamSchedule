using StreamSchedule.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamSchedule;

internal class Utils
{
    internal static class Responses
    {
        internal static string Fail => "NOIDONTTHINKSO ";
        internal static string Ok => "ok ";
        internal static string Surprise => "oh ";
    }

    internal Privileges ParsePrivilege(string text)
    {
        return text.ToLower() switch
        {
            "ban" => Privileges.Mod,
            "ok" => Privileges.None,
            "trusted" => Privileges.Trusted,
            "mod" => Privileges.Mod,
            _ => Privileges.None
        };
    }
}
