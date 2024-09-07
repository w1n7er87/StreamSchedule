using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class ClearDay : Command
{
    internal override string Call => "clearday";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "clear schedule for the given day: [date] (d-M-yy or dd-MM-yy)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(1.1);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    private readonly string[] inputPatterns = ["d-M-yy", "dd-MM-yy"];

    internal override string Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        DateTime temp = new();

        if (split.Length < 1 || !DateTime.TryParseExact(split[0], inputPatterns, null, System.Globalization.DateTimeStyles.AssumeLocal, out temp))
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
