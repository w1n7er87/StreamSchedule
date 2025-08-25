using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NLog;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using StreamSchedule.WebExport;

namespace StreamSchedule;

public static class Program
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => logger.Fatal(e.ExceptionObject.ToString());

        if (EF.IsDesignTime)
        {
            new HostBuilder().Build().Run();
            return;
        }

        try
        {
            string pagesDBLocation = Environment.GetEnvironmentVariable("STREAM_SCHEDULE_PAGES", EnvironmentVariableTarget.User) ?? "";

            int[] channelIDs = [85498365, 78135490, 871501999];

            DatabaseContext dbContext = new(new DbContextOptionsBuilder<DatabaseContext>().UseSqlite("Data Source=StreamSchedule.data").Options);
            dbContext.Database.EnsureCreated();

            PagesContext pagesContext = new(new DbContextOptionsBuilder<PagesContext>().UseSqlite($"Data Source={pagesDBLocation}").Options);

            List<User> JoinedUsers = [];
            JoinedUsers.AddRange(channelIDs.Select(id => dbContext.Users.Find(id)!));
            BotCore.Init(JoinedUsers, dbContext, logger, pagesContext);
            Console.ReadLine();
        }
        catch (Exception e)
        {
            logger.Error(e, e.ToString());
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}
