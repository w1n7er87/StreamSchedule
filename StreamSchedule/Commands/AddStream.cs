using StreamSchedule.Data;
using TwitchLib.Client.Models;

namespace StreamSchedule.Commands;

internal class AddStream : Command
{
    internal override string Call => "test";
    internal override Privileges MinPrivilege => Privileges.Mod;

    private readonly string inputPattern = "d-M-H-mm";

    internal override string? Handle(ChatMessage message)
    {
        string[] split = message.Message.Split(" ");
        if (split.Length < 3) { return Utils.Responses.Fail; }

        DateTime temp = DateTime.Now;

        if (!DateTime.TryParseExact(split[1], inputPattern, null, System.Globalization.DateTimeStyles.AssumeLocal, out temp))
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
