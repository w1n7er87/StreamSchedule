using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class AllowOnline : Command
{
    public override string Call => "toggleonline";
    public override Privileges Privileges => Privileges.Mod;
    public override string Help => "allow bot to be used while the stream is live";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        BotCore.AllowedOnline = !BotCore.AllowedOnline;
        return Task.FromResult(Utils.Responses.Ok + $"will now{(BotCore.AllowedOnline? " " : " not ")}respond to commands when live. ");
    }
}
