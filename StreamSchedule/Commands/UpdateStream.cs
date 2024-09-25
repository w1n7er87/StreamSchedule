using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class UpdateStream : Command
{
    internal override string Call => "uus";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "update stream";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["s", "title", "time", "status"];

    private readonly string[] dateTimeInputPatterns = ["d-M-yy", "dd-MM-yy"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> usedArguments).Split(" ");

        DateTime temp = new();

        if (split.Length < 1 || !DateTime.TryParseExact(split[0], dateTimeInputPatterns, null, System.Globalization.DateTimeStyles.AssumeLocal, out temp))
        {
            return Task.FromResult(Utils.Responses.Fail + "bad date ");
        }

        string? newTitle = (split.Length >= 2)? split[1] : null;

        var streamsOnRequestedDay = BotCore.DBContext.Streams.Where(x => x.StreamDate == DateOnly.FromDateTime(temp)).OrderBy(x => x.StreamTime);

        if (!streamsOnRequestedDay.Any())
        {
            return Task.FromResult(Utils.Responses.Fail + "nothing to update on that day");
        }

        int streamIndex = usedArguments.TryGetValue("s", out string? idxs) ? int.TryParse(idxs, out int idx) ? Math.Clamp(idx, 0, streamsOnRequestedDay.Count()) : 0 : 0;

        Data.Models.Stream s = streamsOnRequestedDay.ElementAt(streamIndex);

        if (usedArguments.TryGetValue("title", out _))
        {
            if (newTitle is not null)
            {
                s.StreamTitle = newTitle;
            }
        }
        if (usedArguments.TryGetValue("status", out string? newStatus))
        {

        }
        return Task.FromResult(Utils.Responses.Ok);
    }
}
