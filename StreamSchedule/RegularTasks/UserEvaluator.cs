using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.RegularTasks;

public class UserEvaluator : Periodic
{
    private static readonly DatabaseContext dbContext = new(new DbContextOptionsBuilder<DatabaseContext>().UseSqlite("Data Source=StreamSchedule.data").Options);
    private static DateTime TimeToEvaluate = DateTime.UtcNow + TimeSpan.FromMinutes(10);
    private const float scoreCutoff = 3.5f;

    protected override async Task Update()
    {
        if (DateTime.UtcNow < TimeToEvaluate) return;

        BotCore.Nlog.Info($"Evaluating users . . .");

        (int, int) promotedDemotedCounts = await UpdateAll(scoreCutoff);

        TimeToEvaluate = DateTime.UtcNow + TimeSpan.FromDays(1);
        
        BotCore.Nlog.Info($"Regular user evaluation: {promotedDemotedCounts.Item1} users promoted, {promotedDemotedCounts.Item2} users demoted ({scoreCutoff}) next update: {TimeToEvaluate}");
    }

    private static async Task<(int, int)> UpdateAll(float cutoff)
    {
        int promoted = 0;
        int demoted = 0;

        await foreach (User? user in dbContext.Users.AsAsyncEnumerable())
        {
            if (user.Privileges == Privileges.Banned) continue;

            float score = Userscore.Score(user);

            switch (user.Privileges)
            {
                case < Privileges.Trusted when score >= cutoff:
                    user.Privileges = Privileges.Trusted;
                    promoted++;
                    continue;
                case Privileges.Trusted when score < cutoff:
                    user.Privileges = Privileges.None;
                    demoted++;
                    break;
            }
        }

        await dbContext.SaveChangesAsync();
        return (promoted, demoted);
    }
}
