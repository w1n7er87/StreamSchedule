using StreamSchedule.Markov.Data;

namespace StreamSchedule.Markov;

internal class Link
{
    internal string Key { get; private set; }
    internal Dictionary<string, int> next = [];

    private Link() { Key = ""; }

    internal Link(string key)
    {
        Key = key;
    }

    internal Link(string key, IEnumerable<WordCountPair> words) : this(key)
    {
        foreach(WordCountPair wcp in words)
        {
            next.Add(wcp.Word, wcp.Count);
        }
    }

    internal void Add(string value)
    {
        if (next.ContainsKey(value)) next[value]++;
        else next.Add(value, 1);
    }

    internal Link GetNext()
    {
        if (next.Count < 1) return EOL;
        int maxCount = next.Max(x => x.Value);

        List<string> a = [.. next.Where(x => x.Value >= Random.Shared.Next(maxCount)).Select(x => x.Key)];
        return Markov.GetByKeyOrDefault(a[Random.Shared.Next(a.Count)]);
    }

    internal static Link EOL => new("\n");

    public override string ToString() => Key;
}
