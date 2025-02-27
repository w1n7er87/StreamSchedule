using StreamSchedule.Markov.Data;

namespace StreamSchedule.Markov;

internal enum LinkGenerationMethod
{
    Weighted,
    Ordered,
}

internal class Link
{
    internal string Key { get; private set; }
    internal Dictionary<string, int> next = [];

    private Link()
    { Key = ""; }

    internal Link(string key)
    {
        Key = key;
    }

    internal Link(string key, IEnumerable<WordCountPair> words) : this(key)
    {
        foreach (WordCountPair wcp in words)
        {
            next.Add(wcp.Word, wcp.Count);
        }
    }

    internal void Add(string value)
    {
        if (next.ContainsKey(value)) next[value]++;
        else next.Add(value, 1);
    }

    internal Link GetNext(LinkGenerationMethod method)
    {
        if (next.Count < 1) return EOL;

        if (method is LinkGenerationMethod.Ordered)
        {
            int randomCutoff = Random.Shared.Next(1, next.Count + 1);
            KeyValuePair<string, int>[] upperHalf = [.. next.OrderBy(x => x.Value).TakeLast(randomCutoff)];
            return Markov.GetByKeyOrDefault(upperHalf[Random.Shared.Next(upperHalf.Length)].Key);
        }
        else
        {
            int randomCutoff = Random.Shared.Next(next.Max(x => x.Value));
            KeyValuePair<string, int>[] upperHalf = [.. next.Where(x => x.Value >= randomCutoff)];
            return Markov.GetByKeyOrDefault(upperHalf[Random.Shared.Next(upperHalf.Length)].Key);
        }
    }

    internal static Link EOL => new("\n");

    public override string ToString() => Key;
}
