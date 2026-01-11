using StreamSchedule.Data;
using StreamSchedule.Markov2;

namespace StreamSchedule.Commands;

internal class Markov : Command
{
    public override string Call => "markov";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => "markov. o - ordered, w - weighted, c[value] tokens(1-75) (-l -s -m)";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Longer);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["o", "w", "c", "s", "l", "m", "count"];
    public override List<string> Aliases { get; set; } = [];
    
    private static bool Muted = false;
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        try
        {
            string? word = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args).Split(" ").LastOrDefault();

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
            {
                Markov2.Markov.Save();
                return Task.FromResult(Utils.Responses.Ok + "saved ");
            }

            if (args.TryGetValue("l", out _) && message.Sender.Privileges >= Privileges.Uuh)
            {
                Markov2.Markov.Load();
                return Task.FromResult(Utils.Responses.Ok + "loaded ");
            }

            if (args.TryGetValue("count", out _))
            {
                return Task.FromResult(new CommandResult($"{Markov2.Markov.TokenCount} tokens "));
            }

            string result = Markov2.Markov.GenerateSequence(word, count, method);
            BotCore.Nlog.Info($"markov query: {(int)method} {count} : {word}");
            BotCore.Nlog.Info(result.Replace("\e", ""));
            return Task.FromResult(new CommandResult(result.Replace("\e", ""), requiresFilter: true));
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error(e);
            return Task.FromResult(Utils.Responses.Surprise);
        }
    }
}
