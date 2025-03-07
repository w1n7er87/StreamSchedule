using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Kill : Command
{
    internal override string Call => "kill";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "kill the bot: [time] (in seconds, optional)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["fr"];

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);

        if (message.sender.Privileges == Privileges.Uuh && usedArgs.TryGetValue("fr", out _))
        {
            await BotCore.DBContext.SaveChangesAsync();
            await Markov.Markov.SaveAsync();

            Environment.Exit(0);

            return new CommandResult("buhbye ", false);
        }

        string target = message.content.Split(' ')[0];

        if (Random.Shared.Next(100) > 25) return new CommandResult("✋ unauthorized action. ", false);

        return new CommandResult($"MEGALUL 🔪 {target}", false);
    }
}
