using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NeoSmart.Unicode;
using StreamSchedule.Data;
using StreamSchedule.Markov2;
using StreamSchedule.Markov2.Data;

namespace StreamSchedule.Commands;

internal class Scramble : Command
{
    public override string Call => "unscramble";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => $"scramble, try to get a word of [c] length ({minCount} - {maxCount})";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.HalfAMinute);
    public override string[] Arguments => ["c", "m"];
    public override List<string> Aliases { get; set; } = [];

    private static readonly Dictionary<string, ActiveGame> activeGames = [];
    private static readonly MarkovContext context = new(new DbContextOptionsBuilder<MarkovContext>().UseSqlite("Data Source=Markov2.data").Options);
    private static readonly Random random = new();
    private static bool muted = false;
    private const int minCount = 3;
    private const int maxCount = 15;
    
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        if (activeGames.TryGetValue(message.ChannelID, out _))
            return Task.FromResult(new CommandResult("✋ Awkward a game is already in progress "));

        _ = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> args);
        
        if (args.TryGetValue("m", out _) && message.Sender.Privileges >= Privileges.Mod) muted = !muted;

        if (muted) return Task.FromResult(new CommandResult());
        
        int desiredCount;
        {
            int def = Random.Shared.Next(minCount, maxCount + 1);
            desiredCount = args.TryGetValue("c", out string? cc) ? int.TryParse(cc, out int c) ? Math.Clamp(c, minCount, maxCount) : def : def;
        }
        
        long timeStart = Stopwatch.GetTimestamp();
        bool ok = false;
        string word = "";
        int attempts = 0;
        
        List<Token> candidates = [.. context.Tokens.Where(tt => tt.Value.Length == desiredCount).AsNoTracking()];

        while (!ok)
        {
            if (!candidates.Any())
            {
                desiredCount = Math.Clamp(desiredCount - 1, minCount, maxCount);
                attempts++;
                candidates = [.. context.Tokens.Where(tt => tt.Value.Length == desiredCount).AsNoTracking()];
                continue;
            }

            Token tt = candidates.ElementAt(Random.Shared.Next(candidates.Count));
            
            IQueryable<TokenPair> usedIn = context.TokenPairs.Where(tp => tp.Count >= 5 && (tp.TokenID == tt.TokenID || tp.NextTokenID == tt.TokenID)).AsNoTracking();

            if (!usedIn.Any())
            {
                attempts++;
                continue;
            }
            
            word = tt.Value;
            ok = true;
        }

        Codepoint[] w = word.Codepoints().ToArray();
        
        string shuffled = "";
        bool picked = false;
        int shuffleCount = 0;
        while (!picked)
        {
            random.Shuffle(w);
            shuffled = string.Join("", w.Select(c => c.AsString()));
            shuffleCount++;
            if (BadWords.Contains(shuffled)) continue;
            picked = true;
        }
        
        activeGames[message.ChannelID] = new ActiveGame(word, message.ChannelName, message.ChannelID);
        
        BotCore.Nlog.Info($"picked {word} in {Stopwatch.GetElapsedTime(timeStart)} in {attempts}, shuffling {shuffleCount} times");
        return Task.FromResult(new CommandResult($"Unscramble this: \" {shuffled} \" you have 30s. "));
    }

    public static void CheckWord(UniversalMessageInfo message)
    {
        if (activeGames.TryGetValue(message.ChannelID, out ActiveGame? game))
            game.TryWord(message.Content.Split(" ").FirstOrDefault()?.ToLower() ?? "") ;
    }
    
    private class ActiveGame
    {
        private ActiveGame()
        {
            word = "";
            channelName = "";
            channelID = "";
        }
        
        private readonly CancellationTokenSource cts = new();
        private readonly string word;
        private readonly string channelName;
        private readonly string channelID;
        
        public ActiveGame(string word, string channelName, string channelID) : base()
        {
            this.word = word;
            this.channelName = channelName;
            this.channelID = channelID;
            Task.Run(() => GameTimer(this, cts.Token));
        }

        public void TryWord(string w)
        {
            if (!w.Equals(word, StringComparison.CurrentCultureIgnoreCase)) return;
            BotCore.OutQueuePerChannel[channelName].Enqueue(new CommandResult($"FeelsGoodMan the word was \" {word} \" "));
            cts.Cancel();
            activeGames.Remove(channelID);
            cts.Dispose();
        }

        internal void ExpireGame()
        {
            BotCore.OutQueuePerChannel[channelName].Enqueue(new CommandResult($"Awkward time is out, the word was \" {word} \" "));
            activeGames.Remove(channelID);
            cts.Dispose();
        }
    }
    
    private static async Task GameTimer(ActiveGame game, CancellationToken token)
    {
        DateTime timeStart = DateTime.UtcNow;
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(100, token);
            if (DateTime.UtcNow - timeStart < TimeSpan.FromSeconds(30)) continue;
            
            game.ExpireGame();
            return;
        }
    }
}
