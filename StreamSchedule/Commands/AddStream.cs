using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class AddStream : Command
{
    internal override string Call => "sets";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "set new stream time or update given day: [date-time] (d-M-H-mm or dd-MM-H-mm) [stream title] (required)";

    private readonly string[] inputPatterns = ["d-M-H-mm", "dd-MM-H-mm"];

    internal override string Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(" ");
        if (split.Length < 3) { return Utils.Responses.Fail; }

        DateTime temp = DateTime.Now;

        if (!DateTime.TryParseExact(split[1], inputPatterns, null, System.Globalization.DateTimeStyles.AssumeLocal, out temp))
        {
            return Utils.Responses.Fail + "bad date ";
        }

        Data.Models.Stream stream = new()
        {
            StreamDate = DateOnly.FromDateTime(temp),
            StreamTime = TimeOnly.FromDateTime(temp),
            StreamTitle = message.Message[(split[0].Length + split[1].Length + 2)..]
        };

        try
        {
            Data.Models.Stream? s = Body.dbContext.Streams.SingleOrDefault(x => x.StreamDate == stream.StreamDate);
            if (s == null)
            {
                Body.dbContext.Streams.Add(stream);
            }
            else
            {
                Body.dbContext.Streams.Update(s);
                s.StreamTime = stream.StreamTime;
                s.StreamTitle = stream.StreamTitle;
            }
            Console.WriteLine(Body.dbContext.SaveChanges());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Utils.Responses.Fail;
        }

        return Utils.Responses.Ok;
    }
}
