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
    internal override string[]? Arguments => ["add", "rm", "p", "alias"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        List<TextCommand> commands = [.. BotCore.DBContext.TextCommands];

        string text = Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> usedArguments);
        string commandName = text.Split(' ')[0].ToLower();
        text = text[commandName.Length..].TrimStart();

        Privileges privileges = usedArguments.TryGetValue("p", out string? pp) ? PrivilegeUtils.ParsePrivilege(pp) : Privileges.None;

        if (string.IsNullOrEmpty(commandName)) return Task.FromResult(Utils.Responses.Fail + (" no command name provided "));
        if (commandName.Length < 2) return Task.FromResult(Utils.Responses.Fail + (" command name should be 2 characters or longer "));

        if (usedArguments.TryGetValue("add", out _))
        {
            if (usedArguments.TryGetValue("alias", out _)) return Task.FromResult(new CommandResult(AddAlias(commandName, text, ref commands)));
            return Task.FromResult(new CommandResult(AddCommand(commandName, text, privileges, commands)));
        }

        if (usedArguments.TryGetValue("rm", out _))
        {
            if (usedArguments.TryGetValue("alias", out _)) return Task.FromResult(new CommandResult(RemoveAlias(commandName, text, ref commands)));
            return Task.FromResult(new CommandResult(RemoveCommand(commandName, commands)));
        }

        return Task.FromResult(Utils.Responses.Surprise);
    }

    private static string AddCommand(string commandName, string content, Privileges privileges, in List<TextCommand> commands)
    {
        if (string.IsNullOrEmpty(content)) return (Utils.Responses.Fail + " no content provided ").ToString();

        if (commands.Any(x => x.Name == commandName) || BotCore.CurrentCommands.Any(x => x?.Call == commandName))
        { return (Utils.Responses.Fail + " command with this name already exists. ").ToString(); }

        BotCore.DBContext.TextCommands.Add(new() { Name = commandName, Content = content, Privileges = privileges });
        BotCore.DBContext.SaveChanges();
        return $"{Utils.Responses.Ok} added command \"{commandName}\" for {PrivilegeUtils.PrivilegeToString(privileges)}";
    }

    private static string RemoveCommand(string commandName, in List<TextCommand> commands)
    {
        if (commands.Count <= 0) return (Utils.Responses.Fail + " there are no custom commands. ").ToString();

        TextCommand? c = BotCore.DBContext.TextCommands.FirstOrDefault(x => x.Name.ToLower() == commandName);

        if (c is null) return (Utils.Responses.Fail + " there is no command with this name. ").ToString();

        BotCore.DBContext.TextCommands.Remove(c);
        BotCore.DBContext.SaveChanges();
        return Utils.Responses.Ok.ToString();
    }

    private static string AddAlias(string commandName, string alias, ref List<TextCommand> commands)
    {
        if (commands.Any(x => x.Name == commandName))
        {
            if (commandName.Equals(alias, StringComparison.OrdinalIgnoreCase)) return Utils.Responses.Fail.ToString() + $"{commandName} = {alias} Stare ";

            var c = commands.First(x => x.Name == commandName);
            c.Aliases ??= [alias];
            if (!c.Aliases.Contains(alias)) { c.Aliases.Add(alias); }
            BotCore.DBContext.SaveChanges();
            return $"{Utils.Responses.Ok} added \"{alias}\" as alias for {c.Name} command";
        }

        if (BotCore.CurrentCommands.Any(x => x!.Call == commandName))
        {
            if (commandName.Equals(alias, StringComparison.OrdinalIgnoreCase)) return Utils.Responses.Fail.ToString() + $"{commandName} = {alias} Stare ";

            var c = BotCore.DBContext.CommandAliases.First(x => x.CommandName.ToLower() == commandName);
            c.Aliases ??= [alias];
            if (!c.Aliases.Contains(alias)) { c.Aliases.Add(alias); }
            BotCore.DBContext.SaveChanges();
            return $"{Utils.Responses.Ok} added \"{alias}\" as alias for {c.CommandName} command";
        }
        return $"{Utils.Responses.Fail} no command \"{commandName}\"";
    }

    private static string RemoveAlias(string commandName, string alias, ref List<TextCommand> commands)
    {
        if (commands.Any(x => x.Name == commandName))
        {
            var c = commands.First(x => x.Name == commandName);
            if (c.Aliases is null) return Utils.Responses.Fail.ToString() + $"{commandName} has no aliases ";
            c.Aliases.Remove(alias);
            BotCore.DBContext.SaveChanges();
            return $"{Utils.Responses.Ok} removed \"{alias}\" as alias for {c.Name} command";
        }

        if (BotCore.CurrentCommands.Any(x => x!.Call == commandName))
        {
            var c = BotCore.DBContext.CommandAliases.First(x => x.CommandName == commandName);
            if (c.Aliases is null) return Utils.Responses.Fail.ToString() + $"{commandName} has no aliases ";
            c.Aliases.Remove(alias);
            BotCore.DBContext.SaveChanges();
            return $"{Utils.Responses.Ok} removed \"{alias}\" as alias for {c.CommandName} command";
        }
        return $"{Utils.Responses.Fail} no command \"{commandName}\"";
    }
}
