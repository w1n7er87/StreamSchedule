using StreamSchedule.Data;

namespace StreamSchedule.Commands;

/// <summary>
/// surely there will no problems if bot shuts down during db operations (actually Clueless )
/// </summary>
internal class Kill : Command
{
    internal override string Call => "kill";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "kill the bot: [time] (in seconds, optional)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(1.1);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    private async Task KillTask(TimeSpan delay)
    {
        await Task.Delay(delay);
        Environment.Exit(0);
    }

    internal override Task<string> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        int duration = 1;
        if (split.Length > 0)
        {
            _ = int.TryParse(split[0], out duration);
        }
        TimeSpan delay = TimeSpan.FromSeconds(Math.Min(1, duration));
        Task.Run(() => KillTask(delay));
        return Task.FromResult("buhbye ");
    }
}
