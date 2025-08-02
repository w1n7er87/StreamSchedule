using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Export.Data;

namespace StreamSchedule.Commands;

public class Test : Command
{
    internal override string Call => "test";
    internal override Privileges MinPrivilege => Privileges.Uuh;
    internal override string Help => "Erm";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;
    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string slug = Export.ExportUtils.GetSlug();
        
        BotCore.PagesDB.PageContent.Add(new Content()
        {
            CreatedAt = DateTime.UtcNow,
            HtmlContent = "<p> buh </p>",
            Slug = slug
        });
        
        BotCore.PagesDB.SaveChanges();
        
        BotCore.Nlog.Info(BotCore.PagesDB.Database.GetConnectionString());
        BotCore.Nlog.Info(slug);
        return Task.FromResult(new CommandResult(""));
    }
}
