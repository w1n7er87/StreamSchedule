using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Kill : Command
{
    internal override string Call => "kill";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "kill the bot: [time] (in seconds, optional)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["frfr"];

    private async Task KillTask(TimeSpan delay)
    {
        await Task.Delay(delay);
        await BotCore.DBContext.SaveChangesAsync();
        Environment.Exit(0);
    }

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments!, message.content, out Dictionary<string, string> usedArgs);

        if (message.sender.Privileges == Privileges.Uuh && usedArgs.TryGetValue("frfr", out _))
        {
            string[] split = text.Split(' ');
            int duration = 1;
            if (split.Length > 0)
            {
                _ = int.TryParse(split[0], out duration);
            }
            TimeSpan delay = TimeSpan.FromSeconds(Math.Min(1, duration));
            Task.Run(() => KillTask(delay));
            return Task.FromResult(new CommandResult("buhbye ", false));
        }

        string target = message.content.Split(' ')[0];

        if (Random.Shared.Next(101) > 15) return Task.FromResult(new CommandResult("✋ unauthorized action. ", false));

        return Task.FromResult(new CommandResult($"MEGALUL 🔪 {target}", false));
    }
}
