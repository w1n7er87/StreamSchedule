
namespace StreamSchedule.Markov
{
    internal static class Markov
    {
        private static readonly Dictionary<string, Link> links = [];
        private const int MaxLinks = 50;

        public static void AddMessage(string message)
        {
            string[] split = message.Split(' ', StringSplitOptions.TrimEntries);
            for(int i = 0; i < split.Length; i++)
            {
                Add(split[i], (i + 1 >= split.Length) ? null : split[i + 1]);
            }
        }

        public static string Generate(string? input)
        {
            List<Link> chain = [];
            string? lastWord = input?.Split(" ")[^1];
            Link first;

            if(string.IsNullOrEmpty(lastWord)) first = links.ToList()[Random.Shared.Next(links.Count)].Value;
            else first = links.TryGetValue(lastWord, out Link? exists)? exists! : links.ToList()[Random.Shared.Next(links.Count)].Value;

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

        private static void Add(string current, string? next)
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
    }

    internal class Link(string key) 
    {
        internal string Key { get => key; set => key = value; }
        private string key = key;

        internal Dictionary<string, int> next = [];

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

        public override string ToString()
        {
            return key;
        }

        internal static Link EOL => new("\n");
    }
}
