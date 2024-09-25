using Microsoft.EntityFrameworkCore;
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

    private readonly string[] inputPatterns = ["d-M-H-mm", "dd-MM-H-mm"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(" ");
        if (split.Length < 2) { return Task.FromResult(Utils.Responses.Fail); }

        DateTime temp = DateTime.Now;

        if (!DateTime.TryParseExact(split[0], inputPatterns, null, System.Globalization.DateTimeStyles.AssumeLocal, out temp))
        {
            return Task.FromResult(Utils.Responses.Fail + "bad date ");
        }

        Data.Models.Stream stream = new()
        {
            StreamDate = DateOnly.FromDateTime(temp),
            StreamTime = TimeOnly.FromDateTime(temp),
            StreamTitle = message.Message[(split[0].Length + 1)..],
            StreamStatus = StreamStatus.Confirmed,
        };

        try
        {
            var existingStreamsOnThatDay = BotCore.DBContext.Streams.Where(x => x.StreamDate == stream.StreamDate);


            if (!existingStreamsOnThatDay.Any(x => x.StreamTime == stream.StreamTime))
            {
                BotCore.DBContext.Streams.Add(stream);
                BotCore.DBContext.SaveChanges();
                return Task.FromResult(Utils.Responses.Ok + $" added a new stream \"{stream.StreamTitle[..15]}\"");
            }
            else
            {
                var s = existingStreamsOnThatDay.First(x => x.StreamTime == stream.StreamTime);
                s = stream;
                BotCore.DBContext.SaveChanges();
                return Task.FromResult(Utils.Responses.Ok + $" added a new stream \"{stream.StreamTitle[..15]}\"");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.FromResult(Utils.Responses.Fail);
        }
    }
}
