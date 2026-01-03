using System.Diagnostics;
using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;

namespace StreamSchedule.Commands;

internal class HypeTracker : Command
{
    public override string Call => "hype";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => $"track current hype train with a summary at the end, -for[h] to specify duration in hours (default 2, max 12) ({MaxChannels} channels max) ";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["for"];
    public override List<string> Aliases { get; set; } = [];

    private static readonly List<HypeTrainMonitor> ActiveMonitors = [];
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);
    private const int MaxChannels = 10;
    
    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string? channelName = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args).Split(" ").FirstOrDefault();
        
        if (string.IsNullOrEmpty(channelName)) return Utils.Responses.Fail + "no username provided";
        if (ActiveMonitors.Count >= 10) return Utils.Responses.Fail + $"too many channels are currently being tracked {ActiveMonitors.Count} (max {MaxChannels})";
        
        User? targetUser = (await BotCore.API.Helix.Users.GetUsersAsync(logins:[channelName])).Users.FirstOrDefault();
        
        if (targetUser is null) return Utils.Responses.Fail + "user does not exist";
        if (targetUser.BroadcasterType is not ("affiliate" or "partner")) return Utils.Responses.Fail + "user is not a affiliate or partner";
        
        HypeTrainMonitor? alreadyExists = ActiveMonitors.FirstOrDefault(x => x.ChannelName.Equals(targetUser.Login));
        if (alreadyExists is not null) return Utils.Responses.Fail + $"already monitoring {targetUser.Login}, {Stopwatch.GetElapsedTime(alreadyExists.Started):hh'h 'mm'm '} left";
        
        TimeSpan duration = DefaultDuration;
        if (args.TryGetValue("for", out string? dd))
        {
            if (int.TryParse(dd, out int ddd))
            {
                duration = TimeSpan.FromHours(Math.Clamp(ddd, 0, 12));
            }
        }

        HypeTrainMonitor newMonitor = new (Stopwatch.GetTimestamp(), duration, targetUser.Login, message.Sender.Username!, message.ChannelName);
        
        ActiveMonitors.Add(newMonitor);
        Task.Run(() => Monitor(newMonitor));
        
        BotCore.Nlog.Info($"monitoring {targetUser.Login} for {duration:hh'h 'mm'm '}");
        return Utils.Responses.Ok + $"monitoring {targetUser.Login} for {duration:hh'h 'mm'm '}";
    }

    private static async Task Monitor(HypeTrainMonitor monitor)
    {
        HypeTrain? tracked = null;
        TimeSpan interval = TimeSpan.FromSeconds(5);
        int failCount = 0;
        
        while (true)
        {
            try
            {
                if (Stopwatch.GetElapsedTime(monitor.Started) > monitor.Duration && tracked is null)
                {
                    ActiveMonitors.Remove(monitor);
                    BotCore.Nlog.Info($"{monitor.ChannelName} monitor has expired after {Stopwatch.GetElapsedTime(monitor.Started):hh'h 'mm'm '}");
                    BotCore.OutQueuePerChannel[monitor.OutputChannelName].Enqueue(new CommandResult($"hype train monitor for @{monitor.ChannelName} has expired after {Stopwatch.GetElapsedTime(monitor.Started):hh'h 'mm'm '} @{monitor.RequestedBy}"));
                    return;
                }

                await Task.Delay(interval);

                HypeTrain? current = await GraphQLClient.GetHypeTrain(monitor.ChannelName);

                if (tracked?.Execution is null)
                {
                    if (current?.Execution is not null)
                    {
                        tracked = current;
                        BotCore.Nlog.Info($"{monitor.ChannelName} hype train started, tracking it now");
                        BotCore.OutQueuePerChannel[monitor.OutputChannelName].Enqueue(new CommandResult($"active hype train in @{monitor.ChannelName}  @{monitor.RequestedBy}"));
                    }
                    continue;
                }

                if (current?.Execution is null)
                {
                    string summary = $"hype train in @{monitor.ChannelName} has ended: {HypeTrainSummary(tracked)} @{monitor.RequestedBy}";
                    ActiveMonitors.Remove(monitor);
                    BotCore.Nlog.Info(summary);
                    BotCore.OutQueuePerChannel[monitor.OutputChannelName].Enqueue(new CommandResult(summary));
                    return;
                }

                if (current.Approaching is not null)
                {
                    double timeLeft = (current.Approaching.ExpiresAt - DateTime.UtcNow)?.TotalSeconds ?? 11;
                    interval = timeLeft <= 10 ? TimeSpan.FromMilliseconds(500) : interval;
                }
                
                if (current.Execution is not null)
                {
                    double timeLeft = (current.Execution.ExpiresAt - DateTime.UtcNow)?.TotalSeconds ?? 11;
                    interval = timeLeft <= 10 ? TimeSpan.FromMilliseconds(500) : interval;
                }
                tracked = current;
            }
            catch(Exception e)
            {
                failCount++;
                BotCore.Nlog.Error($"error while updating monitor for {monitor.ChannelName}: \n {e} \n waiting for {10 * failCount}s");
                if (failCount > 5)
                {
                    ActiveMonitors.Remove(monitor);
                    BotCore.Nlog.Info($"removing monitor for {monitor.ChannelName} after 5 failed attempts ");
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(10 * failCount));
            }
        }
    }
    
    private static float GetPercentage(int? currentProgress, int? goal) => MathF.Round((currentProgress ?? 1.0f) / (goal ?? 1.0f) * 100.0f, 4);

    private static string HypeTrainSummary(HypeTrain train)
    {
        if (train.Approaching is not null)
        {
            string events = train.Approaching.EventsRemaining?.FirstOrDefault()?.Events.ToString() ?? "0";
            string isKappaApproaching = train.Approaching.IsGoldenKappaTrain ?? false ? "golden Kappa" : "";
            string isTreasureApproaching = train.Approaching.IsTreasureTrain ?? false ? "treasure" : "";

            return $"{isKappaApproaching} {isTreasureApproaching} hype train was approaching and failed to start with {events} left";
        }

        if (train.Execution is null) return " train is null huh ";
        
        string isKappa = train.Execution.IsGoldenKappaTrain ?? false ? "golden Kappa " : "";
        string isTreasure = train.Execution.IsTreasureTrain ?? false ? "treasure " : "";

        float progress = GetPercentage(train.Execution.Progress?.Progression, train.Execution.Progress?.Goal);
        string hypeTrain = $"lvl {train.Execution.Progress?.Level?.Value ?? 0} {Helpers.HypeTrainDifficultyToString(train.Execution.Config?.Difficulty)} {isTreasure}{isKappa}hype train - {progress}%";
        hypeTrain += $" ( record: lvl {train.Execution.AllTimeHigh?.Level?.Value ?? 0} {GetPercentage(train.Execution.AllTimeHigh?.Progression, train.Execution.AllTimeHigh?.Goal)}% ) ";

        string shared = "";
        if (train.Execution.SharedHypeTrainDetails is not null)
        {
            shared = "shared contribution: ";
            shared += string.Join(", ", train.Execution.SharedHypeTrainDetails.SharedProgress?.Select(x => $"{x?.User?.Login} - {GetPercentage(x?.ChannelProgress?.Total, train.Execution.Progress?.Total)}%") ?? []);
            shared += $". ( shared record: lvl {train.Execution.SharedHypeTrainDetails.SharedAllTimeHighRecords?[0]?.ChannelAllTimeHigh?.Level?.Value ?? 0} {GetPercentage(train.Execution.SharedHypeTrainDetails.SharedAllTimeHighRecords?[0]?.ChannelAllTimeHigh?.Progression, train.Execution.SharedHypeTrainDetails.SharedAllTimeHighRecords?[0]?.ChannelAllTimeHigh?.Goal)}% ) ";
        }

        string treasureDetails = "";
        if (train.Execution.TreasureTrainDetails is not null)
            treasureDetails = $" ( discount {train.Execution.TreasureTrainDetails.DiscountPercentage}%, starts at lvl{train.Execution.TreasureTrainDetails.DiscountLevelThreshold} ) ";

        hypeTrain += treasureDetails;
        hypeTrain += shared;
        hypeTrain += string.Join("; ", train.Execution.Participations?.Select(x => x?.ToString() ?? "") ?? []);

        return hypeTrain;
    }

    private record HypeTrainMonitor(long Started, TimeSpan Duration, string ChannelName, string RequestedBy, string OutputChannelName);

}
