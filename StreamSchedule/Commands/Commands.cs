using System.Reflection;

namespace StreamSchedule.Commands;

internal static class Commands
{
    public static readonly List<Type> knownCommands = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(Command))).ToList();
}
