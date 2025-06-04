using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class Permit : Command
{
    internal override string Call => "permit";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "block term, or replace it with another term";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["as", "remove", "noreplace", "anycase"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs).Split(" ", StringSplitOptions.TrimEntries);
        CommandResult result = new("");

        if (split.Length < 1) return Task.FromResult(result + "no term provided");
        PermittedTerm? term = BotCore.DBContext.PermittedTerms.FirstOrDefault(x => x.Term == split[0]);

        if (usedArgs.TryGetValue("remove", out _))
        {
            if (term is null) return Task.FromResult(result + "term not found");
            BotCore.DBContext.PermittedTerms.Remove(term);
            BotCore.DBContext.SaveChanges();
            return Task.FromResult(result + $"removed {term.Term} ");
        }

        term ??= BotCore.DBContext.PermittedTerms.Add(new() { Term = split[0] }).Entity;

        if (usedArgs.TryGetValue("as", out string? alt)) term.Alternative = alt;

        if (usedArgs.TryGetValue("noreplace", out _)) term.Noreplace = true;

        if (usedArgs.TryGetValue("anycase", out _)) term.Anycase = true;

        BotCore.DBContext.SaveChangesAsync();
        return Task.FromResult(Utils.Responses.Ok);
    }
}
