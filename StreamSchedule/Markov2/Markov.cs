using Microsoft.EntityFrameworkCore;
using StreamSchedule.Markov2.Data;

namespace StreamSchedule.Markov2;

public static class Markov
{
    public static readonly Queue<string> TokenizationQueue = new();
    
    private static Dictionary<int, Token> TokenLookup = [];
    private static Dictionary<int, List<TokenPair>> TokenPairLookup = [];

    private static readonly MarkovContext context = new(new DbContextOptionsBuilder<MarkovContext>().UseSqlite("Data Source=Markov2.data").Options);

    private static bool IsReady = false;

    static Markov()
    {
        context.Database.EnsureCreated();
        if (context.Tokens.Any())
        {
            Load();
        }
        else
        {
            TokenLookup[0] = new Token(0, "\e");
        }
        
        Task.Run(Tokenizer);
        IsReady = true;
    }

    private static void Tokenizer()
    {
        while (true)
        {
            if (TokenizationQueue.Count <= 0 || !IsReady)
            {
                Task.Delay(50);
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

    public static void Save()
    {
        IsReady = false;
        context.Tokens.AddRange(TokenLookup.Where(t => !context.Tokens.Contains(t.Value)).Select(t => t.Value));
        foreach (KeyValuePair<int, List<TokenPair>> tp in TokenPairLookup)
        {
            context.TokenPairs.AddRange(tp.Value.Where(t => !context.TokenPairs.Contains(t)));
        }

        context.SaveChanges();
        IsReady = true;
    }

    public static void Load()
    {
        IsReady = false;
        TokenPairLookup = new Dictionary<int, List<TokenPair>>();
        TokenLookup = new Dictionary<int, Token>();
        
        foreach (Token token in context.Tokens.OrderBy(t => t.TokenID))
        {
            TokenPairLookup[token.TokenID] = [.. context.TokenPairs.Where(tp => tp.TokenID == token.TokenID)];
            TokenLookup.Add(token.TokenID, token);
        }

        IsReady = true;
    }
    
    public static string GenerateSequence(string? firstWord = null, int maxLength = 25, Method method = Method.ordered)
    {
        Token? first = null;
        if (!string.IsNullOrWhiteSpace(firstWord)) first = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(firstWord)).Value;
        
        first ??= TokenLookup[Random.Shared.Next(0, TokenLookup.Count)];

        List<int> tokenIDs = method switch 
        {
            Method.ordered => PickOrdered(first.TokenID, maxLength),
            Method.weighted => PickWeighted(first.TokenID, maxLength),
            _ => PickRandom(first.TokenID, maxLength),
        };
        string result = "";
        
        foreach (int tokenID in tokenIDs)
        {
            result += TokenLookup[tokenID].Value + " ";
        }
        return result;
    }
    
    
    private static List<int> PickOrdered(int id, int maxLength)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            TokenPairLookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;

            int cut = Random.Shared.Next(0, p.Count + 1);
            sequence.Add(p.OrderByDescending(x => x.Count).ElementAt(Random.Shared.Next(0, cut)).NextTokenID);
        }
        return sequence;
    }

    private static List<int> PickWeighted(int id, int maxLength)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            TokenPairLookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;
            
            int max = p.MaxBy(x => x.Count)?.Count ?? 2;
            int cut = Random.Shared.Next(0, max + 1);
            sequence.Add(p.OrderBy(x => x.Count).First(x => x.Count >= cut).NextTokenID);
        }
        return sequence;
    }

    private static List<int> PickRandom(int id, int maxLength)
    {
        List<int> sequence = [id];
        for (int i = 1; i < maxLength; i++)
        {
            TokenPairLookup.TryGetValue(sequence[i - 1], out List<TokenPair>? p);
            if (p is null || p.Count == 0) return sequence;
            
            sequence.Add(p[Random.Shared.Next(0, p.Count)].NextTokenID);
        }
        return sequence;
    }
}
