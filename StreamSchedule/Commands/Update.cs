using StreamSchedule.Data;
using System.Diagnostics;

namespace StreamSchedule.Commands;

internal class Update : Command
{
    public override string Call => "updates";
    public override Privileges Privileges => Privileges.Uuh;
    public override string Help => "update the bot and restart";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        BotCore.OutQueuePerChannel[message.channelName].Enqueue(new CommandResult("📆 🛠️ ", reply: false));

        await BotCore.DBContext.SaveChangesAsync();
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c start \"\" \"update.bat\"",
            UseShellExecute = true
        });

        Environment.Exit(0);

        return new CommandResult("you are not supposed to read this MyHonestReaction ");
    }
}
