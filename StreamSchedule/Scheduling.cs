using Quartz;
using Quartz.Impl;
using StreamSchedule.Data.Models;
using StreamSchedule.Jobs;

namespace StreamSchedule;

internal static class Scheduling
{
    private static IScheduler _scheduler;
    private static List<User> Channels = [];

    public static async void Init(List<User> channels)
    {
        Channels = channels;
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        StdSchedulerFactory factory = new();
        _scheduler = await factory.GetScheduler();
        await _scheduler.Start();
        StartRecurringJobs();
    }

    private static async void StartRecurringJobs()
    {
        IJobDetail globalEmoteMonitorJob = JobBuilder.Create<GlobalEmoteMonitor>()
            .WithIdentity("globalEmoteMonitor")
            .UsingJobData("FirstRun", true)
            .Build();
        ITrigger globalEmoteMonitorTrigger = TriggerBuilder.Create()
            .WithIdentity("globalEmoteMonitor")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever())
            .Build();
        await _scheduler.ScheduleJob(globalEmoteMonitorJob, globalEmoteMonitorTrigger);

        Dictionary<IJobDetail, IReadOnlyCollection<ITrigger>> jobs = [];

        foreach (var channel in Channels)
        {
            var triggers = new HashSet<ITrigger>();
            var jobInstance = JobBuilder.Create<ChannelEmoteMonitor>()
                .WithIdentity(channel.Id.ToString(), "channelEmoteMonitor")
                .UsingJobData("UserID", channel.Id.ToString())
                .UsingJobData("Username", channel.Username)
                .UsingJobData("FirstRun", true)
                .Build();

            var jobTrigger = TriggerBuilder.Create()
                .WithIdentity(channel.Id.ToString(), "channelEmoteMonitor")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever())
                .Build();

            triggers.Add(jobTrigger);
            jobs[jobInstance] = triggers;
        }
        await _scheduler.ScheduleJobs(jobs, true);
    }

    private static async void OnProcessExit(object? sender, EventArgs? e)
    {
        await _scheduler.Shutdown();
    }
}
