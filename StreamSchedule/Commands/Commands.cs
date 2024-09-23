using System.Reflection;

namespace StreamSchedule.Commands;

internal static class Commands
{
    public static readonly List<Type> knownCommands = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(Command))).ToList();

    internal static string RetrieveArguments(string[] args, string input, out Dictionary<string, string> usedArgs)
    {
        usedArgs = [];
        string[] split = input.Split(' ');

        foreach (string ss in split)
        {
            foreach (var arg in args)
            {
                if (ss.Contains($"-{arg}", StringComparison.InvariantCultureIgnoreCase))
                {
                    usedArgs[arg.ToLower()] = ss.Replace($"-{arg}", "").ToLower();
                    input = input.Replace(ss , "", StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }
        return input.TrimStart();
    }
}
