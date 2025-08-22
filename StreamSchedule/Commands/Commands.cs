using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using System.Reflection;

namespace StreamSchedule.Commands;

internal static class Commands
{
    private static List<Type> CommandClasses => [.. Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(Command)))];

    public static readonly List<ICommand> AllCommands = [];
    
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
        foreach (Type c in CommandClasses)
        {
            Command currentCommandInstance = (Command)Activator.CreateInstance(c)!;
            
            channels.ForEach(x => currentCommandInstance.LastUsedOnChannel.Add(x, DateTime.MinValue));

            CommandAlias? alias = context.CommandAliases.FirstOrDefault(x => x.CommandName == currentCommandInstance.Call);
            
            if (alias is null)
            {
                alias = new CommandAlias() { CommandName = currentCommandInstance.Call.ToLower() , Aliases = [] };
                context.Add(alias);
                context.SaveChanges();
            }
            currentCommandInstance.Aliases = alias.Aliases;
            
            AllCommands.Add(currentCommandInstance);
        }
        
        foreach (TextCommand tc in context.TextCommands.ToList())
        {
            channels.ForEach(x => tc.LastUsedOnChannel.Add(x, DateTime.MinValue));
            AllCommands.Add(tc);
        }
    }

    internal static bool IsNameAvailable(string alias)
    {
        foreach (ICommand c in AllCommands)
        {
            if (c.Call.Equals(alias, StringComparison.OrdinalIgnoreCase)) return false;
            if (c.Aliases.Contains(alias)) return false;
        }
        return true;
    }
}
