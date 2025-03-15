﻿using StreamSchedule.Markov.Data;

namespace StreamSchedule.Markov;

internal enum LinkGenerationMethod
{
    Weighted,
    InverseWeighted,
    Ordered,
    InverseOrdered,
    Random,
}

internal class Link
{
    internal string Key { get; private set; }
    internal readonly Dictionary<string, int> next = [];

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
            if (next.ContainsKey(wcp.Word)) continue;
            next.Add(wcp.Word, wcp.Count);
        }
    }

    internal void Add(string value)
    {
        if (!next.TryAdd(value, 1)) next[value]++;
    }

    internal Link GetNext(LinkGenerationMethod method)
    {
        if (next.Count < 1) return EOL;

        switch (method)
        {
            case LinkGenerationMethod.Ordered:
            {
                int randomCutoff = Random.Shared.Next(1, next.Count + 1);
                KeyValuePair<string, int>[] upperHalf = [.. next.Where(x => !x.Key.Equals("\n")).OrderBy(x => x.Value).TakeLast(randomCutoff)];
                return (upperHalf.Length < 1) ? EOL : Markov.GetByKeyOrDefault(upperHalf[Random.Shared.Next(upperHalf.Length)].Key);
            }
            case LinkGenerationMethod.InverseOrdered:
            {
                int randomCutoff = Random.Shared.Next(1, next.Count + 1);
                KeyValuePair<string, int>[] lowerHalf = [.. next.Where(x => !x.Key.Equals("\n")).OrderBy(x => -x.Value).TakeLast(randomCutoff)];
                return (lowerHalf.Length < 1) ? EOL : Markov.GetByKeyOrDefault(lowerHalf[Random.Shared.Next(lowerHalf.Length)].Key);
            }
            case LinkGenerationMethod.Weighted:
            {
                List<KeyValuePair<string, int>> noEOL = [.. next.Where(x => !x.Key.Equals("\n"))];
                if (noEOL.Count == 0) return EOL;

                int randomCutoff = Random.Shared.Next(noEOL.Max(x => x.Value));
                KeyValuePair<string, int>[] upperHalf = [.. noEOL.Where(x => x.Value >= randomCutoff)];
                return (upperHalf.Length == 0) ? EOL : Markov.GetByKeyOrDefault(upperHalf[Random.Shared.Next(upperHalf.Length)].Key);
            }
            case LinkGenerationMethod.InverseWeighted:
            {
                List<KeyValuePair<string, int>> noEOL = [.. next.Where(x => !x.Key.Equals("\n"))];
                if (noEOL.Count == 0) return EOL;

                int randomCutoff = Random.Shared.Next(noEOL.Min(x => x.Value), noEOL.Max(x => x.Value));
                KeyValuePair<string, int>[] lowerHalf = [.. noEOL.Where(x => x.Value <= randomCutoff)];
                return (lowerHalf.Length == 0) ? EOL : Markov.GetByKeyOrDefault(lowerHalf[Random.Shared.Next(lowerHalf.Length)].Key);
            }
            case LinkGenerationMethod.Random:
                return Markov.GetByKeyOrDefault(next.Keys.ToArray()[Random.Shared.Next(next.Keys.Count)]);
            default:
                return EOL;
        }
    }

    internal static Link EOL => new("\n");

    public override string ToString() => Key;
}
