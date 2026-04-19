using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StreamSchedule.Markov2.Data;

namespace StreamSchedule.Markov2;

public static class Markov
{
    public static readonly Queue<string> TokenizationQueue = new();

    private static Dictionary<int, Token> TokenLookup = [];
    private static Dictionary<int, List<TokenPair>> TokenPairLookup = [];
    private static Dictionary<int, List<TokenPair>> ReverseTokenPairLookup = [];

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
        _ = new Saver();
        _ = new Tokenizer();
        IsReady = true;
    }

    private sealed class Saver : Periodic
    {
        protected override void Update()
        {
            if (DateTime.UtcNow - lastSave <= saveInterval) return;
            Save();
            lastSave = DateTime.UtcNow;
            BotCore.Nlog.Info("Markov save cycle");
        }
    }

    private sealed class Tokenizer : Periodic
    {
        protected override void Update()
        {
            if (TokenizationQueue.Count <= 0 || !IsReady) return;

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

                    if (ReverseTokenPairLookup.TryGetValue(next.TokenID, out List<TokenPair>? tempReverse))
                        tempReverse.Add(tp);
                    else
                        ReverseTokenPairLookup.Add(next.TokenID, [tp]);

                    continue;
                }

                TokenPairLookup.TryGetValue(current.TokenID, out List<TokenPair>? temp);
                TokenPair? pairWithNext = temp?.FirstOrDefault(x => x.NextTokenID == next.TokenID);
                if (pairWithNext is null)
                {
                    pairWithNext = new TokenPair(current.TokenID, next.TokenID, 1);
                    if (temp is not null)
                        TokenPairLookup[current.TokenID].Add(pairWithNext);
                    else
                        TokenPairLookup.Add(current.TokenID, [pairWithNext]);

                    if (ReverseTokenPairLookup.TryGetValue(next.TokenID, out List<TokenPair>? tempReverse))
                        tempReverse.Add(pairWithNext);
                    else
                        ReverseTokenPairLookup.Add(next.TokenID, [pairWithNext]);
                }
                else { pairWithNext.Count++; }
            }
        }
        catch (Exception e) { BotCore.Nlog.Error(e); }
    }

    public static TimeSpan Save()
    {
        long startSave = Stopwatch.GetTimestamp();

        context.Tokens.AddRange(TokenLookup.Where(t => !context.Tokens.Contains(t.Value)).Select(t => t.Value));

        foreach (KeyValuePair<int, List<TokenPair>> tp in TokenPairLookup)
            context.TokenPairs.AddRange(tp.Value.Where(t => !context.TokenPairs.Contains(t)));

        context.SaveChanges();
        TimeSpan elapsed = Stopwatch.GetElapsedTime(startSave);
        BotCore.Nlog.Info($"markov save took {elapsed.TotalSeconds} s");

        return elapsed;
    }

    public static TimeSpan Load()
    {
        long startLoad = Stopwatch.GetTimestamp();
        IsReady = false;
        TokenLookup = [];
        TokenPairLookup = [];
        ReverseTokenPairLookup = [];

        if (!context.Tokens.Any())
            TokenLookup[0] = new Token(0, "\e");

        ILookup<int, TokenPair> tokenPairsPerToken = context.TokenPairs.AsNoTracking().ToLookup(t => t.TokenID);
        ILookup<int, TokenPair> tokenPairsPerNext = context.TokenPairs.AsNoTracking().ToLookup(t => t.NextTokenID);

        foreach (Token token in context.Tokens.AsNoTracking())
        {
            TokenPairLookup.Add(token.TokenID, [.. tokenPairsPerToken[token.TokenID]]);
            ReverseTokenPairLookup.Add(token.TokenID, [.. tokenPairsPerNext[token.TokenID]]);
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

        random = new Random(seed ?? (DateTime.Now.Minute + DateTime.Now.Millisecond) / (DateTime.Now.Hour + 1) );

        Token? source = null;
        if (!string.IsNullOrWhiteSpace(firstWord)) source = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(firstWord)).Value;
        source ??= TokenLookup[random.Next(0, TokenLookup.Count)];

        List<int> generatedTokens = [source.TokenID];

        if (method.HasFlag(Method.include))
        {
            List<int> generatedLowerHalf = [source.TokenID];
            method &= ~Method.reverse;

            bool forwardFailed = false;
            bool reverseFailed = false;

            while (generatedLowerHalf.Count + generatedTokens.Count - 1 < maxLength)
            {
                if (forwardFailed && reverseFailed) break;

                bool forward;
                if (Random.Shared.Next(101) >= 50)
                {
                    forward = true;
                    if (forwardFailed) forward = false;
                }
                else
                {
                    forward = false;
                    if (reverseFailed) forward = true;
                }

                if (forward)
                    forwardFailed = generatedTokens.PickNext(method);
                else
                    reverseFailed = generatedLowerHalf.PickNext(method | Method.reverse);
            }

            generatedLowerHalf.RemoveAt(0);
            generatedLowerHalf.Reverse();
            generatedTokens = [..generatedLowerHalf, ..generatedTokens];
        }
        else
        {
            while (generatedTokens.Count < maxLength)
                if (generatedTokens.PickNext(method))
                    break;

            if (method.HasFlag(Method.reverse)) generatedTokens.Reverse();
        }

        string result = "";

        foreach (int tokenID in generatedTokens) result += TokenLookup[tokenID].Value + " ";

        return result;
    }

    private static bool PickNext(this List<int> sequence, Method method)
    {
        bool reverse = method.HasFlag(Method.reverse);
        bool force = method.HasFlag(Method.force);

        List<TokenPair>? pairs = null;
        if (reverse) ReverseTokenPairLookup.TryGetValue(sequence.Last(), out pairs);
        else TokenPairLookup.TryGetValue(sequence.Last(), out pairs);
        if (pairs is null || pairs.Count == 0) return true;
        if (force)
        {
            pairs.RemoveAll(tp => tp.NextTokenID == eolID);
            if (pairs.Count == 0) return true;
        }

        TokenPair next = method switch
        {
            _ when method.HasFlag(Method.ordered) => Ordered(),
            _ when method.HasFlag(Method.weighted) => Weighted(),
            _ when method.HasFlag(Method.random) => Random(),
            _ => Random()
        };

        sequence.Add(reverse ? next.TokenID : next.NextTokenID);
        return false;

        TokenPair Ordered() => pairs.OrderByDescending(x => x.Count).ElementAt(random.Next(0, random.Next(0, pairs.Count + 1)));

        TokenPair Weighted() => pairs.OrderBy(x => x.Count).First(tp => tp.Count >= random.Next(0, (pairs.MaxBy(x => x.Count)?.Count ?? 2) + 1));

        TokenPair Random() => pairs[random.Next(0, pairs.Count)];
    }
}