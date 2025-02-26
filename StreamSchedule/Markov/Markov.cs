using Microsoft.EntityFrameworkCore;
using StreamSchedule.Markov.Data;
using System.Diagnostics;

namespace StreamSchedule.Markov;

internal static class Markov
{
    private static MarkovContext context;

    private static readonly Dictionary<string, Link> links = [];
    private const int MaxLinks = 75;

    public static async Task AddMessageAsync(string message)
    {
        string[] split = message.Split(' ', StringSplitOptions.TrimEntries);
        for(int i = 0; i < split.Length; i++)
        {
            await AddLinkAsync(split[i], (i + 1 >= split.Length) ? null : split[i + 1]);
        }
    }

    public static string Generate(string? input)
    {
        List<Link> chain = [];
        string? lastWord = input?.Split(" ", StringSplitOptions.TrimEntries)[^1];

        Link first;
        if(string.IsNullOrEmpty(lastWord)) first = links.ToList()[Random.Shared.Next(links.Count)].Value;
        else first = links.TryGetValue(lastWord, out Link? exists)? exists : links.ToList()[Random.Shared.Next(links.Count)].Value;

        chain.Add(first);

        int count = 0;
        while (count < MaxLinks)
        {
            Link toAdd = chain[^1].GetNext();
            if (toAdd.Key.Equals("\n")) break;
            chain.Add(toAdd);
            count++;
        }

        return string.Join(" ", chain);
    }

    private static async Task AddLinkAsync(string current, string? next)
    {
        if (string.IsNullOrEmpty(next)) next = "\n";
        if (links.ContainsKey(current)) 
        {
            links[current].Add(next);
            LinkStored ls = context.Links.FirstOrDefault(x => x.Key == current)!;
            WordCountPair? wc =  ls.NextWords.FirstOrDefault(x => x.Word == next);
            if(wc is not null)
            {
                wc.Count++;
            }
            else
            {
                ls.NextWords.Add(new WordCountPair() { Word = next, Count = 1, });
            }
            return;
        }

        links.Add(current, new Link(current));
        links[current].Add(next);
        context.Links.Add(new LinkStored()
        {
            Key = current,
            NextWords = [new WordCountPair()
            {
                Word = next,
                Count = 1,
            }]
        });
        await context.SaveChangesAsync();
    }

    internal static Link GetByKeyOrDefault(string key)
    {
        return links.TryGetValue(key, out Link? result) ? result : Link.EOL;
    }

    public static void Load(MarkovContext contextInjected)
    {
        long start = Stopwatch.GetTimestamp();

        BotCore.Nlog.Info("started loading markov");
        context = contextInjected;

        foreach (var b in context.Links.Include(x => x.NextWords))
        {
            links.Add(b.Key, new Link(b.Key, b.NextWords));
        }
        BotCore.Nlog.Info($"finished loading markov ({Stopwatch.GetElapsedTime(start).TotalSeconds}s )");
    }

    public static async Task SaveAsync()
    {
        long start = Stopwatch.GetTimestamp();
        BotCore.Nlog.Info("started saving markov" );
        await context.SaveChangesAsync();
        BotCore.Nlog.Info($"finished saving markov ({Stopwatch.GetElapsedTime(start).TotalSeconds}s )");
    }
}
