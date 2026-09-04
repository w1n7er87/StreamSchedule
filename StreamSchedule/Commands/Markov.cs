using StreamSchedule.Data;
using StreamSchedule.Markov2;

namespace StreamSchedule.Commands;

internal class Markov : Command
{
    public override string Call => "markov";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => $"{(Muted ? " muted " : "")}c count 1-{maxTokenCount}({defaultTokenCount}) k top-k sampling t temperature q seed r reverse i include f force try no eol";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.TwoMinutes);
    public override string[] Arguments => ["k", "t", "c", "m", "f", "q", "r", "i", "count", "load", "save", "dump", "trim"];
    public override List<string> Aliases { get; set; } = [];

    private static bool Muted = true;
    private const int maxTokenCount = 75;
    private const int defaultTokenCount = 12;

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        try
        {
            string? word = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args).Split(' ').LastOrDefault(x => !string.IsNullOrEmpty(x));

            if (args.TryGetValue("dump", out _) && message.Sender.Privileges >= Privileges.Uuh)
            {
                Markov2.Markov.DumpTokenStats(word ?? "", args.TryGetValue("r", out _));
                return Task.FromResult(Utils.Responses.Ok);
            }

            if (args.TryGetValue("trim", out _) && message.Sender.Privileges >= Privileges.Uuh)
            {
                Markov2.Markov.Cleanup();
                return Task.FromResult(Utils.Responses.Ok);
            }

            if (args.TryGetValue("m", out _) && message.Sender.Privileges >= Privileges.Uuh)
            {
                Muted = !Muted;
                return Task.FromResult(Utils.Responses.Ok);
            }

            if(Muted) return Task.FromResult(new CommandResult(""));
            
            float temperature = args.TryGetValue("t", out string? tt) ? float.TryParse(tt, out temperature)  ? temperature : 10f : 10f;
            
            int k = args.TryGetValue("k", out string? kk) ? int.TryParse(kk, out k)  ? k : 99 : 99;

            int count = args.TryGetValue("c", out string? cc)? int.TryParse(cc, out int ccc)? Math.Clamp(ccc, 1, maxTokenCount) : defaultTokenCount : defaultTokenCount;

            int? seed = args.TryGetValue("q", out string? qq) ? int.TryParse(qq, out int qqq) ? Math.Clamp(qqq, 0, int.MaxValue - 1) : null : null;
            
            Method method = Method.none;

            if (args.TryGetValue("r", out _)) method |= Method.reverse;

            if (args.TryGetValue("f", out _)) method |= Method.force;

            if (args.TryGetValue("i", out _)) method |= Method.include;
            
            if (args.TryGetValue("save", out _) && message.Sender.Privileges >= Privileges.Uuh)
                return Task.FromResult(Utils.Responses.Ok + $"saved in {Markov2.Markov.Save():s's 'fff'ms '}");
            
            if (args.TryGetValue("load", out _) && message.Sender.Privileges >= Privileges.Uuh)
                return Task.FromResult(Utils.Responses.Ok + $"loaded in {Markov2.Markov.Load():s's 'fff'ms '}");
            
            if (args.TryGetValue("count", out _))
                return Task.FromResult(new CommandResult($"{Markov2.Markov.TokenCount} tokens {Markov2.Markov.TokenPairCount} pairs "));

            string result = Markov2.Markov.GenerateSequence(word, maxLength: count, method: method, seed: seed, k: k, temperature: temperature);
            return Task.FromResult(new CommandResult(result, requiresFilter: true));
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error(e);
            return Task.FromResult(Utils.Responses.Surprise);
        }
    }
}
