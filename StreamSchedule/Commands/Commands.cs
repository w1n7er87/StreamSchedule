using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using System.Reflection;

namespace StreamSchedule.Commands;

internal static class Commands
{
    private static List<Type> KnownCommands => [.. Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(Command)))];

    public static List<Command> CurrentCommands { get; private set; } = [];
    public static List<TextCommand> CurrentTextCommands { get; private set; } = [];

    private static readonly List<string> _allCurrentAliasStrings = [];

    internal static string RetrieveArguments(string[] args, string input, out Dictionary<string, string> usedArgs)
    {
        usedArgs = [];
        string[] split = input.Split(' ');

        foreach (string ss in split)
        {
            foreach (string arg in args)
            {
                if (!ss.Contains($"-{arg}", StringComparison.InvariantCultureIgnoreCase)) continue;
                usedArgs[arg.ToLower()] = ss.Replace($"-{arg}", "");
                input = input.Replace(ss, "", StringComparison.InvariantCultureIgnoreCase);
            }
        }
        return input.TrimStart();
    }

    internal static void InitializeCommands(List<string> channels, DatabaseContext context)
    {
        List<CommandAlias> aliases = [.. context.CommandAliases];
        List<TextCommand> textCommands = [.. context.TextCommands];

        foreach (CommandAlias alias in aliases)
        {
            if (alias.Aliases is null) continue;
            _allCurrentAliasStrings.AddRange(alias.Aliases);
        }

        foreach (TextCommand cmd in textCommands)
        {
            if (cmd.Aliases is null) continue;
            _allCurrentAliasStrings.AddRange(cmd.Aliases);
        }

        foreach (Type c in KnownCommands)
        {
            CurrentCommands.Add((Command)Activator.CreateInstance(c)!);

            channels.ForEach(x => CurrentCommands[^1].LastUsedOnChannel.Add(x, DateTime.Now));

            if (!aliases.Any(x => x.CommandName.Equals(CurrentCommands[^1].Call, StringComparison.OrdinalIgnoreCase)))
            {
                context.Add(new CommandAlias() { CommandName = CurrentCommands[^1].Call.ToLower() });
            }
        }
        context.SaveChanges();
        CurrentTextCommands = textCommands;
    }

    internal static bool IsNameAvailable(string alias)
    {
        List<string> commandNames = [];
        CurrentCommands.ForEach(x => commandNames.Add(x.Call));
        CurrentTextCommands.ForEach(x => commandNames.Add(x.Name));

        return !commandNames.Any(x => x.Equals(alias, StringComparison.OrdinalIgnoreCase)) && !_allCurrentAliasStrings.Any(x => x.Equals(alias, StringComparison.OrdinalIgnoreCase));
    }

    internal static void AddAlias(string alias) => _allCurrentAliasStrings.Add(alias);

    internal static void RemoveAlias(string alias) => _allCurrentAliasStrings.Remove(alias);
}
