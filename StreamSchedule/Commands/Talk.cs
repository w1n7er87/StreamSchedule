using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Talk : Command
{
    public override string Call => "streamschedule";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => "eerm ";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.TwoMinutes);
    public override string[] Arguments => ["t", "as", "b", "m"];
    public override List<string> Aliases { get; set; } = [];
    private static bool Muted = true;
    
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string clean = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args);
        
        if (args.TryGetValue("m", out _) && message.Sender.Privileges >= Privileges.Uuh)
        {
            Muted = !Muted;
            return Task.FromResult(Utils.Responses.Ok);
        }

        if (!LLM.Inference.AllGood) return Task.FromResult(Utils.Responses.Fail);
        
        float t = args.TryGetValue("t", out string? tt) ? float.TryParse(tt, out t)  ? t : 0.65f : 0.65f;
        string user = "";
        if (args.TryGetValue("as", out string? u))
        {
            u = u.ToLower();
            user = u.Equals("me") ? message.Sender.Username! : u;
        }
        else
        {
            user = "streamschedule";
        }
        
        string result = LLM.Inference.Generate(user, $"@{user} {clean}", message.ID, string.IsNullOrWhiteSpace(clean), automated: args.TryGetValue("b", out _), temperature: t);
        
        BotCore.Nlog.Info($"t:{t}|u:{user}:\n{result}");

        return Task.FromResult(new CommandResult(result, requiresFilter: true, reply: false));
    }
}
