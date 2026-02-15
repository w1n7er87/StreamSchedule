using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreamSchedule.Markov2.Data;

namespace StreamSchedule.Markov2;

public static class Markov
{
    public static readonly Queue<string> TokenizationQueue = new();
    
    private static Dictionary<int, Token> TokenLookup = [];
    private static Dictionary<int, List<TokenPair>> TokenPairLookup = [];
    
    public static int TokenCount => TokenLookup.Count;
    public static int TokenPairCount => context.TokenPairs.Count();

    private static readonly MarkovContext context = new(new DbContextOptionsBuilder<MarkovContext>().UseSqlite("Data Source=Markov2.data").Options);

    private static bool IsReady = false;

    private static readonly int eolID = 0;
    public static bool Start => true;

    private static DateTime lastSave = DateTime.UtcNow;
    private static readonly TimeSpan saveInterval = TimeSpan.FromHours(3);
    
    private static Random random = new();
    
    static Markov()
    {
        context.Database.EnsureCreated();
        eolID = context.Tokens.FirstOrDefault(t => t.Value.Equals("\e"))?.TokenID ?? 0;
        Task.Run(FirstLoader);
    }

    private static async Task FirstLoader()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        BotCore.Nlog.Info("Loading markov");
        Load();
        Task.Run(Saver);
        Task.Run(Tokenizer);
        IsReady = true;
    }
    
    private static async Task Saver()
    {
        while (true)
        {
            await Task.Delay(1000);
            if (DateTime.UtcNow - lastSave > saveInterval)
            {
                Save();
                lastSave = DateTime.UtcNow;
            }
        }
    }
    
    private static async Task Tokenizer()
    {
        while (true)
        {
            if (TokenizationQueue.Count <= 0 || !IsReady)
            {
                await Task.Delay(50);
                continue;
            }
            
            TokenizeMessage(TokenizationQueue.Peek());
            TokenizationQueue.Dequeue();
        }
    }
    
    private static void TokenizeMessage(string message)
    {
        try
        {
            string[] words = message.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string nextWord = (i + 1 >= words.Length) ? "\e" : words[i + 1];
                Token? next = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(nextWord)).Value;

                if (next is null)
                {
                    next = new Token(TokenLookup.Count, nextWord);
                    TokenLookup.Add(next.TokenID, next);
                    TokenPairLookup.Add(next.TokenID, []);
                }
            
                Token? current = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(words[i])).Value;
                if (current is null)
                {
                    current = new Token(TokenLookup.Count, words[i]);
                    TokenLookup.Add(current.TokenID, current);
                
                    TokenPair tp = new TokenPair(current.TokenID, next.TokenID, 1);
                    TokenPairLookup.Add(current.TokenID, [tp]);
                    continue;
                }
            
                TokenPairLookup.TryGetValue(current.TokenID, out List<TokenPair>? temp);
                TokenPair? pairWithNext = temp?.FirstOrDefault(x => x.NextTokenID == next.TokenID);
                if (pairWithNext is null)
                {
                    pairWithNext = new TokenPair(current.TokenID, next.TokenID, 1);
                    if (temp is not null)
                    {
                        TokenPairLookup[current.TokenID].Add(pairWithNext);
                    }
                    else
                    {
                        TokenPairLookup.Add(current.TokenID, [pairWithNext]);
                    }
                }
                else
                {
                    pairWithNext.Count++;
                }
            }
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error(e);
            return;
        }
    }

    public static TimeSpan Save()
    {
        long startSave = Stopwatch.GetTimestamp();
        IsReady = false;
        
        context.Tokens.AddRange(TokenLookup.Where(t => !context.Tokens.Contains(t.Value)).Select(t => t.Value));
        foreach (KeyValuePair<int, List<TokenPair>> tp in TokenPairLookup)
        {
            context.TokenPairs.AddRange(tp.Value.Where(t => !context.TokenPairs.Contains(t)));
        }

        context.SaveChanges();
        TimeSpan elapsed = Stopwatch.GetElapsedTime(startSave);
        BotCore.Nlog.Info($"markov save took {elapsed.Seconds} s");
        IsReady = true;
        return elapsed;
    }

    public static TimeSpan Load()
    {
        long startLoad = Stopwatch.GetTimestamp();
        IsReady = false;
        TokenPairLookup = [];
        TokenLookup = [];
        
        if (!context.Tokens.Any())
            TokenLookup[0] = new Token(0, "\e");

        ILookup<int, TokenPair> tokenPairs = context.TokenPairs.AsNoTracking().ToLookup(t => t.TokenID);

        foreach (Token token in context.Tokens.AsNoTracking())
        {
            TokenPairLookup.Add(token.TokenID, [.. tokenPairs[token.TokenID]]);
            TokenLookup.Add(token.TokenID, token);
        }

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startLoad);
        BotCore.Nlog.Info($"markov load took {elapsed.Seconds} s");
        IsReady = true;
        return elapsed;
    }

    public static string GenerateSequence(string? firstWord = null, int maxLength = 25, Method method = Method.random, int? seed = null)
    {
        if (!IsReady) return "uuh ";
        
        bool forceNoLineEnd = method.HasFlag(Method.force);
        bool reverse = method.HasFlag(Method.reverse);
        
        random = new Random(seed ?? DateTime.Now.Millisecond);
        
        Token? first = null;
        if (!string.IsNullOrWhiteSpace(firstWord)) first = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(firstWord)).Value;
        
        first ??= TokenLookup[random.Next(0, TokenLookup.Count)];

        List<int> tokenIDs = method switch
        {
            _ when method.HasFlag(Method.ordered) => reverse ? PickOrderedReverse(first.TokenID, maxLength) : PickOrdered(first.TokenID, maxLength, forceNoLineEnd),
            _ when method.HasFlag(Method.weighted) => reverse ? PickWeightedReverse(first.TokenID, maxLength) : PickWeighted(first.TokenID, maxLength, forceNoLineEnd),
            _ when method.HasFlag(Method.random) => reverse ? PickRandomReverse(first.TokenID, maxLength) : PickRandom(first.TokenID, maxLength, forceNoLineEnd),
            _ => PickRandom(first.TokenID, maxLength, forceNoLineEnd),
        };
        string result = "";

        foreach (int tokenID in tokenIDs) result += TokenLookup[tokenID].Value + " ";

        return result;
    }
    
    private static List<int> PickOrdered(int id, int maxLength, bool forceNoLineEnd)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            TokenPairLookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;
            if (forceNoLineEnd)
            {
                p.RemoveAll(t => t.NextTokenID == eolID);
                if (p.Count == 0) return sequence;
            }

            int cut = random.Next(0, p.Count + 1);
            sequence.Add(p.OrderByDescending(x => x.Count).ElementAt(random.Next(0, cut)).NextTokenID);
        }
        return sequence;
    }
    
    private static List<int> PickOrderedReverse(int id, int maxLength)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            int i1 = i;
            List<TokenPair> p = context.TokenPairs.Where(tp => tp.NextTokenID == sequence[i1 - 1]).AsNoTracking().ToList();
            if (p.Count == 0)
            {
                sequence.Reverse();
                return sequence;
            }
            
            int cut = random.Next(0, p.Count + 1);
            sequence.Add(p.OrderByDescending(x => x.Count).ElementAt(random.Next(0, cut)).NextTokenID);
        }
        sequence.Reverse();
        return sequence;
    }

    private static List<int> PickWeighted(int id, int maxLength, bool forceNoLineEnd)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            TokenPairLookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;
            if (forceNoLineEnd)
            {
                p.RemoveAll(t => t.NextTokenID == eolID);
                if (p.Count == 0) return sequence;
            }

            int max = p.MaxBy(x => x.Count)?.Count ?? 2;
            int cut = random.Next(0, max + 1);
            sequence.Add(p.OrderBy(x => x.Count).First(x => x.Count >= cut).NextTokenID);
        }
        return sequence;
    }

    private static List<int> PickWeightedReverse(int id, int maxLength)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            int i1 = i;
            List<TokenPair> p = context.TokenPairs.Where(tp => tp.NextTokenID == sequence[i1 - 1]).AsNoTracking().ToList();
            if (p.Count == 0)
            {
                sequence.Reverse();
                return sequence;
            }
            
            int max = p.MaxBy(x => x.Count)?.Count ?? 2;
            int cut = random.Next(0, max + 1);
            sequence.Add(p.OrderBy(x => x.Count).First(x => x.Count >= cut).NextTokenID);
        }
        
        sequence.Reverse();
        return sequence;
    }

    
    private static List<int> PickRandom(int id, int maxLength, bool forceNoLineEnd)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            TokenPairLookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;
            if (forceNoLineEnd)
            {
                p.RemoveAll(t => t.NextTokenID == eolID);
                if (p.Count == 0) return sequence;
            }

            sequence.Add(p[random.Next(0, p.Count)].NextTokenID);
        }
        return sequence;
    }

    private static List<int> PickRandomReverse(int id, int maxLength)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            int i1 = i;
            List<TokenPair> candidates = context.TokenPairs.Where(tp => tp.NextTokenID == sequence[i1 - 1]).AsNoTracking().ToList();
            if (candidates.Count == 0)
            {
                sequence.Reverse();
                return sequence;
            }
            
            sequence.Add(candidates[random.Next(0, candidates.Count)].TokenID);
        }
        sequence.Reverse();
        return sequence;
    }
}
