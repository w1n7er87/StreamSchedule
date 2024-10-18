using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Kill : Command
{
    internal override string Call => "kill";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "kill the bot: [time] (in seconds, optional)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    private async Task KillTask(TimeSpan delay)
    {
        BotCore.DBContext.SaveChanges();
        await Task.Delay(delay);
        Environment.Exit(0);
    }

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        if (message.Privileges < Privileges.Uuh)
        {
            string target = message.Message.Split(' ')[0];

            if(Random.Shared.Next(int.Parse(message.UserId)) > 15)
            {
                return Task.FromResult(new CommandResult("✋ unauthorized action. "));
            }
            else
            {
                return Task.FromResult(new CommandResult($"MEGALUL {target}"));
            }
        }

        string[] split = message.Message.Split(' ');
        int duration = 1;
        if (split.Length > 0)
        {
            _ = int.TryParse(split[0], out duration);
        }
        TimeSpan delay = TimeSpan.FromSeconds(Math.Min(1, duration));
        Task.Run(() => KillTask(delay));
        return Task.FromResult(new CommandResult("buhbye ", false));
    }
}
