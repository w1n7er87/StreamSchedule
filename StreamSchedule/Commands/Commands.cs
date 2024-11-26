using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using System.Reflection;

namespace StreamSchedule.Commands;

internal static class Commands
{
    private static List<Type> KnownCommands => Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(Command))).ToList();

    public static List<Command> CurrentCommands { get; private set; } = [];
    public static List<TextCommand> CurrentTextCommands { get; private set; } = [];

    private static readonly List<string> _currentAliases = [];

    internal static string RetrieveArguments(string[] args, string input, out Dictionary<string, string> usedArgs)
    {
        usedArgs = [];
        string[] split = input.Split(' ');

        foreach (string ss in split)
        {
            foreach (var arg in args)
            {
                if (!ss.Contains($"-{arg}", StringComparison.InvariantCultureIgnoreCase)) continue;
                usedArgs[arg.ToLower()] = ss.Replace($"-{arg}", "").ToLower();
                input = input.Replace(ss, "", StringComparison.InvariantCultureIgnoreCase);
            }
        }
        return input.TrimStart();
    }

    internal static void InitializeCommands(string[] channels, DatabaseContext context)
    {
        List<CommandAlias> aliases = [.. context.CommandAliases];
        List<TextCommand> textCommands = [.. context.TextCommands];

        foreach (var alias in aliases)
        {
            if (alias?.Aliases is null) continue;
            _currentAliases.AddRange(alias.Aliases);
        }

        foreach (var cmd in textCommands)
        {
            if (cmd?.Aliases is null) continue;
            _currentAliases.AddRange(cmd.Aliases);
        }

        foreach (var c in KnownCommands)
        {
            CurrentCommands.Add((Command)Activator.CreateInstance(c)!);
            foreach (string channel in channels)
            {
                CurrentCommands[^1].LastUsedOnChannel.Add(channel, DateTime.Now);
            }

            if (!aliases.Any(x => x.CommandName.Equals(CurrentCommands[^1].Call, StringComparison.OrdinalIgnoreCase)))
            {
                context.Add(new CommandAlias() { CommandName = CurrentCommands[^1].Call.ToLower() });
            }
        }
        context.SaveChanges();
        CurrentTextCommands = textCommands;
    }

    internal static bool CheckNameAvailability(string alias)
    {
        List<string> commandNames = [];
        CurrentCommands.ForEach(x => commandNames.Add(x.Call));
        CurrentTextCommands.ForEach(x => commandNames.Add(x.Name));

        return !commandNames.Any(x => x.Equals(alias, StringComparison.OrdinalIgnoreCase)) && !_currentAliases.Any(x => x.Equals(alias, StringComparison.OrdinalIgnoreCase));
    }

    internal static void AddNewTextCommand( TextCommand textCommand) => CurrentTextCommands.Add(textCommand);
    internal static void RemoveTextCommand( TextCommand textCommand) => CurrentTextCommands.Remove(textCommand);

    internal static void AddAlias(string alias) => _currentAliases.Add(alias);
    internal static void RemoveAlias(string alias) => _currentAliases.Remove(alias);
}
