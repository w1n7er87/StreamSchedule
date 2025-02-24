using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Markovs : Command
{
    internal override string Call => "markov";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "Markov chain or Markov process is a stochastic process describing a sequence of possible events in which the probability of each event depends only on the state attained in the previous event. Informally, this may be thought of as, \"What happens next depends only on the state of affairs now.\"";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int) Cooldowns.Minute);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["muted"];

    private static bool isMuted = true;

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);
        if(usedArgs.TryGetValue("muted",out _) && message.sender.Privileges >= Privileges.Mod)
        {
            isMuted = !isMuted;
            return new CommandResult(isMuted ? "ok i shut up" : "ok unmuted");
        }

        return new CommandResult(isMuted ? "" : Markov.Markov.Generate(message.content));
    }
}
