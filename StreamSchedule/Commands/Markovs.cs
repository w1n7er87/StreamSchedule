﻿using StreamSchedule.Data;
using StreamSchedule.Markov;

namespace StreamSchedule.Commands;

internal class Markovs : Command
{
    internal override string Call => "markovv";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "Markov chain or Markov process is a stochastic process describing a sequence of possible events in which the probability of each event depends only on the state attained in the previous event. Informally, this may be thought of as, \"What happens next depends only on the state of affairs now.\"";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.HalfAMinute);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["muted", "w", "o"];

    private static bool isMuted = true;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string cleanContent = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs).Trim();
        if (usedArgs.TryGetValue("muted", out _) && message.sender.Privileges >= Privileges.Mod)
        {
            isMuted = !isMuted;
            return Task.FromResult(new CommandResult(isMuted ? "ok i shut up" : "ok unmuted"));
        }

        LinkGenerationMethod method = usedArgs.TryGetValue("w", out _) ? LinkGenerationMethod.Weighted : LinkGenerationMethod.Random;
        method = usedArgs.TryGetValue("o", out _) ? LinkGenerationMethod.Ordered: method;

        return Task.FromResult(new CommandResult(isMuted ? "" : Markov.Markov.Generate(cleanContent, method)));
    }
}
