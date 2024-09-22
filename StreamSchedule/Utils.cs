using StreamSchedule.Data;

namespace StreamSchedule;

internal static class Utils
{
    internal static class Responses
    {
        internal static CommandResult Fail => new("NOIDONTTHINKSO ", false);
        internal static CommandResult Ok => new("ok ", false);
        internal static CommandResult Surprise => new("oh ", false);
    }
}
