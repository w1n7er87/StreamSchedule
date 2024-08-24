using StreamSchedule.Data;

namespace StreamSchedule;

internal static class Utils
{
    internal static class Responses
    {
        internal static string Fail => "NOIDONTTHINKSO ";
        internal static string Ok => "Ok ";
        internal static string Surprise => "oh ";
    }

    internal static Privileges ParsePrivilege(string text)
    {
        return text.ToLower() switch
        {
            "banned" => Privileges.Banned,
            "ok" => Privileges.None,
            "trusted" => Privileges.Trusted,
            "mod" => Privileges.Mod,
            _ => Privileges.None
        };
    }
}
