using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class CommandManagement : Command
{
    internal override string Call => "cmd";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "manage simple text commands: -add/-rm (-p[priv] optional) [command name](required) [command content](required)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["add", "rm", "p", "alias"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        List<TextCommand> commands = [.. BotCore.DBContext.TextCommands];

        string text = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArguments);
        string commandName = text.Split(' ')[0].ToLower();
        text = text[commandName.Length..].TrimStart();

        Privileges privileges = usedArguments.TryGetValue("p", out string? pp) ? PrivilegeUtils.ParsePrivilege(pp) : Privileges.None;

        if (string.IsNullOrEmpty(commandName)) return Task.FromResult(Utils.Responses.Fail + " no command name provided ");
        if (commandName.Length < 2) return Task.FromResult(Utils.Responses.Fail + " command name should be 2 characters or longer ");

        if (usedArguments.TryGetValue("add", out _))
        {
            return Task.FromResult(usedArguments.TryGetValue("alias", out _)
                ? AddAlias(commandName, text, ref commands)
                : AddCommand(commandName, text, privileges));
        }

        if (usedArguments.TryGetValue("rm", out _))
        {
            return Task.FromResult(usedArguments.TryGetValue("alias", out _)
                ? RemoveAlias(commandName, text, ref commands)
                : RemoveCommand(commandName, commands));
        }

        return Task.FromResult(Utils.Responses.Surprise);
    }

    private static CommandResult AddCommand(string commandName, string content, Privileges privileges)
    {
        if (string.IsNullOrEmpty(content)) return Utils.Responses.Fail + " no content provided ";

        if (!Commands.IsNameAvailable(commandName)) return Utils.Responses.Fail + " command with this name/alias already exists. ";

        TextCommand newCommand = new() { Name = commandName, Content = content, Privileges = privileges };

        Commands.CurrentTextCommands.Add(newCommand);
        BotCore.DBContext.TextCommands.Add(newCommand);
        BotCore.DBContext.SaveChanges();

        return Utils.Responses.Ok + $"added command \" {commandName} \" for {PrivilegeUtils.PrivilegeToString(privileges)}";
    }

    private static CommandResult RemoveCommand(string commandName, in List<TextCommand> commands)
    {
        if (commands.Count <= 0) return Utils.Responses.Fail + " there are no custom commands. ";

        TextCommand? c = commands.FirstOrDefault(x => x.Name.ToLower() == commandName);

        if (c is null) return Utils.Responses.Fail + $"there is no \" {commandName} \" command ";

        Commands.CurrentTextCommands.Remove(c);
        BotCore.DBContext.TextCommands.Remove(c);
        BotCore.DBContext.SaveChanges();

        return Utils.Responses.Ok;
    }

    private static CommandResult AddAlias(string commandName, string alias, ref List<TextCommand> textCommands)
    {
        if (!Commands.IsNameAvailable(alias)) return Utils.Responses.Fail + " command with this name/alias already exists. "; 

        if (textCommands.Any(x => x.Name == commandName))
        {
            var c = textCommands.First(x => x.Name == commandName);
            c.Aliases ??= [];
            c.Aliases.Add(alias);
            Commands.AddAlias(alias);
            BotCore.DBContext.SaveChanges();
            return Utils.Responses.Ok + $"added \" {alias} \" as alias for {c.Name} command";
        }

        if (Commands.CurrentCommands.Any(x => x.Call == commandName))
        {
            var c = BotCore.DBContext.CommandAliases.First(x => x.CommandName.ToLower() == commandName);
            c.Aliases ??= [];
            c.Aliases.Add(alias);
            Commands.AddAlias(alias);
            BotCore.DBContext.SaveChanges();
            return Utils.Responses.Ok + $"added \" {alias} \" as alias for {c.CommandName} command";
        }
        
        return Utils.Responses.Fail + $"there is no \" {commandName} \" command ";
    }

    private static CommandResult RemoveAlias(string commandName, string alias, ref List<TextCommand> textCommands)
    {
        if (textCommands.Any(x => x.Name == commandName))
        {
            var c = textCommands.First(x => x.Name == commandName);
            if (c.Aliases is null) return Utils.Responses.Fail + $" {commandName} has no aliases ";
            c.Aliases.Remove(alias);
            Commands.RemoveAlias(alias);
            BotCore.DBContext.SaveChanges();
            return Utils.Responses.Ok + $"removed \" {alias} \" as alias for {c.Name} command";
        }

        if (Commands.CurrentCommands.Any(x => x.Call == commandName))
        {
            var c = BotCore.DBContext.CommandAliases.First(x => x.CommandName == commandName);
            if (c.Aliases is null) return Utils.Responses.Fail + $" {commandName} has no aliases ";
            c.Aliases.Remove(alias);
            Commands.RemoveAlias(alias);
            BotCore.DBContext.SaveChanges();
            return Utils.Responses.Ok + $"removed \" {alias} \" as alias for {c.CommandName} command";
        }
        return Utils.Responses.Fail + $"there is no \" {commandName} \" command ";
    }
}
