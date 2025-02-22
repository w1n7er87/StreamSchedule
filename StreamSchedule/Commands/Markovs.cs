
using StreamSchedule.Data;

namespace StreamSchedule.Commands
{
    internal class Markovs : Command
    {
        internal override string Call => "markov";
        internal override Privileges MinPrivilege => Privileges.Trusted;
        internal override string Help => "uuh trying some stuff";
        internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int) Cooldowns.Short);
        internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
        internal override string[] Arguments => ["muted"];

        private static bool isMuted = true;

        internal override Task<CommandResult> Handle(UniversalMessageInfo message)
        {

            Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);
            if(usedArgs.TryGetValue("muted",out _) && message.sender.Privileges >= Privileges.Mod)
            {
                isMuted = !isMuted;
                return Task.FromResult(new CommandResult(isMuted ? "ok i shut up" : "ok unmuted"));
            }

            return Task.FromResult(new CommandResult(Markov.Markov.Generate(message.content)));
        }
    }
}
