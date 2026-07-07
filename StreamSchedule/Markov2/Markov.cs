using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
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

    private static bool Ready = false;

    private static int eolID = 0;
    private static int bolID = 0;
    public static bool Start => true;

    private static DateTime lastSave = DateTime.UtcNow;
    private static readonly TimeSpan saveInterval = TimeSpan.FromMinutes(30);
    
    static Markov()
    {
        context.Database.EnsureCreated();
        Task.Run(FirstLoader);
    }

    private static async Task FirstLoader()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        BotCore.Nlog.Info("Loading markov");
        
        Token? bol = context.Tokens.FirstOrDefault(t => t.Value.Equals("\r"));
        if (bol is null)
        {
            bol = new Token(context.Tokens.Count(), "\r");
            context.Tokens.Add(bol);
            bolID = bol.TokenID;
            await context.SaveChangesAsync();
        }
        else bolID = bol.TokenID;

        Token? eol = context.Tokens.FirstOrDefault(t => t.Value.Equals("\e"));
        if (eol is null)
        {
            eol = new Token(context.Tokens.Count(), "\e");
            context.Tokens.Add(eol);
            eolID = eol.TokenID;
            await context.SaveChangesAsync();
        }
        else eolID = eol.TokenID;
        
        Load();
        _ = new Saver();
        Task.Run(Tokenizer);
        Ready = true;
    }

    private sealed class Saver : Periodic
    {
        protected override Task Update()
        {
            if (DateTime.UtcNow - lastSave <= saveInterval) return Task.CompletedTask;
            Task.Run(Save);
            lastSave = DateTime.UtcNow;
            BotCore.Nlog.Info("Markov save cycle");
            return Task.CompletedTask;
        }
    }

    private static async Task Tokenizer()
    {
        while (true)
        {
            if (TokenizationQueue.Count <= 0 || !Ready)
            {
                await Task.Delay(50);
                continue;
            }
            await TokenizeMessage(TokenizationQueue.Peek());
            TokenizationQueue.Dequeue();
        }
    }
    
    public static TimeSpan Save()
    {
        Ready = false;
        long startSave = Stopwatch.GetTimestamp();
        context.SaveChanges();
        context.ChangeTracker.Clear();
        TimeSpan elapsed = Stopwatch.GetElapsedTime(startSave);
        BotCore.Nlog.Info($"markov save took {elapsed.TotalSeconds} s");

        Ready = true;
        return elapsed;
    }

    public static TimeSpan Load()
    {
        long startLoad = Stopwatch.GetTimestamp();
        Ready = false;
        TokenLookup = [];
        TokenPairLookup = [];
        ReverseTokenPairLookup = [];
        
        IEnumerable<TokenPair> pairs = context.TokenPairs.AsNoTracking().ToList();
        
        ILookup<int, TokenPair> tokenPairsPerToken = pairs.ToLookup(t => t.TokenID);
        ILookup<int, TokenPair> tokenPairsPerNext = pairs.ToLookup(t => t.NextTokenID);

        foreach (Token token in context.Tokens)
        {
            TokenPairLookup.Add(token.TokenID, [.. tokenPairsPerToken[token.TokenID]]);
            ReverseTokenPairLookup.Add(token.TokenID, [.. tokenPairsPerNext[token.TokenID]]);
            TokenLookup.Add(token.TokenID, token);
        }

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startLoad);
        BotCore.Nlog.Info($"markov load took {elapsed.Seconds} s");
        Ready = true;
        return elapsed;
    }
    
    public static void DumpTokenStats(string token, bool reverse)
    {
        if (!Ready) return;
        Token? t = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(token)).Value;
        if (t is null) return;
        List<TokenPair>? pairs;
        if (reverse) ReverseTokenPairLookup.TryGetValue(t.TokenID, out pairs);
        else TokenPairLookup.TryGetValue(t.TokenID, out pairs);
        if (pairs is null) return;

        string result = "\n";
        if (reverse)
        {
            foreach (var tokenID in pairs.Select(p => new { id = p.TokenID, count = p.Count, tpID = p.ID}).OrderBy(pp => pp.count))
            {
                Token tt = TokenLookup[tokenID.id];
                result += $"{tokenID.count} - (pair id: {tokenID.tpID} token id:{tt.TokenID}) - {tt.Value} {t.Value} \r\n ";
            }
        }
        else
        {
            foreach (var tokenID in pairs.Select(p => new { id = p.NextTokenID, count = p.Count, tpID = p.ID}).OrderBy(pp => pp.count))
            {
                Token tt = TokenLookup[tokenID.id];
                result += $"{tokenID.count} - (pair id: {tokenID.tpID} token id:{tt.TokenID}) - {t.Value} {tt.Value} \r\n ";
            }
        }
        BotCore.Nlog.Info(result + $"{pairs.Count} total");
    }

    public static void Cleanup()
    {
        Ready = false;
        int c = 0;

        for (int i = 0; i < TokenPairLookup.Count; i++)
        {        
            bool clean = false;

            while (!clean)
            {
                clean = true;
                List<TokenPair> tokenPairs = TokenPairLookup[i];

                for (int j = 0; j < tokenPairs.Count; j++)
                {
                    TokenPair tokenPair = tokenPairs[j];
                    for (int k = 0; k < tokenPairs.Count; k++)
                    {
                        TokenPair pair = tokenPairs[k];
                        if (tokenPair.NextTokenID != pair.NextTokenID) continue;
                        if (tokenPair.ID == pair.ID) continue;
                        clean = false;
                        
                        if (context.Entry(tokenPair).State == EntityState.Detached)
                            context.TokenPairs.Attach(tokenPair);

                        tokenPair.Count += pair.Count;

                        if (context.Entry(pair).State == EntityState.Detached)
                            context.TokenPairs.Attach(pair);
                        
                        tokenPairs.Remove(pair);
                        context.TokenPairs.Remove(pair);
                        c++;
                    }
                }

                BotCore.Nlog.Info($"{i} / {TokenPairLookup.Count}");
            }
        }

        Save();
        Load();
        
        BotCore.Nlog.Info($"ok {c}");
        Ready = true;
    }
    
    private static Task TokenizeMessage(string message)
    {
        List<string> words = message.Split(' ').Prepend("\r").ToList();
        for (int i = 0; i < words.Count; i++)
        {
            string nextWord = (i + 1 >= words.Count) ? "\e" : words[i + 1];
            Token? next = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(nextWord)).Value;

            if (next is null)
            {
                next = new Token(TokenLookup.Count, nextWord);
                TokenLookup.Add(next.TokenID, next);
                TokenPairLookup.Add(next.TokenID, []);
                context.Tokens.Add(next);
            }

            Token? current = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(words[i])).Value;
            if (current is null)
            {
                current = new Token(TokenLookup.Count, words[i]);
                TokenLookup.Add(current.TokenID, current);
                context.Tokens.Add(current);

                TokenPair tp = new TokenPair(current.TokenID, next.TokenID, 1);
                TokenPairLookup.Add(current.TokenID, [tp]);
                context.TokenPairs.Add(tp);

                if (ReverseTokenPairLookup.TryGetValue(next.TokenID, out List<TokenPair>? tempReverse))
                    tempReverse.Add(tp);
                else
                    ReverseTokenPairLookup.Add(next.TokenID, [tp]);

                continue;
            }

            TokenPairLookup.TryGetValue(current.TokenID, out List<TokenPair>? pairsOfCurrent);
            TokenPair? pairWithNext = pairsOfCurrent?.FirstOrDefault(x => x.NextTokenID == next.TokenID);
            if (pairWithNext is null)
            {
                pairWithNext = new TokenPair(current.TokenID, next.TokenID, 1);
                
                if (pairsOfCurrent is not null)
                    pairsOfCurrent.Add(pairWithNext);
                else
                    TokenPairLookup.Add(current.TokenID, [pairWithNext]);

                if (ReverseTokenPairLookup.TryGetValue(next.TokenID, out List<TokenPair>? tempReverse))
                    tempReverse.Add(pairWithNext);
                else
                    ReverseTokenPairLookup.Add(next.TokenID, [pairWithNext]);
                
                context.TokenPairs.Add(pairWithNext);
            }
            else
            {
                if (context.Entry(pairWithNext).State == EntityState.Detached)
                    context.TokenPairs.Attach(pairWithNext);
                pairWithNext.Count++;
            }
        }
        return Task.CompletedTask;
    }
    
    private static Func<int, int> Rnd = Random.Shared.Next;
    
    public static string GenerateSequence(string? firstWord = null, int maxLength = 25, Method method = Method.weighted, int? seed = null)
    {
        if (!Ready) return "uuh ";
        
        Rnd = seed == null ? Random.Shared.Next : new Random(seed ?? 1).Next;
        
        Token? source = null;
        if (!string.IsNullOrWhiteSpace(firstWord)) source = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(firstWord)).Value;
        source ??= TokenLookup[Rnd(TokenLookup.Count)];

        List<int> generatedTokens = [source.TokenID];

        if (method.HasFlag(Method.include))
        {
            List<int> generatedLowerHalf = [source.TokenID];
            method &= ~Method.reverse;

            bool forwardStopped = false;
            bool reverseStopped = false;
            
            bool forward = true;
            int generatedCount = 1;
            
            while (generatedCount < maxLength)
            {
                if (forwardStopped && reverseStopped) break;
                if (forwardStopped) forward = false;
                if (reverseStopped) forward = true;
                
                if (forward)
                {
                    forwardStopped = generatedTokens.PickNext(method, out bool needExtra, pickLast:(maxLength - generatedCount <= 2));
                    if (needExtra)
                    {
                        forwardStopped = false;
                        forward = true;
                        continue;
                    }
                }
                else
                    reverseStopped = generatedLowerHalf.PickNext(method | Method.reverse, out _);
                
                generatedCount++;
                forward = !forward;
            }
            
            generatedLowerHalf.RemoveAt(0);
            generatedLowerHalf.Reverse();
            generatedTokens = [..generatedLowerHalf, ..generatedTokens];
        }
        else
        {
            while (generatedTokens.Count < maxLength)
            {
                if (generatedTokens.PickNext(method, out bool needExtra, generatedTokens.Count == maxLength - 1) || needExtra)
                {
                    if (needExtra) maxLength += 1;
                    else break;
                }
            }
            if (method.HasFlag(Method.reverse)) generatedTokens.Reverse();
        }

        string result = "";

        foreach (int tokenID in generatedTokens) result += TokenLookup[tokenID].Value + " ";

        return result.Replace("\e", "").Replace("\r", "");
    }

    private static bool PickNext(this List<int> sequence, Method method, out bool needExtra, bool pickLast = false)
    {
        bool reverse = method.HasFlag(Method.reverse);
        bool force = method.HasFlag(Method.force);
        needExtra = false;
        
        List<TokenPair>? pairsWithLastInSequence = null;
        if (reverse) ReverseTokenPairLookup.TryGetValue(sequence.Last(), out pairsWithLastInSequence);
        else TokenPairLookup.TryGetValue(sequence.Last(), out pairsWithLastInSequence);
        if (pairsWithLastInSequence is null || pairsWithLastInSequence.Count == 0) return true;
        
        if (force && !pickLast)
        {
            pairsWithLastInSequence = pairsWithLastInSequence.Where(tp => tp.NextTokenID != eolID).ToList();
            if (pairsWithLastInSequence.Count == 0)
            {
                sequence.Add(eolID);
                return true;
            }
        }
        
        TokenPair pairWithNext = method switch
        {
            _ when method.HasFlag(Method.ordered) => Ordered(),
            _ when method.HasFlag(Method.random) => Random(),
            _ => Weighted()
        };
        
        if (pickLast & !reverse)
        {
            TokenPairLookup.TryGetValue(pairWithNext.NextTokenID, out List<TokenPair>? temp);
            if (temp is null || temp.Count == 0)
            {
                sequence.Add(pairWithNext.NextTokenID);
                return true;
            }
            if (temp.FirstOrDefault(tp => tp.NextTokenID == eolID) is null) needExtra = true;
        }
        
        sequence.Add(reverse ? pairWithNext.TokenID : pairWithNext.NextTokenID);
        return false;

        TokenPair Ordered() => pairsWithLastInSequence.OrderByDescending(x => x.Count).ElementAt(Rnd(Rnd(pairsWithLastInSequence.Count + 1)));
        TokenPair Weighted_() => pairsWithLastInSequence.OrderBy(x => x.Count).First(tp => tp.Count >= Rnd((pairsWithLastInSequence.MaxBy(x => x.Count)?.Count ?? 2) + 1));
        TokenPair Weighted__() => pairsWithLastInSequence.Select(tp => new { count = tp.Count, pair = tp }).OrderByDescending(tpc => tpc.count).First(tpc => tpc.count >= Rnd(pairsWithLastInSequence.MaxBy(tp => tp.Count)?.Count ?? 0)).pair;
        TokenPair Weighted()
        {
            int sum = pairsWithLastInSequence.Sum(p => p.Count);
            int rnd = Rnd(sum);
            int c = 0;
            foreach (TokenPair tokenPair in pairsWithLastInSequence)
            {
                c += tokenPair.Count;
                if (c >= rnd) return tokenPair;
            }
            return pairsWithLastInSequence.Last();
        }
        TokenPair Random() => pairsWithLastInSequence[Rnd(pairsWithLastInSequence.Count)];
    }
}
