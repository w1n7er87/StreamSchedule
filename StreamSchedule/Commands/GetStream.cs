using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace StreamSchedule.Commands
{
    internal class GetStream : Command
    {
        internal override string Call => "erm";
        internal override Privileges MinPrivilege => Privileges.None;

        internal override string Handle(ChatMessage message)
        {
            Data.Models.Stream? next = Body.dbContext.Streams.FirstOrDefault(x => x.StreamDate >= DateOnly.FromDateTime(DateTime.UtcNow));
            if (next == null)
            {
                return "There is no more streams scheduled DinkDonk ";
            }
            else
            {
                DateTime fullDate = new DateTime(next.StreamDate, next.StreamTime);
                TimeSpan span = fullDate - DateTime.Now;
                return $"Next stream is in {Math.Floor(span.TotalHours).ToString() + span.ToString("'h 'm'm 's's'")} : {next.StreamTitle}";
            }
        }
    }
}