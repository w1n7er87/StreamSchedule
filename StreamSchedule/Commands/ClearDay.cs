using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class ClearDay : Command
{
    internal override string Call => "clearday";

    internal override Privileges MinPrivilege => Privileges.Mod;

    private readonly string[] inputPatterns = ["d-M-yy", "dd-MM-yy"];

    internal override string Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        DateTime temp = new();

        if (split.Length < 2 || !DateTime.TryParseExact(split[1], inputPatterns, null, System.Globalization.DateTimeStyles.AssumeLocal, out temp))
        {
            return Utils.Responses.Fail + "bad date ";
        }

        Data.Models.Stream? interest = Body.dbContext.Streams.SingleOrDefault(s => s.StreamDate == DateOnly.FromDateTime(temp));

        if (interest == null)
        {
            return "Nothing on that day.";
        }
        else
        {
            Body.dbContext.Streams.Remove(interest);
            Body.dbContext.SaveChanges();
            return Utils.Responses.Ok;
        }
    }
}
