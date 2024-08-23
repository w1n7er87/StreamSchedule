using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace StreamSchedule.Commands;

internal abstract class Command 
{
    internal abstract string Call { get; }
    
    internal abstract bool Handle(ChatMessage message);
}
