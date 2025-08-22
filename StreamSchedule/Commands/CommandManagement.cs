using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class CommandManagement : Command
{
    public override string Call => "cmd";
    public override Privileges Privileges => Privileges.Mod;
    public override string Help => "manage simple text commands: -add/-rm (-p[priv] optional) [command name](required) [command content](required)";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["add", "rm", "p", "alias"];
    public override List<string> Aliases { get; set; } = [];
    
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string commandContent = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArguments);
        string commandName = commandContent.Split(' ')[0].ToLower();
        commandContent = commandContent[commandName.Length..].TrimStart();

        Privileges privileges = usedArguments.TryGetValue("p", out string? pp) ? PrivilegeUtils.ParsePrivilege(pp) : Privileges.None;

        if (string.IsNullOrEmpty(commandName)) return Task.FromResult(Utils.Responses.Fail + " no command name provided ");
        if (commandName.Length < 2) return Task.FromResult(Utils.Responses.Fail + " command name should be 2 characters or longer ");

        if (usedArguments.TryGetValue("add", out _))
        {
            return Task.FromResult(usedArguments.TryGetValue("alias", out _)
                ? AddAlias(commandName, commandContent)
                : AddCommand(commandName, commandContent, privileges, LastUsedOnChannel));
        }

        if (usedArguments.TryGetValue("rm", out _))
        {
            return Task.FromResult(usedArguments.TryGetValue("alias", out _)
                ? RemoveAlias(commandName, commandContent)
                : RemoveCommand(commandName));
        }

        return Task.FromResult(Utils.Responses.Surprise);
    }

    private static CommandResult AddCommand(string commandName, string content, Privileges privileges, Dictionary<string,DateTime> lastUsed)
    {
        if (string.IsNullOrEmpty(content)) return Utils.Responses.Fail + " no content provided ";

        if (!Commands.IsNameAvailable(commandName)) return Utils.Responses.Fail + " command with this name/alias already exists. ";

        TextCommand newCommand = new() { Name = commandName, Content = content, Privileges = privileges, Aliases = [] };
        
        foreach (string channel in lastUsed.Keys)
        {
            newCommand.LastUsedOnChannel.Add(channel, DateTime.MinValue);
        }

        Commands.AllCommands.Add(newCommand);
        BotCore.DBContext.TextCommands.Add(newCommand);
        BotCore.DBContext.SaveChanges();

        return Utils.Responses.Ok + $"added command \" {commandName} \" for {PrivilegeUtils.PrivilegeToString(privileges)}";
    }

    private static CommandResult RemoveCommand(string commandName)
    {
        ICommand? c = Commands.AllCommands.FirstOrDefault(x => x.Call == commandName || x.Aliases.Contains(commandName));

        if (c is null) return Utils.Responses.Fail + $"there is no \" {commandName} \" command ";
        if (c.GetType().IsSubclassOf(typeof(Command))) return Utils.Responses.Fail + $"the \" {commandName} \" command cannot be removed ";
        
        Commands.AllCommands.Remove(c);
        BotCore.DBContext.TextCommands.Remove((TextCommand)c);
        BotCore.DBContext.SaveChanges();

        return Utils.Responses.Ok;
    }

    private static CommandResult AddAlias(string commandName, string alias)
    {
        if (!Commands.IsNameAvailable(alias)) return Utils.Responses.Fail + " command with this name/alias already exists. ";
        ICommand? command = Commands.AllCommands.FirstOrDefault(x => x.Call == commandName);
        if(command is null) return Utils.Responses.Fail + $"there is no \" {commandName} \" command ";
        
        command.Aliases.Add(alias);
        BotCore.DBContext.SaveChanges();
        return Utils.Responses.Ok + $"added \" {alias} \" alias for {command.Call} command";
    }

    private static CommandResult RemoveAlias(string commandName, string alias)
    {
        ICommand? command = Commands.AllCommands.FirstOrDefault(x => x.Call == commandName || x.Aliases.Contains(commandName));
        if (command is null) return Utils.Responses.Fail + $"there is no \" {commandName} \" command ";
        if (!command.Aliases.Contains(alias)) return Utils.Responses.Fail + $"command \" {commandName} \" does not have \" {alias} \" alias ";
        command.Aliases.Remove(alias);
        BotCore.DBContext.SaveChanges();
        return Utils.Responses.Ok + $"removed \" {alias} \" alias for \" {commandName} \" command";
    }
}
