using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.LLM.Tools;

internal sealed class Stream : Tool
{
    public override string Token => "[STRM]";
    public override string Execute()
    {
        Data.Models.Stream? next = BotCore.DBContext.Streams.Where(x => x.StreamDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            .AsNoTracking()
            .AsEnumerable()
            .OrderBy(x => new DateTime(x.StreamDate, x.StreamTime))
            .FirstOrDefault(x => new DateTime(x.StreamDate, x.StreamTime) >= DateTime.UtcNow);
        return next is null ? "no stream" : next.StreamTitle!;
    }
}
