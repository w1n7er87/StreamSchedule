using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Kill : Command
{
    public override string Call => "kill";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "kill the bot";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["fr"];
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);

        if (message.sender.Privileges == Privileges.Uuh && usedArgs.TryGetValue("fr", out _))
        {
            await BotCore.DBContext.SaveChangesAsync();

            Environment.Exit(0);

            return new CommandResult("buhbye ", false);
        }

        string target = text.Split(' ')[0];

        return Random.Shared.Next(100) > 25 ? new CommandResult("✋ unauthorized action. ", false) : new CommandResult($"MEGALUL 🔪 {target}", false, true);
    }
}
