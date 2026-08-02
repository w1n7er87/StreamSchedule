using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Talk : Command
{
    public override string Call => "streamschedule";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => "eerm ";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.TwoMinutes);
    public override string[] Arguments => ["t", "as", "b"];
    public override List<string> Aliases { get; set; } = [];
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string clean = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args);
        float t = args.TryGetValue("t", out string? tt) ? float.TryParse(tt, out t)  ? t : 0.4f : 0.4f;
        string user = args.TryGetValue("as", out string? u) ? u.ToLower() : "streamschedule";
        string result = LLM.Inference.Generate(user, $"@{user} {clean}", message.ID, automated: args.TryGetValue("b", out _), temperature: t);
        
        BotCore.Nlog.Info($"t:{t}|u:{user}:\n{result}");

        return Task.FromResult(new CommandResult(result, requiresFilter: true, reply: false));
    }
}
