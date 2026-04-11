using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class CheckCD : Command
{
    public override string Call => "checkcd";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "check cooldown [command] (username)";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];
    
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Content.Split(' ');
        if (split.Length == 0)
            return Task.FromResult(Utils.Responses.Fail + "no command provided");

        User targetUser = (split.Length > 1) ? User.TryGetUser(username: split[1], out targetUser) ? targetUser : message.Sender : message.Sender;
        ICommand? command = Commands.AllCommands.FirstOrDefault(x => x.Call == split[0] || x.Aliases.Contains(split[0]));
        
        if (command is null)
            return Task.FromResult(Utils.Responses.Fail + "no such command");

        if (!command.PersonalCooldowns.TryGetValue(targetUser.Id, out Cooldown? cooldown))
            return Task.FromResult(new CommandResult($"base cooldown : {ReadableCooldown(command.Cooldown)}"));
        
        return Task.FromResult(new CommandResult(
            $"{targetUser.Username}'s cooldown for {command.Call} : {ReadableCooldown(cooldown.currentCooldown)} " +
            $"expires in {ReadableCooldown(cooldown.ExpiresAtUtc - DateTime.UtcNow)}, " +
            $"reset in {ReadableCooldown(cooldown.ResetAtUtc - DateTime.UtcNow)} "));
    }

    private static string ReadableCooldown(TimeSpan timeSpan) 
        => $"{(timeSpan < TimeSpan.Zero ? "-" : "") }{(timeSpan.Days != 0 ? $"{Math.Abs(timeSpan.Days)}d " : "")}{(timeSpan.Hours != 0 ? $"{Math.Abs(timeSpan.Hours)}h " : "")}{(timeSpan.Minutes != 0 ? $"{Math.Abs(timeSpan.Minutes)}m " : "")}{(timeSpan.Seconds != 0 ? $"{Math.Abs(timeSpan.Seconds)}.{timeSpan:fff}s " : $"0.{timeSpan:fff}s")}";
    
}
