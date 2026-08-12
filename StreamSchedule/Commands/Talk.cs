using StreamSchedule.Data;
using StreamSchedule.LLM;

namespace StreamSchedule.Commands;

internal class Talk : Command
{
    public override string Call => "talk";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => $"{(Muted ? " muted " : "")}have a chat a with real frontier AGI (t temp, as chatter, c context {Inference.minContext} - {Inference.maxContext}, l ask nicely for a longer message. )";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Longer);
    public override string[] Arguments => ["t", "as", "m", "c", "l"];
    public override List<string> Aliases { get; set; } = [];
    private static bool Muted = false;
    
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string clean = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args);

        if (args.TryGetValue("m", out _) && message.Sender.Privileges >= Privileges.Uuh)
        {
            Muted = !Muted;
            return Task.FromResult(Utils.Responses.Ok);
        }

        if (Muted) return Task.FromResult(new CommandResult(""));
        if (!Inference.AllGood) return Task.FromResult(Utils.Responses.Fail);

        float t = args.TryGetValue("t", out string? tt) ? float.TryParse(tt, out t)  ? t : 0.75f : 0.75f;
        t = float.Clamp(t, 0.01f, 3f);
        string user;
        
        if (args.TryGetValue("as", out string? u))
        {
            u = u.ToLower();
            user = u.Equals("me") ? message.Sender.Username! : u;
        }
        else
        {
            user = "streamschedule";
        }

        bool shrt = !args.TryGetValue("l", out _);

        int? contextSize = args.TryGetValue("c", out string? cc) ? int.TryParse(cc, out int ccc) ? ccc : null : null;
        string result = Inference.Answer(user, shrt, message.ID, clean, t, contextSize);
        BotCore.Nlog.Info(result);
        //return Task.FromResult(new CommandResult(result, requiresFilter: true, reply: false));
        return Task.FromResult(new CommandResult(""));
    }
}
