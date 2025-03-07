﻿using StreamSchedule.Data;
using System.Diagnostics;

namespace StreamSchedule.Commands;

internal class Update : Command
{
    internal override string Call => "updates";
    internal override Privileges MinPrivilege => Privileges.Uuh;
    internal override string Help => "update the bot and restart";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        BotCore.SendLongMessage(message.channelName, null, "📆 🛠️ ");

        BotCore.DBContext.SaveChanges();
        await Markov.Markov.SaveAsync();

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
