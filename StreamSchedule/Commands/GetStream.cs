using StreamSchedule.Data;

namespace StreamSchedule.Commands
{
    internal class GetStream : Command
    {
        internal override string Call => "stream";
        internal override Privileges MinPrivilege => Privileges.None;
        internal override string Help => "time until next stream on the schedule.";
        internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Medium);
        internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
        internal override string[]? Arguments => null;

        internal override Task<CommandResult> Handle(UniversalMessageInfo message)
        {
            var futureStreams = Body.dbContext.Streams.Where(x => x.StreamDate >= DateOnly.FromDateTime(DateTime.Now));
            Data.Models.Stream? next = null;
            foreach (var stream in futureStreams)
            {
                DateTime fullDate = new DateTime(stream.StreamDate, stream.StreamTime);
                if (fullDate >= DateTime.Now)
                {
                    next = stream;
                    break;
                }
            }

            if (next == null)
            {
                return Task.FromResult(new CommandResult("There is no more streams scheduled DinkDonk "));
            }
            else
            {
                DateTime fullDate = new DateTime(next.StreamDate, next.StreamTime);
                TimeSpan span = fullDate - DateTime.Now;
                return Task.FromResult(new CommandResult($"Next stream is in {Math.Floor(span.TotalHours).ToString() + span.ToString("'h 'm'm 's's'")} : {next.StreamTitle}"));
            }
        }
    }
}
