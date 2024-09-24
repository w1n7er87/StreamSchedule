using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class CommandManagement : Command
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

        Privileges privileges = usedArguments.TryGetValue("p", out string? pp) ? PrivilegesConversion.ParsePrivilege(pp) : Privileges.None;

        if (string.IsNullOrEmpty(commandName)) return Task.FromResult(Utils.Responses.Fail + (" no command name provided "));
        if (commandName.Length < 2) return Task.FromResult(Utils.Responses.Fail + (" command name should be 2 characters or longer "));
        if (usedArguments.TryGetValue("add", out _)) return Task.FromResult(new CommandResult(AddCommand(commandName, text, privileges, commands)));
        if (usedArguments.TryGetValue("rm", out _)) return Task.FromResult(new CommandResult(RemoveCommand(commandName, commands)));

        return Task.FromResult(Utils.Responses.Surprise);
    }

    private static string AddCommand(string commandName, string content, Privileges privileges, in List<TextCommand> commands)
    {
        if (string.IsNullOrEmpty(content)) return (Utils.Responses.Fail + " no content provided ").ToString();

        if (commands.FirstOrDefault(x => x.Name.Equals(commandName)) is not null || BotCore.CurrentCommands.Any(x => x?.Call == commandName))
        { return (Utils.Responses.Fail + " command with this name already exists. ").ToString(); }

        BotCore.DBContext.TextCommands.Add(new() { Name = commandName, Content = content, Privileges = privileges });
        BotCore.DBContext.SaveChanges();
        return $"{Utils.Responses.Ok} added command \"{commandName}\" for {PrivilegesConversion.PrivilegeToString(privileges)}";
    }

    private static string RemoveCommand(string commandName, in List<TextCommand> commands)
    {
        if (commands.Count <= 0) return (Utils.Responses.Fail + " there are no custom commands. ").ToString();

        TextCommand? c = BotCore.DBContext.TextCommands.FirstOrDefault(x => x.Name == commandName);

        if (c is null) return (Utils.Responses.Fail + " there is no command with this name. ").ToString();

        BotCore.DBContext.TextCommands.Remove(c);
        BotCore.DBContext.SaveChanges();
        return Utils.Responses.Ok.ToString();
    }
}
