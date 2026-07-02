using StreamSchedule.Data;
using StreamSchedule.Markov2;
using Markov = StreamSchedule.Markov2.Markov;

namespace StreamSchedule.Personality;

public static class Personality
{
    static Personality()
    {
        _ = new SaySomething();
    }

    public static bool Start => true;
    private static bool _online = false;
    public static bool Online
    {
        private get => _online; 
        set
        {
            _online = value;
            if (value)
            {
                timeToSpeak = DateTime.UtcNow + OnlineInterval;
            }
        }
    }
    
    private static TimeSpan OfflineInterval => new TimeSpan(hours: 1, minutes: Random.Shared.Next(-15, 45), seconds: Random.Shared.Next(32));
    private static TimeSpan OnlineInterval => new TimeSpan(hours: 0, minutes: Random.Shared.Next(5, 45), seconds: Random.Shared.Next(32));

    private static DateTime timeToSpeak = DateTime.UtcNow + TimeSpan.FromMinutes(5);
    private static readonly Func<string>[] actions = [SpeakOnTopic, RemindSchedule];

    private sealed class SaySomething : Periodic
    {
        protected override Task Update()
        {
            if (DateTime.UtcNow < timeToSpeak) return Task.CompletedTask;
            string result;
            if (Online)
            {
                result = SpeakOnTopic();
                timeToSpeak = DateTime.UtcNow + OnlineInterval;
            }
            else
            {
                result = actions[Random.Shared.Next(actions.Length)]();
                timeToSpeak = DateTime.UtcNow + OfflineInterval;
            }
            BotCore.OutQueuePerChannel["vedal987"].Enqueue(new OutgoingMessage(new CommandResult(result, requiresFilter:true), null));
            BotCore.Nlog.Info($"said \"{result}\", next line in {timeToSpeak - DateTime.UtcNow} ");
            return Task.CompletedTask;
        }
    }

    private static string SpeakOnTopic()
    {
        string commonWord = BotCore.MessageCache
            .TakeLast(25)
            .SelectMany(m => m.Message.Split(" "))
            .GroupBy(s => s)
            .Select(g => new {word = g.Key, count = g.Count()})
            .OrderByDescending(g => g.count)
            .FirstOrDefault()?.word ?? "uuh";

        return Markov.GenerateSequence(commonWord, Random.Shared.Next(2, 15), Method.ordered);
    }

    private static string RemindSchedule()
    {
        string[] responses =
        [
            "did yall know there is {0} today ",
            "yo there is {0} today ",
            "can't wait for {0} today ",
            "thank god there is {0} today ",
            "so excited for {0} today ",
            "finally {0} today "
        ];
        
        string the = BotCore.DBContext.Streams.FirstOrDefault(s => s.StreamDate == DateOnly.FromDateTime(DateTime.UtcNow))?.StreamTitle ?? "no stream";
        return string.Format(responses[Random.Shared.Next(responses.Length)], the);
    }
}
