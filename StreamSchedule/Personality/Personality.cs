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
            if (value)timeToSpeak = DateTime.UtcNow + OnlineInterval;
        }
    }
    
    private static TimeSpan OfflineInterval => new TimeSpan(hours: 0, minutes: 45 + Random.Shared.Next(-20, 15), seconds: Random.Shared.Next(32));
    private static TimeSpan OnlineInterval => new TimeSpan(hours: 0, minutes: Random.Shared.Next(5, 15), seconds: Random.Shared.Next(32));

    private static DateTime timeToSpeak = DateTime.UtcNow + TimeSpan.FromMinutes(5);
    private static readonly Func<string>[] actions = [SpeakOnTopic, SpeakOnTopic, SpeakOnTopic, HugLast, RemindSchedule, Talk, Talk, Talk, Talk];

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
            BotCore.Nlog.Info($"said \"{result}\", next line at {timeToSpeak.ToLocalTime()} ");
            return Task.CompletedTask;
        }
    }

    private static string SpeakOnTopic()
    {
        string commonWord = BotCore.MessageCache
            .TakeLast(15)
            .SelectMany(m => m.Message.Split(" "))
            .GroupBy(s => s)
            .Select(g => new {word = g.Key, count = g.Count()})
            .OrderByDescending(g => g.count)
            .FirstOrDefault()?.word ?? "uuh";

        return Markov.GenerateSequence(commonWord, Random.Shared.Next(4, 8), Method.ordered | Method.force);
    }

    private static string RemindSchedule()
    {
        string[] responses =
        [
            "did yall know there is {0} today ",
            "yo there is {0} today ",
            "can't wait for today's {0} ",
            "thank god there is {0} today ",
            "so excited for {0} today ",
            "finally {0} today "
        ];
        
        var stream = BotCore.DBContext.Streams.FirstOrDefault(s => s.StreamDate == DateOnly.FromDateTime(DateTime.UtcNow));
        string the = "no stream";
        if (stream is not null) the = stream.StreamDate.ToDateTime(stream.StreamTime) < DateTime.UtcNow ? the : stream.StreamTitle ?? the;

        return string.Format(responses[Random.Shared.Next(responses.Length)], the);
    }

    private static string HugLast()
    {
        string username = BotCore.MessageCache.TakeLast(1).FirstOrDefault()?.Username ?? "uuh";
        return $"{(Random.Shared.Next(101) >= 50? "HUGGIES " : "catKISS ")} {username} {Markov.GenerateSequence("Hey!",4)}";
    }

    private static string Talk()
    {
        List<string> names = BotCore.MessageCache.TakeLast(25).Select(m => m.Username).ToList();
        if (names.Count == 0) return "Awkward ";
        return LLM.Inference.Speak(names[Random.Shared.Next(names.Count)], 0.9f);
    }
}
