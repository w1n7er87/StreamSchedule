using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Talk : Command
{
    public override string Call => "test";
    public override Privileges Privileges => Privileges.Uuh;
    public override string Help => "eerm ";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.TwoMinutes);
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Content.Split(" ");
        float t = float.TryParse(split[0], out t)  ? t : 0.7f;
        string user = "";
        if (split.Length > 1) user = message.Content.Split(" ")[1];
        string result = LLM.Inference.Generate(user, temperature: t);
        BotCore.Nlog.Info($"t:{t}|u:{user}:\n{result}");
        return Task.FromResult(new CommandResult(result));
    }
}
