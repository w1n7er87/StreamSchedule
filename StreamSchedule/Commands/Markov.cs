using StreamSchedule.Data;
using StreamSchedule.Markov2;

namespace StreamSchedule.Commands;

internal class Markov : Command
{
    public override string Call => "markov";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => "markov. nl online, o ordered, w weighted, c[value(1-75)] specify token count, f force no eol (will still stop if eol is the only next for last token) ";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Longer);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["o", "w", "c", "s", "l", "m", "f", "nl", "count"];
    public override List<string> Aliases { get; set; } = [];
    
    private static bool Muted = false;
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        try
        {
            string? word = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args).Split(' ').LastOrDefault(x => !string.IsNullOrEmpty(x));

            bool online = args.TryGetValue("nl", out _);
            
            if (args.TryGetValue("m", out _) && message.Sender.Privileges >= Privileges.Mod)
            {
                Muted = !Muted;
                return Task.FromResult(Utils.Responses.Ok);
            }
            if(Muted) return Task.FromResult(new CommandResult("uuh "));

            int count = args.TryGetValue("c", out string? cc)? int.TryParse(cc, out int ccc)? Math.Clamp(ccc, 1, 75) : 25 : 25;

            Method method = Method.random;

            if (args.TryGetValue("o", out _)) method = Method.ordered;

            if (args.TryGetValue("w", out _)) method = Method.weighted;

            if (args.TryGetValue("s", out _) && message.Sender.Privileges >= Privileges.Uuh)
                return Task.FromResult(Utils.Responses.Ok + $"saved in {Markov2.Markov.Save():s's 'fff'ms '}");

            if (args.TryGetValue("l", out _) && message.Sender.Privileges >= Privileges.Uuh)
                return Task.FromResult(Utils.Responses.Ok + $"loaded in {Markov2.Markov.Load():s's 'fff'ms '}");

            if (args.TryGetValue("count", out _))
                return Task.FromResult(new CommandResult($"{(online ? Markov2.Markov.TokenCountOnline : Markov2.Markov.TokenCount)} tokens {(online ? Markov2.Markov.TokenPairCountOnline : Markov2.Markov.TokenPairCount)} pairs "));

            string result = Markov2.Markov.GenerateSequence(word, count, method, args.TryGetValue("f", out _), online);
            return Task.FromResult(new CommandResult(result.Replace("\e", ""), requiresFilter: true));
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error(e);
            return Task.FromResult(Utils.Responses.Surprise);
        }
    }
}
