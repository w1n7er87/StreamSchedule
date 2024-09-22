using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class BasicTextCommand : Command
{
    internal override string Call => "cmd";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "manage simple text commands: -add/-rm (-p[priv] optional) [command name](required) [command content](required) ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["add", "rm", "p"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        List<TextCommand> commands = [.. BotCore.DBContext.TextCommands];
        string text = Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> usedArguments);
        string commandName = text.Split(' ')[0].ToLower();
        text = text[commandName.Length..];
        Console.WriteLine(text);
        if (string.IsNullOrEmpty(commandName)) return Task.FromResult(Utils.Responses.Fail + (" no command name provided "));

        if (commandName.Length < 2) return Task.FromResult(Utils.Responses.Fail + (" command name should be 2 characters or longer "));

        if (usedArguments.TryGetValue("add", out _))
        {
            if (string.IsNullOrEmpty(text)) return Task.FromResult(Utils.Responses.Fail + (" no content provided "));

            if (commands.Count > 0)
            {
                if (commands.FirstOrDefault(x => x.Name.Equals(commandName)) is not null || BotCore.CurrentCommands.Any(x => x?.Call == commandName))
                {
                    return Task.FromResult(Utils.Responses.Fail + " command with this name already exists. ");
                }
                else
                {
                    Privileges p = usedArguments.TryGetValue("p", out string? pp) ? PrivilegesConversion.ParsePrivilege(pp) : Privileges.None;
                    BotCore.DBContext.TextCommands.Add(new() { Name = commandName, Content = text, Privileges = p });
                    BotCore.DBContext.SaveChanges();
                    return Task.FromResult(Utils.Responses.Ok);
                }
            }
            else
            {
                Privileges p = usedArguments.TryGetValue("p", out string? pp) ? PrivilegesConversion.ParsePrivilege(pp) : Privileges.None;
                BotCore.DBContext.TextCommands.Add(new() { Name = commandName, Content = text, Privileges = p });
                BotCore.DBContext.SaveChanges();
                return Task.FromResult(Utils.Responses.Ok);
            }
        }
        else if (usedArguments.TryGetValue("rm", out _))
        {
            if (commands.Count <= 0)
            {
                return Task.FromResult(Utils.Responses.Fail + " there are no custom commands. ");
            }
            TextCommand? c = BotCore.DBContext.TextCommands.FirstOrDefault(x => x.Name == commandName);
            if (c is null)
            {
                return Task.FromResult(Utils.Responses.Fail + " there is no command with this name. ");
            }
            BotCore.DBContext.TextCommands.Remove(c);
            BotCore.DBContext.SaveChanges();
            return Task.FromResult(Utils.Responses.Ok);
        }
        return Task.FromResult(Utils.Responses.Surprise);
    }
}
