using System.Text;
using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class Top : Command
{
    public override string Call => "top";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "get top chatters by messages sent offline(default)/online or by score/ratio";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["online", "offline", "total", "score", "ratio", "p", "d"];
    public override List<string> Aliases { get; set; } = [];

    private const int PageSize = 10;

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        _ = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args);
        int page = args.TryGetValue("p", out string? p) ? int.TryParse(p, out int pp) ? Math.Max(pp - 1, 0) : 0 : 0;
        bool descending = args.TryGetValue("d", out _);

        int skip = page * PageSize;

        if (args.TryGetValue("ratio", out _)) return Task.FromResult(new CommandResult(GetTopByRatio(skip, descending), requiresFilter:true));
        if (args.TryGetValue("score", out _)) return Task.FromResult(new CommandResult(GetTopByScore(skip, descending), requiresFilter:true));
        if (args.TryGetValue("online", out _)) return Task.FromResult(new CommandResult(GetTopByOnline(skip, descending), requiresFilter:true));
        if (args.TryGetValue("total", out _)) return Task.FromResult(new CommandResult(GetTopByTotal(skip, descending), requiresFilter:true));
        
        return Task.FromResult(new CommandResult(GetTopByOffline(skip, descending), requiresFilter:true));
    }

    private static StringBuilder GetTopByRatio(int skipToPage, bool descending)
    {
        StringBuilder result = new();

        List<(User, float)> topTen =
        [
            .. BotCore.DBContext.Users
                .AsNoTracking()
                .AsEnumerable()
                .Select(u => (u, Userscore.Ratio(u)) )
                .OrderByDescending(x => descending ? -x.Item2 : x.Item2)
                .Skip(skipToPage)
                .Take(PageSize)
        ];

        for (int i = 0; i < topTen.Count; i++)
            result.Append($"{skipToPage + i + 1} {topTen[i].Item1.Username!.Insert(1, "\udb40\uddef")} {MathF.Round(topTen[i].Item2, 3)} er ");
        
        return result;
    }

    private static StringBuilder GetTopByScore(int skipToPage, bool descending)
    {
        StringBuilder result = new();

        List<(User,float)> topTen =
        [
            .. BotCore.DBContext.Users
                .AsNoTracking()
                .AsEnumerable()
                .Select(u => (u, Userscore.Score(u)))
                .OrderByDescending(x => descending ? -x.Item2 : x.Item2)
                .Skip(skipToPage)
                .Take(PageSize)
        ];

        for (int i = 0; i < topTen.Count; i++)
            result.Append($"{skipToPage + i + 1} {topTen[i].Item1.Username!.Insert(1, "\udb40\uddef")} {MathF.Round(topTen[i].Item2, 3)} er ");
        
        return result;
    }

    private static StringBuilder GetTopByOnline(int skipToPage, bool descending)
    {
        StringBuilder result = new();

        List<User> topTen =
        [
            .. BotCore.DBContext.Users.OrderByDescending(x => descending ? -x.MessagesOnline : x.MessagesOnline)
                .Skip(skipToPage)
                .Take(PageSize)
                .AsNoTracking()
        ];

        for (int i = 0; i < topTen.Count; i++)
            result.Append($"{skipToPage + i + 1} {topTen[i].Username!.Insert(1, "\udb40\uddef")} {topTen[i].MessagesOnline} er ");
        
        return result;
    }

    private static StringBuilder GetTopByOffline(int skipToPage, bool descending)
    {
        StringBuilder result = new();

        List<User> topTen =
        [
            .. BotCore.DBContext.Users.OrderByDescending(x => descending ? -x.MessagesOffline : x.MessagesOffline)
                .Skip(skipToPage)
                .Take(PageSize)
                .AsNoTracking()
        ];

        for (int i = 0; i < topTen.Count; i++)
            result.Append($"{skipToPage + i + 1} {topTen[i].Username!.Insert(1, "\udb40\uddef")} {topTen[i].MessagesOffline} er ");
        
        return result;
    }
    
    private static StringBuilder GetTopByTotal(int skipToPage, bool descending)
    {
        StringBuilder result = new();
        List<User> topTen = [.. BotCore.DBContext.Users.OrderByDescending(x => descending ? -(x.MessagesOffline + x.MessagesOnline) : x.MessagesOffline + x.MessagesOnline)
            .Skip(skipToPage)
            .Take(PageSize)
            .AsNoTracking()
        ];
        
        for (int i = 0; i < topTen.Count; i++)
            result.Append($"{skipToPage + i + 1} {topTen[i].Username!.Insert(1, "\udb40\uddef")} {topTen[i].MessagesOffline + topTen[i].MessagesOnline} er ");
        
        return result;
    }
}
