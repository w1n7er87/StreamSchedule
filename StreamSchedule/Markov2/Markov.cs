using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StreamSchedule.Markov2.Data;

namespace StreamSchedule.Markov2;

public static partial class Markov
{
    public static readonly Queue<string> TokenizationQueue = new();

    private static Dictionary<int, Token> TokenLookup = [];
    private static Dictionary<int, List<TokenPair>> TokenPairLookup = [];
    private static Dictionary<int, List<TokenPair>> ReverseTokenPairLookup = [];

    private static Func<int, int> Rnd = Random.Shared.Next;
    private static Func<double> Rndd = Random.Shared.NextDouble;

    public static int TokenCount => TokenLookup.Count;
    public static int TokenPairCount => context.TokenPairs.Count();

    private static readonly MarkovContext context = new(new DbContextOptionsBuilder<MarkovContext>().UseSqlite("Data Source=Markov2.data").Options);

    private static bool Ready = false;

    private static int eolID = 0;
    private static int bolID = 0;
    public static bool Start => true;

    private static DateTime lastSave = DateTime.UtcNow;
    private static readonly TimeSpan saveInterval = TimeSpan.FromMinutes(30);

    private const int absoluteMaxCount = 125;

    static Markov()
    {
        context.Database.EnsureCreated();
        Task.Run(FirstLoader);
    }

    private static async Task FirstLoader()
    {
        await Task.Delay(TimeSpan.FromSeconds(7));
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
            foreach (var tokenID in pairs.Select(p => new { id = p.TokenID, count = p.Count, tpID = p.ID }).OrderBy(pp => pp.count))
            {
                Token tt = TokenLookup[tokenID.id];
                result += $"{tokenID.count} - (pair id: {tokenID.tpID} token id:{tt.TokenID}) - {tt.Value} {t.Value} \r\n ";
            }
        }
        else
        {
            foreach (var tokenID in pairs.Select(p => new { id = p.NextTokenID, count = p.Count, tpID = p.ID }).OrderBy(pp => pp.count))
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

                        if (context.Entry(tokenPair).State == EntityState.Detached) context.TokenPairs.Attach(tokenPair);

                        tokenPair.Count += pair.Count;

                        if (context.Entry(pair).State == EntityState.Detached) context.TokenPairs.Attach(pair);

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
        message = Exclamations().Replace(Questions().Replace(message, "???"), "!!!");

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

                if (ReverseTokenPairLookup.TryGetValue(next.TokenID, out List<TokenPair>? tempReverse)) tempReverse.Add(tp);
                else ReverseTokenPairLookup.Add(next.TokenID, [tp]);

                continue;
            }

            TokenPairLookup.TryGetValue(current.TokenID, out List<TokenPair>? pairsOfCurrent);
            TokenPair? pairWithNext = pairsOfCurrent?.FirstOrDefault(x => x.NextTokenID == next.TokenID);
            if (pairWithNext is null)
            {
                pairWithNext = new TokenPair(current.TokenID, next.TokenID, 1);

                if (pairsOfCurrent is not null) pairsOfCurrent.Add(pairWithNext);
                else TokenPairLookup.Add(current.TokenID, [pairWithNext]);

                if (ReverseTokenPairLookup.TryGetValue(next.TokenID, out List<TokenPair>? tempReverse)) tempReverse.Add(pairWithNext);
                else ReverseTokenPairLookup.Add(next.TokenID, [pairWithNext]);

                context.TokenPairs.Add(pairWithNext);
            }
            else
            {
                if (context.Entry(pairWithNext).State == EntityState.Detached) context.TokenPairs.Attach(pairWithNext);
                pairWithNext.Count++;
            }
        }

        return Task.CompletedTask;
    }

    public static string GenerateSequence(string? firstWord = null, int k = 9999, float temperature = 1f, int maxLength = 25, Method method = Method.none, int? seed = null)
    {
        if (!Ready) return "uuh ";

        Rnd = seed == null ? Random.Shared.Next : new Random(seed ?? 1).Next;
        Rndd = seed == null ? Random.Shared.NextDouble : new Random(seed ?? 1).NextDouble;

        Token? source = null;
        if (!string.IsNullOrWhiteSpace(firstWord)) source = TokenLookup.FirstOrDefault(t => t.Value.Value.Equals(firstWord)).Value;
        source ??= TokenLookup[Rnd(TokenLookup.Count)];

        List<int> generatedTokens = [source.TokenID];

        if (method.HasFlag(Method.include))
        {
            List<int> generatedLowerHalf = [source.TokenID];
            method &= ~Method.reverse;

            int generatedForward = 1;
            int generatedBackward = 0;
            int maxLengthForward = maxLength / 2;
            int maxLengthReverse = maxLength / 2;

            if (maxLength % 2 != 0) maxLengthForward++;

            while (generatedForward < maxLengthForward)
                if (generatedTokens.PickNextForward(method, generatedTokens.Count, ref maxLengthForward, k, temperature)) break;
                else generatedForward++;

            while (generatedBackward < maxLengthReverse)
                if (generatedLowerHalf.PickNextReverse(method, generatedLowerHalf.Count, ref maxLengthReverse, k, temperature)) break;
                else generatedBackward++;

            generatedLowerHalf.RemoveAt(0);
            generatedLowerHalf.Reverse();
            generatedTokens = [.. generatedLowerHalf, .. generatedTokens];
        }
        else
        {
            while (generatedTokens.Count < maxLength)
                if (generatedTokens.PickNextForward(method, generatedTokens.Count, ref maxLength, k, temperature)) break;

            if (method.HasFlag(Method.reverse)) generatedTokens.Reverse();
        }

        string result = "";

        foreach (int tokenID in generatedTokens) result += TokenLookup[tokenID].Value + " ";

        return result.Replace("\e", "").Replace("\r", "");
    }

    private static bool PickNextForward(this List<int> sequence, Method method, int count, ref int targetCount, int k, double temperature)
    {
        bool force = method.HasFlag(Method.force);

        TokenPairLookup.TryGetValue(sequence.Last(), out List<TokenPair>? pairsWithLastInSequence);
        if (pairsWithLastInSequence is null || pairsWithLastInSequence.Count == 0) return true;
        if (force)
        {
            pairsWithLastInSequence = pairsWithLastInSequence.Where(tp => tp.NextTokenID != eolID).ToList();
            if (pairsWithLastInSequence.Count == 0)
            {
                sequence.Add(eolID);
                return true;
            }
        }

        TokenPair pairWithNext = SampleTopKWithTemperature(pairsWithLastInSequence, targetCount, temperature, count, force, k, false);

        sequence.Add(pairWithNext.NextTokenID);

        if (targetCount - count == 1)
        {
            TokenPairLookup.TryGetValue(sequence.Last(), out List<TokenPair>? pairsWithSelected);
            pairsWithSelected ??= [];
            if (pairsWithSelected.Count != 0 && pairsWithSelected.All(tp => tp.NextTokenID != eolID)) targetCount = Math.Min(absoluteMaxCount, targetCount + 1);
        }

        return false;
    }

    private static bool PickNextReverse(this List<int> sequence, Method method, int count, ref int targetCount, int k, double temperature)
    {
        bool force = method.HasFlag(Method.force);

        ReverseTokenPairLookup.TryGetValue(sequence.Last(), out List<TokenPair>? pairsWithLastInSequence);
        if (pairsWithLastInSequence is null || pairsWithLastInSequence.Count == 0) return true;
        if (force)
        {
            pairsWithLastInSequence = pairsWithLastInSequence.Where(tp => tp.TokenID != bolID).ToList();
            if (pairsWithLastInSequence.Count == 0)
            {
                sequence.Add(bolID);
                return true;
            }
        }

        TokenPair pairWithNext = SampleTopKWithTemperature(pairsWithLastInSequence, targetCount, temperature, count, force, k, true);

        sequence.Add(pairWithNext.TokenID);

        if (targetCount - count == 1)
        {
            ReverseTokenPairLookup.TryGetValue(sequence.Last(), out List<TokenPair>? pairsWithSelected);
            pairsWithSelected ??= [];
            if (pairsWithSelected.Count != 0 && pairsWithSelected.All(tp => tp.TokenID != bolID)) targetCount = Math.Min(absoluteMaxCount, targetCount + 1);
        }

        return false;
    }

    private static TokenPair SampleTopKWithTemperature(List<TokenPair> pairsWithLastInSequence, int max, double temperature, int count, bool force, int k, bool reverse)
    {
        var scoredPairs = pairsWithLastInSequence.Select(p => new { Pair = p, Score = Math.Exp(Math.Log(p.Count) / temperature) }).OrderByDescending(x => x.Score).ToList();

        int terminationOffset = 6;
        int softRampStartTokenIndex = max - terminationOffset;

        int actualK = Math.Min(k, scoredPairs.Count);
        var topKPairs = scoredPairs.Take(actualK).ToList();

        if (!force && count >= softRampStartTokenIndex && max > terminationOffset)
        {
            float runwayProgress = (float)(count - softRampStartTokenIndex + 1) / terminationOffset;
            var masterEolItem = reverse ? scoredPairs.FirstOrDefault(x => x.Pair.TokenID == bolID) : scoredPairs.FirstOrDefault(x => x.Pair.NextTokenID == eolID);

            if (masterEolItem != null)
            {
                double currentEom = masterEolItem.Score;
                double boostedScore = currentEom > 0.0 ? currentEom * (1.0f + runwayProgress * 2.0f) : currentEom + (runwayProgress * 15.0f);

                var boostedEolItem = masterEolItem with { Score = boostedScore };

                int existingIndex = reverse ? topKPairs.FindIndex(x => x.Pair.TokenID == bolID) : topKPairs.FindIndex(x => x.Pair.NextTokenID == eolID);

                if (existingIndex >= 0) topKPairs[existingIndex] = boostedEolItem;
                else topKPairs.Add(boostedEolItem);
            }
        }

        double totalScore = topKPairs.Sum(x => x.Score);
        double rndTarget = Rndd() * totalScore;
        double currentSum = 0;

        foreach (var item in topKPairs)
        {
            currentSum += item.Score;
            if (currentSum >= rndTarget) { return item.Pair; }
        }

        return topKPairs.Last().Pair;
    }

    [GeneratedRegex(@"\?{4,}")]
    private static partial Regex Questions();

    [GeneratedRegex(@"!{4,}")]
    private static partial Regex Exclamations();
}
