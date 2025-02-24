using Microsoft.EntityFrameworkCore;
using StreamSchedule.Markov.Data;

namespace StreamSchedule.Markov;

internal static class Markov
{
    private static MarkovContext context;
    private static IEnumerable<LinkStored> StoredLinks;

    private static readonly Dictionary<string, Link> links = [];
    private const int MaxLinks = 75;

    public static void AddMessage(string message)
    {
        string[] split = message.Split(' ', StringSplitOptions.TrimEntries);
        for(int i = 0; i < split.Length; i++)
        {
            AddLink(split[i], (i + 1 >= split.Length) ? null : split[i + 1]);
        }
    }

    public static string Generate(string? input)
    {
        List<Link> chain = [];
        string? lastWord = input?.Split(" ",StringSplitOptions.TrimEntries)[^1];
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

    private static void AddLink(string current, string? next)
    {
        if (string.IsNullOrEmpty(next)) next = "\n";
        if (links.ContainsKey(current)) {links[current].Add(next); return; }
        links.Add(current, new Link(current));
        links[current].Add(next);
    }

    internal static Link GetByKeyOrDefault(string key)
    {
        return links.TryGetValue(key, out Link? result) ? result : Link.EOL;
    }

    public static void Load(MarkovContext contextInjected)
    {
        context = contextInjected;
        StoredLinks = context.Links.Include(x => x.NextWords);
        
        foreach (var b in StoredLinks)
        {
            links.Add(b.Key, new Link(b.Key, b.NextWords));
        }
    }

    public static async Task SaveAsync()
    {
        foreach(var linkInMemory in links.Values)
        {
            var storedLink = StoredLinks.FirstOrDefault(sl => sl.Key == linkInMemory.Key);
            if (storedLink is not null)
            {
                foreach(var nextWord in linkInMemory.next)
                {
                    var storedWord = storedLink.NextWords.FirstOrDefault(x => x.Word == nextWord.Key);
                    if (storedWord is not null)
                    {
                        storedWord.Count = nextWord.Value;
                    }
                    else
                    {
                        storedLink.NextWords.Add(new WordCountPair() {
                            Link = storedLink,
                            LinkID = storedLink.ID,
                            Word = nextWord.Key,
                            Count = nextWord.Value 
                        });
                    }
                }
            }
            else
            {
                LinkStored toAdd = new()
                {
                    Key = linkInMemory.Key,
                    NextWords = linkInMemory.next.Select(x =>  new WordCountPair() {
                        Word = x.Key,
                        Count = x.Value 
                    }).ToList()
                };

                context.Add(toAdd);
            }
        }
        await context.SaveChangesAsync();
    }
}
