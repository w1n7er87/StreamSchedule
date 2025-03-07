﻿using Microsoft.EntityFrameworkCore;
using StreamSchedule.Markov.Data;
using System.Diagnostics;

namespace StreamSchedule.Markov;

internal static class Markov
{
    private static MarkovContext context;

    private static readonly Dictionary<string, Link> links = [];
    private const int MaxLinks = 90;

    private static bool hasLoaded = false;
    private static long lastSaveTimestamp;

    public static async Task AddMessageAsync(string message)
    {
        if (!hasLoaded) return;

        if(Stopwatch.GetElapsedTime(lastSaveTimestamp).TotalSeconds >= 10)
        {
            lastSaveTimestamp = Stopwatch.GetTimestamp();
            await context.SaveChangesAsync();
        }

        string[] split = message.Split(' ', StringSplitOptions.TrimEntries);
        for (int i = 0; i < split.Length; i++)
        {
            await AddLink(split[i], (i + 1 >= split.Length) ? null : split[i + 1]);
        }
    }

    public static string Generate(string? input , LinkGenerationMethod method)
    {
        List<Link> chain = [];
        string? lastWord = input?.Split(" ", StringSplitOptions.TrimEntries)[^1];

        Link first;
        if (string.IsNullOrEmpty(lastWord)) first = links.ToList()[Random.Shared.Next(links.Count)].Value;
        else first = links.TryGetValue(lastWord, out Link? exists) ? exists : links.ToList()[Random.Shared.Next(links.Count)].Value;

        chain.Add(first);

        int count = 0;
        while (count < MaxLinks)
        {
            Link toAdd = chain[^1].GetNext(method);
            if (toAdd.Key.Equals("\n")) break;
            chain.Add(toAdd);
            count++;
        }

        return string.Join(" ", chain);
    }

    private static async Task AddLink(string current, string? next)
    {

        if (string.IsNullOrEmpty(next)) next = "\n";

        if (links.TryGetValue(current, out Link? value)) value.Add(next);
        else links.Add(current, new Link(current));

        LinkStored ls = (await context.Links.FirstOrDefaultAsync(x => x.Key == current)) ?? context.Links.Add(new LinkStored()
            {
                Key = current,
                NextWords = [new WordCountPair()
                {
                    Word = next,
                    Count = 1,
                }]
            }).Entity;

        WordCountPair? wc = ls.NextWords.FirstOrDefault(x => x.Word == next);

        if (wc is not null) wc.Count++;
        else ls.NextWords.Add(new WordCountPair() { Word = next, Count = 1, });
    }

    internal static Link GetByKeyOrDefault(string key) => links.TryGetValue(key, out Link? result) ? result : Link.EOL;

    internal static int Count() => links.Count;

    public static async Task<string> Prune()
    {
        int startingCount = links.Count;
        int prunedCount = 0;
        
        List<Link> noChildren = [];
        foreach(Link l in links.Values)
        {
            if(l.next.Count == 1 && l.next.Keys.First().Equals("\n") ) noChildren.Add(l);
        }

        foreach(Link noChildrenCandidate in noChildren)
        {
            bool isPresentAsNext = false;
            foreach(Link linkInMemory in links.Values)
            {
                if (linkInMemory.next.ContainsKey(noChildrenCandidate.Key)) isPresentAsNext = true;
            }

            if (isPresentAsNext) continue;
            
            links.Remove(noChildrenCandidate.Key);
            var linkInDb = context.Links.First(x => x.Key == noChildrenCandidate.Key);
            context.Links.Remove(linkInDb);
            context.NextWords.Remove(linkInDb.NextWords.First());
            prunedCount++;
        }

        await context.SaveChangesAsync();

        return $"Pruned {prunedCount} links ( {float.Round((prunedCount / (float)startingCount) * 100, 3)}% )";
    }

    public static void Load(MarkovContext contextInjected)
    {
        long start = Stopwatch.GetTimestamp();
        int duplicates = 0;
        BotCore.Nlog.Info("started loading markov");
        context = contextInjected;

        foreach (var b in context.Links.Include(x => x.NextWords).AsNoTracking())
        {
            if (links.ContainsKey(b.Key))
            {
                duplicates++;
                continue;
            }
            links.Add(b.Key, new Link(b.Key, b.NextWords));
        }
        BotCore.Nlog.Info($"finished loading markov, has {duplicates} dupes ({Stopwatch.GetElapsedTime(start).TotalSeconds}s )");

        hasLoaded = true;
        lastSaveTimestamp = Stopwatch.GetTimestamp();
    }

    public static async Task SaveAsync()
    {
        long start = Stopwatch.GetTimestamp();
        BotCore.Nlog.Info("started saving markov");
        await context.SaveChangesAsync();
        BotCore.Nlog.Info($"finished saving markov ({Stopwatch.GetElapsedTime(start).TotalSeconds}s )");
    }
}
