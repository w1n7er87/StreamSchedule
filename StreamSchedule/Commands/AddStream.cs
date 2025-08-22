using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class AddStream : Command
{
    public override string Call => "sets";
    public override Privileges Privileges => Privileges.Mod;
    public override string Help => "set new stream time or update given day: [date-time] (d-M-H-mm dd-MM-H-mm d-M-yy-H-mm dd-MM-yy-H-mm d-M-yyyy-H-mm dd-MM-yyyy-H-mm) [stream title] (required)";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    private readonly string[] _inputPatterns = ["d-M-H-mm", "dd-MM-H-mm", "d-M-yy-H-mm", "dd-MM-yy-H-mm", "d-M-yyyy-H-mm", "dd-MM-yyyy-H-mm"];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.content.Split(" ");
        if (split.Length < 2) { return Task.FromResult(Utils.Responses.Fail); }

        if (!DateTime.TryParseExact(split[0], _inputPatterns, null, System.Globalization.DateTimeStyles.AssumeLocal, out var temp))
        {
            return Task.FromResult(Utils.Responses.Fail + "bad date ");
        }
        temp = temp.ToUniversalTime();

        Data.Models.Stream stream = new()
        {
            StreamDate = DateOnly.FromDateTime(temp),
            StreamTime = TimeOnly.FromDateTime(temp),
            StreamTitle = message.content[(split[0].Length + 1)..]
        };

        Data.Models.Stream? s = BotCore.DBContext.Streams.FirstOrDefault(x => x.StreamDate == stream.StreamDate);
        if (s == null)
        {
            BotCore.DBContext.Streams.Add(stream);
        }
        else
        {
            BotCore.DBContext.Streams.Update(s);
            s.StreamTime = stream.StreamTime;
            s.StreamTitle = stream.StreamTitle;
        }
        BotCore.DBContext.SaveChanges();

        return Task.FromResult(Utils.Responses.Ok);
    }
}
