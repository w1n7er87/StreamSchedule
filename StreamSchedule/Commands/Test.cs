using System.Text;
using StreamSchedule.Data;
using StreamSchedule.Export;
using StreamSchedule.Export.Conversions;
using StreamSchedule.Export.Data;
using StreamSchedule.Export.Templates;
using StreamSchedule.GraphQL;

namespace StreamSchedule.Commands;

internal class Test : Command
{
    public override string Call => "test";
    public override Privileges Privileges => Privileges.Uuh;
    public override string Help => "Erm";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        List<string> emoteIDs = BotCore.MessageCache.FirstOrDefault(x => x.Id == message.ID)?.EmoteSet.Emotes
            .Select(x => x.Id).ToList() ?? [];

        List<Task<GraphQL.Data.Emote?>> emoteTasks = [.. emoteIDs.Select(GraphQLClient.GetEmote)];
        List<GraphQL.Data.Emote?> emotes = [.. (await Task.WhenAll(emoteTasks))];
        
        string slug = ExportUtils.GetSlug(message.channelName);
        StringBuilder html = new();

        int count = emotes.Count - Random.Shared.Next(emotes.Count);
        
        html.Append(string.Format(Templates.EmotesBlock, "Added",
            string.Join("\n", emotes.Take(count).Select(x => Conversions.EmoteToHtml(x) ?? ""))));
        
        html.Append(Templates.Divider);
        
        html.Append(string.Format(Templates.EmotesBlock, "Removed",
            string.Join("\n",
                emotes.Take(new Range(count, emotes.Count)).Select(x => Conversions.EmoteToHtml(x) ?? ""))));
        
        _ = await BotCore.PagesDB.PageContent.AddAsync(new Content()
        {
            EmbeddedStyleName = Templates.EmoteUpdatesStyleName,
            EmbeddedStyleVersion = Templates.EmoteUpdatesStyleVersion,
            CreatedAt = DateTime.UtcNow,
            HtmlContent = html.ToString(),
            Slug = slug,
            Summary = string.Format(Templates.EmoteUpdatesSummary, message.sender.Username),
            Title = message.sender.Username
        });
        
        await BotCore.PagesDB.SaveChangesAsync();
        
        BotCore.Nlog.Info(ExportUtils.EmotesUrlBase + slug);

        return new CommandResult("");
    }
}
