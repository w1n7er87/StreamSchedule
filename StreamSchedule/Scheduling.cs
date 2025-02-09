using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using StreamSchedule.Data.Models;
using StreamSchedule.Jobs;

namespace StreamSchedule;

internal static class Scheduling
{
    private static IScheduler _scheduler;
    private static List<EmoteMonitorChannel> Channels = [];

    public static async void Init()
    {
        LogProvider.IsDisabled = true;
        Channels = BotCore.DBContext.EmoteMonitorChannels.Any(x => x.Deleted == false) ? [.. BotCore.DBContext.EmoteMonitorChannels] : [];
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        StdSchedulerFactory factory = new();
        _scheduler = await factory.GetScheduler();
        await _scheduler.Start();
        StartRecurringJobs();
    }

    public static async void RemoveEmoteMonitor(EmoteMonitorChannel channel)
    {
        await _scheduler.UnscheduleJob(new TriggerKey(channel.ChannelID.ToString(), "channelEmoteMonitor"));
    }

    public static async void StartNewChannelMonitorJob(EmoteMonitorChannel channel)
    {
        IJobDetail newEmoteMonitorJob = JobBuilder.Create<ChannelEmoteMonitor>()
            .WithIdentity(channel.ChannelID.ToString(), "channelEmoteMonitor")
            .UsingJobData("UserID", channel.ChannelID.ToString())
            .UsingJobData("OutputChannelName", channel.OutputChannelName)
            .UsingJobData("Username", channel.ChannelName)
            .UsingJobData("FirstRun", true)
            .UsingJobData("PingList", ' ' + string.Join(" ", channel.UpdateSubscribers.Select(x => '@' + x)))
            .Build();

        ITrigger newEmoteMonitorJobTrigger = TriggerBuilder.Create()
            .WithIdentity(channel.ChannelID.ToString(), "channelEmoteMonitor")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever())
            .Build();
        await _scheduler.ScheduleJob(newEmoteMonitorJob, newEmoteMonitorJobTrigger);
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
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever())
            .Build();
        await _scheduler.ScheduleJob(globalEmoteMonitorJob, globalEmoteMonitorTrigger);

        BotCore.Nlog.Info("started global emote monitor job");

        Dictionary<IJobDetail, IReadOnlyCollection<ITrigger>> jobs = [];

        foreach (var channel in Channels)
        {
            var triggers = new HashSet<ITrigger>();
            var jobInstance = JobBuilder.Create<ChannelEmoteMonitor>()
                .WithIdentity(channel.ChannelID.ToString(), "channelEmoteMonitor")
                .UsingJobData("UserID", channel.ChannelID.ToString())
                .UsingJobData("OutputChannelName", channel.OutputChannelName)
                .UsingJobData("Username", channel.ChannelName)
                .UsingJobData("FirstRun", true)
                .UsingJobData("PingList", ' ' + string.Join(" ", channel.UpdateSubscribers.Select(x => '@' + x)))
                .Build();

            var jobTrigger = TriggerBuilder.Create()
                .WithIdentity(channel.ChannelID.ToString(), "channelEmoteMonitor")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(90 + Random.Shared.Next(-30, 15)).RepeatForever())
                .Build();

            triggers.Add(jobTrigger);
            jobs[jobInstance] = triggers;
            BotCore.Nlog.Info($"created job for emote monitor {channel.ChannelName}");
        }
        await _scheduler.ScheduleJobs(jobs, true);
        BotCore.Nlog.Info("started emote monitor jobs");
    }

    private static async void OnProcessExit(object? sender, EventArgs? e)
    {
        await _scheduler.Shutdown();
    }
}
