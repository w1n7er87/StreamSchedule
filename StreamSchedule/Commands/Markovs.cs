
using StreamSchedule.Data;

namespace StreamSchedule.Commands
{
    internal class Markovs : Command
    {
        internal override string Call => "markov";
        internal override Privileges MinPrivilege => Privileges.Trusted;
        internal override string Help => "uuh trying some stuff";
        internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int) Cooldowns.Medium);
        internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
        internal override string[]? Arguments => null;

        internal override Task<CommandResult> Handle(UniversalMessageInfo message)
        {
            return Task.FromResult(new CommandResult(Markov.Markov.Generate()));
        }
    }
}
