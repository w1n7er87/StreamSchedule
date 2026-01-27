using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreamSchedule.Markov2.Data;

namespace StreamSchedule.Markov2;

public static class Markov
{
    public static readonly Queue<(string, bool)> TokenizationQueue = new();
    
    private static Dictionary<int, Token> TokenLookup = [];
    private static Dictionary<int, Token> TokenLookupOnline = [];
    private static Dictionary<int, List<TokenPair>> TokenPairLookup = [];
    private static Dictionary<int, List<TokenPair>> TokenPairLookupOnline = [];
    
    public static int TokenCount => TokenLookup.Count;
    public static int TokenCountOnline => TokenLookupOnline.Count;
    public static int TokenPairCount => context.TokenPairs.Count();
    public static int TokenPairCountOnline => context.TokenPairsOnline.Count();
    
    private static readonly MarkovContext context = new(new DbContextOptionsBuilder<MarkovContext>().UseSqlite("Data Source=Markov2.data").Options);

    private static bool IsReady = false;

    private static readonly int eolID = 0;
    private static readonly int eolIDOnline = 0;
    
    public static bool Start => true;

    private static DateTime lastSave = DateTime.UtcNow;
    private static readonly TimeSpan saveInterval = TimeSpan.FromHours(3);
    
    static Markov()
    {
        context.Database.EnsureCreated();
        
        Load();
        
        eolID = context.Tokens.FirstOrDefault(t => t.Value.Equals("\e"))?.TokenID ?? 0;
        eolIDOnline = context.TokensOnline.FirstOrDefault(t => t.Value.Equals("\e"))?.TokenID ?? 0;
        
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
    
    private static void TokenizeMessage((string, bool) massageState)
    {
        string message = massageState.Item1;
        bool online = massageState.Item2;
        Dictionary<int, Token> tokens = online ? TokenLookupOnline : TokenLookup;
        Dictionary<int, List<TokenPair>>  pairs = online ? TokenPairLookupOnline : TokenPairLookup;
        
        try
        {
            string[] words = message.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string nextWord = (i + 1 >= words.Length) ? "\e" : words[i + 1];
                Token? next = tokens.FirstOrDefault(t => t.Value.Value.Equals(nextWord)).Value;

                if (next is null)
                {
                    next = new Token(tokens.Count, nextWord);
                    tokens.Add(next.TokenID, next);
                    pairs.Add(next.TokenID, []);
                }
            
                Token? current = tokens.FirstOrDefault(t => t.Value.Value.Equals(words[i])).Value;
                if (current is null)
                {
                    current = new Token(tokens.Count, words[i]);
                    tokens.Add(current.TokenID, current);
                
                    TokenPair tp = new TokenPair(current.TokenID, next.TokenID, 1);
                    pairs.Add(current.TokenID, [tp]);
                    continue;
                }
            
                pairs.TryGetValue(current.TokenID, out List<TokenPair>? temp);
                TokenPair? pairWithNext = temp?.FirstOrDefault(x => x.NextTokenID == next.TokenID);
                if (pairWithNext is null)
                {
                    pairWithNext = new TokenPair(current.TokenID, next.TokenID, 1);
                    if (temp is not null)
                    {
                        pairs[current.TokenID].Add(pairWithNext);
                    }
                    else
                    {
                        pairs.Add(current.TokenID, [pairWithNext]);
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

        context.Tokens.AddRange(TokenLookup.Values.Except(context.Tokens));
        context.TokensOnline.AddRange(TokenLookupOnline.Values.Select(TokenOnline.FromToken).Except(context.TokensOnline));
        context.TokenPairs.AddRange(TokenPairLookup.Values.SelectMany(tp => tp).Except(context.TokenPairs));
        context.TokenPairsOnline.AddRange(TokenPairLookupOnline.Values.SelectMany(tp => tp.Select(TokenPairOnline.FromTokenPair)).Except(context.TokenPairsOnline));
        
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

        TokenLookup = [];
        TokenPairLookup = [];
        TokenLookupOnline = [];
        TokenPairLookupOnline = [];
        
        if (!context.TokensOnline.Any())
            TokenLookupOnline[0] = new Token(0, "\e");
        
        if (!context.Tokens.Any()) 
            TokenLookup[0] = new Token(0, "\e");
        
        List<TokenPair> tokenPairs = [.. context.TokenPairs.AsNoTracking()];
        List<TokenPair> tokenPairsOnline = [.. context.TokenPairsOnline.AsNoTracking()];

        foreach (Token token in context.Tokens)
        {
            TokenPairLookup[token.TokenID] = [.. tokenPairs.Where(tp => tp.TokenID == token.TokenID)];
            TokenLookup.Add(token.TokenID, token);
        }

        foreach (Token token in context.TokensOnline)
        {
            TokenPairLookupOnline[token.TokenID] = [.. tokenPairsOnline.Where(tp => tp.TokenID == token.TokenID)];
            TokenLookupOnline.Add(token.TokenID, token);
        }

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startLoad);
        BotCore.Nlog.Info($"markov load took {elapsed.Seconds} s");
        IsReady = true;
        return elapsed;
    }

    public static string GenerateSequence(string? firstWord = null, int maxLength = 25, Method method = Method.ordered, bool forceNoLineEnd = false, bool online = false)
    {
        if (!IsReady) return "uuh ";

        Dictionary<int, Token> tokens = online ? TokenLookupOnline : TokenLookup;
        Dictionary<int, List<TokenPair>>  pairs = online ? TokenPairLookupOnline : TokenPairLookup;

        int eol = online ? eolIDOnline : eolID;
        
        Token? first = null;
        if (!string.IsNullOrWhiteSpace(firstWord)) first = tokens.FirstOrDefault(t => t.Value.Value.Equals(firstWord)).Value;

        first ??= tokens[Random.Shared.Next(0, tokens.Count)];

        List<int> tokenIDs = method switch
        {
            Method.ordered => PickOrdered(first.TokenID, maxLength, forceNoLineEnd, pairs, eol),
            Method.weighted => PickWeighted(first.TokenID, maxLength, forceNoLineEnd, pairs, eol),
            _ => PickRandom(first.TokenID, maxLength, forceNoLineEnd, pairs, eol),
        };
        string result = "";

        foreach (int tokenID in tokenIDs) result += tokens[tokenID].Value + " ";

        return result;
    }

    private static List<int> PickOrdered(int id, int maxLength, bool forceNoLineEnd, Dictionary<int, List<TokenPair>>  lookup, int eol)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            lookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;
            if (forceNoLineEnd)
            {
                p.RemoveAll(t => t.NextTokenID == eol);
                if (p.Count == 0) return sequence;
            }

            int cut = Random.Shared.Next(0, p.Count + 1);
            sequence.Add(p.OrderByDescending(x => x.Count).ElementAt(Random.Shared.Next(0, cut)).NextTokenID);
        }
        return sequence;
    }

    private static List<int> PickWeighted(int id, int maxLength, bool forceNoLineEnd, Dictionary<int, List<TokenPair>>  lookup, int eol)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            lookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;
            if (forceNoLineEnd)
            {
                p.RemoveAll(t => t.NextTokenID == eol);
                if (p.Count == 0) return sequence;
            }
            
            int max = p.MaxBy(x => x.Count)?.Count ?? 2;
            int cut = Random.Shared.Next(0, max + 1);
            sequence.Add(p.OrderBy(x => x.Count).First(x => x.Count >= cut).NextTokenID);
        }
        return sequence;
    }

    private static List<int> PickRandom(int id, int maxLength, bool forceNoLineEnd, Dictionary<int, List<TokenPair>>  lookup, int eol)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            lookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;
            if (forceNoLineEnd)
            {
                p.RemoveAll(t => t.NextTokenID == eol);
                if (p.Count == 0) return sequence;
            }
            
            sequence.Add(p[Random.Shared.Next(0, p.Count)].NextTokenID);
        }
        return sequence;
    }
}
