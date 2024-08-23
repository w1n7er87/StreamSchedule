using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal static class Commands
{
    public static readonly List<Type> knownCommands = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(Command))).ToList();
}
