﻿using StreamSchedule.Data;
using StreamSchedule.Markov;

namespace StreamSchedule.Commands;

internal class Markovs : Command
{
    internal override string Call => "markov";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "Markov chain formed from this chat's messages";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.HalfAMinute);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["muted", "w", "iw", "o", "io", "prune", "count"];

    private static bool isMuted = true;

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string cleanContent = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs).Trim();
        if (usedArgs.TryGetValue("muted", out _) && message.sender.Privileges >= Privileges.Mod)
        {
            isMuted = !isMuted;
            return new CommandResult(isMuted ? "ok i shut up" : "ok unmuted");
        }

        if (usedArgs.TryGetValue("prune", out _) && message.sender.Privileges == Privileges.Uuh) return Utils.Responses.Ok + (await Markov.Markov.Prune());

        if (usedArgs.TryGetValue("count", out _)) return new CommandResult($"there are {Markov.Markov.Count()} links");

        LinkGenerationMethod method = usedArgs.TryGetValue("w", out _) ? LinkGenerationMethod.Weighted : 
            usedArgs.TryGetValue("o", out _) ? LinkGenerationMethod.Ordered : 
            usedArgs.TryGetValue("iw", out _) ? LinkGenerationMethod.InverseWeighted : 
            usedArgs.TryGetValue("io", out _) ? LinkGenerationMethod.InverseOrdered : 
            LinkGenerationMethod.Random;

        return new CommandResult(isMuted ? "muted ok " : Utils.Filter(Markov.Markov.Generate(cleanContent, method)));
    }
}
