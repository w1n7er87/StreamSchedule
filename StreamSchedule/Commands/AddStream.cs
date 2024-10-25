using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class AddStream : Command
{
    internal override string Call => "sets";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "set new stream time or update given day: [date-time] (d-M-H-mm or dd-MM-H-mm) [stream title] (required)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    private readonly string[] _inputPatterns = ["d-M-H-mm", "dd-MM-H-mm"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(" ");
        if (split.Length < 2) { return Task.FromResult(Utils.Responses.Fail); }

        if (!DateTime.TryParseExact(split[0], _inputPatterns, null,System.Globalization.DateTimeStyles.AssumeLocal, out var temp))
        {
            return Task.FromResult(Utils.Responses.Fail + "bad date ");
        }
        temp = temp.ToUniversalTime();

        Data.Models.Stream stream = new()
        {
            StreamDate = DateOnly.FromDateTime(temp),
            StreamTime = TimeOnly.FromDateTime(temp),
            StreamTitle = message.Message[(split[0].Length + 1)..]
        };

        try
        {
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
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.FromResult(Utils.Responses.Fail);
        }

        return Task.FromResult(Utils.Responses.Ok);
    }
}
