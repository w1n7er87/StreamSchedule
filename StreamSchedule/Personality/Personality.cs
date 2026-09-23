using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;
using StreamSchedule.Markov2;
using Markov = StreamSchedule.Markov2.Markov;
using Stream = StreamSchedule.Data.Models.Stream;

namespace StreamSchedule.Personality;

public static class Personality
{
    static Personality() { _ = new SaySomething(); }

    public static bool Start => true;
    private static bool _online = false;

    public static bool Online
    {
        private get => _online;
        set
        {
            _online = value;
            if (value) timeToSpeak = DateTime.UtcNow + OnlineInterval;
        }
    }

    private static TimeSpan OfflineInterval => new TimeSpan(hours: 0, minutes: 45 + Random.Shared.Next(-20, 15), seconds: Random.Shared.Next(32));
    private static TimeSpan OnlineInterval => new TimeSpan(hours: 0, minutes: Random.Shared.Next(5, 15), seconds: Random.Shared.Next(32));

    private static DateTime timeToSpeak = DateTime.UtcNow + TimeSpan.FromMinutes(5);
    private static readonly Func<Task<string[]>>[] actions = [SpeakOnTopic, SpeakOnTopic, SpeakOnTopic, HugLast, RemindSchedule, PingLurker];

    private sealed class SaySomething : Periodic
    {
        protected override async Task Update()
        {
            if (DateTime.UtcNow < timeToSpeak) return;
            string[] result;
            if (Online)
            {
                result = await SpeakOnTopic();
                timeToSpeak = DateTime.UtcNow + OnlineInterval;
            }
            else
            {
                result = await actions[Random.Shared.Next(actions.Length)]();
                timeToSpeak = DateTime.UtcNow + OfflineInterval;
            }

            BotCore.EnqueueMessage("vedal987", false, result.Select(r => new OutgoingMessage(r, null)).ToList());
            BotCore.Nlog.Info($"said {string.Join(", ", result.Select(r => $"\" {r} \""))}, next line at {timeToSpeak.ToLocalTime()} ");
        }
    }

    private static async Task<string[]> SpeakOnTopic()
    {
        string commonWord = BotCore.MessageCache.TakeLast(15).SelectMany(m => m.Message.Split(" ")).GroupBy(s => s).Select(g => new { word = g.Key, count = g.Count() }).OrderByDescending(g => g.count).FirstOrDefault()?.word ?? "uuh";

        return [Markov.GenerateSequence(commonWord, maxLength: Random.Shared.Next(4, 8), method: Method.force, temperature: 2f)];
    }

    private static async Task<string[]> RemindSchedule()
    {
        string[] responses = ["did yall know there is {0} today ", "yo there is {0} today ", "can't wait for today's {0} ", "thank god there is {0} today ", "so excited for {0} today ", "finally {0} today "];

        Stream? stream = BotCore.DBContext.Streams.FirstOrDefault(s => s.StreamDate == DateOnly.FromDateTime(DateTime.UtcNow));
        string the = "no stream";
        if (stream is not null) the = new DateTime(stream.StreamDate, stream.StreamTime) < DateTime.UtcNow ? the : stream.StreamTitle ?? the;

        return [string.Format(responses[Random.Shared.Next(responses.Length)], the)];
    }

    private static async Task<string[]> HugLast()
    {
        string username = BotCore.MessageCache.TakeLast(1).FirstOrDefault()?.Username ?? "uuh";
        return [$"{(Random.Shared.Next(101) >= 50 ? "HUGGIES " : "catKISS ")} {username} {Markov.GenerateSequence("Hey!", maxLength: 4, temperature: 2f)}"];
    }

    private static async Task<string[]> Talk()
    {
        List<string> names = BotCore.MessageCache.TakeLast(25).Select(m => m.Username).ToList();
        if (names.Count == 0 || !LLM.Inference.AllGood) return ["Awkward "];
        return LLM.Inference.Speak(names[Random.Shared.Next(names.Count)], 0.9f);
    }

    private static async Task<string[]> PingLurker()
    {
        string[] lines = ["sus i see you lurking over there {0} ", "WeirdDude can y'all believe that {0} is just reading chat and not typing anything? ", "uuh i see what you are doing over there {0} "];
        string[] starters = ["Fishinge let's see what do we have here . . . ", "uuh . . . ", "CaitThinking let's see . . . ", "", ""];

        (_, ChattersInfo? chatterGroups) = await GraphQLClient.GetChattersCount("85498365");

        string chatter;

        if (chatterGroups?.Viewers is null || chatterGroups.Viewers.Length == 0) chatter = "vedal987";
        else chatter = chatterGroups.Viewers[Random.Shared.Next(0, chatterGroups.Viewers.Length)]?.Login ?? "vedal987";
        chatter = $"@{chatter}";
        return [starters[Random.Shared.Next(starters.Length)], string.Format(lines[Random.Shared.Next(lines.Length)], chatter)];
    }
}
