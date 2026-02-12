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
    public override Privileges Privileges => Privileges.Uuh;
    public override string Help => "scramble";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    private static readonly Dictionary<string, ActiveGame> activeGames = [];
    private static readonly MarkovContext context = new(new DbContextOptionsBuilder<MarkovContext>().UseSqlite("Data Source=Markov2.data").Options);
    private static readonly Random random = new();
    
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        if (activeGames.TryGetValue(message.ChannelID, out _))
            return Task.FromResult(new CommandResult("✋ Awkward a game is already in progress "));

        long timeStart = Stopwatch.GetTimestamp();
        bool ok = false;
        string word = "";
        int attempts = 0;
        while (!ok)
        {
            IQueryable<TokenPair> candidates = context.TokenPairs.Where(tp => tp.Count >= 5).AsNoTracking();
            TokenPair tp = candidates.ElementAt(Random.Shared.Next(candidates.Count()));
            word = context.Tokens.First(t => t.TokenID == tp.TokenID).Value;
            attempts++;
            if (word.Length < 3) continue;
            ok = true;
        }

        activeGames[message.ChannelID] = new ActiveGame(word, message.ChannelName, message.ChannelID);

        Codepoint[] w = word.Codepoints().ToArray();
        random.Shuffle(w);

        BotCore.Nlog.Info($"picked {word} in {Stopwatch.GetElapsedTime(timeStart)} in {attempts}");
        return Task.FromResult(new CommandResult($"Unscramble this: \" {string.Join("", w.Select(c => c.AsString()))} \" you have 30s. "));
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
