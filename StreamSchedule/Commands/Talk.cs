using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Talk : Command
{
    public override string Call => "talk";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => "eerm ";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Longer);
    public override string[] Arguments => ["t", "as", "m", "c"];
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
        if (!LLM.Inference.AllGood) return Task.FromResult(Utils.Responses.Fail);

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
        
        int? contextSize = args.TryGetValue("c", out string? cc) ? int.TryParse(cc, out int ccc) ? ccc : null : null;
        string result = LLM.Inference.Answer(user, message.ID, clean, t, contextSize);
        return Task.FromResult(new CommandResult(result, requiresFilter: true, reply: false));
    }
}
