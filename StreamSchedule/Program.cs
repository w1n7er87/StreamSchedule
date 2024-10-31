using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NeoSmart.Unicode;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Chat.Emotes;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace StreamSchedule;

public static class Program
{
    private static void Main(string[] args)
    {
        if (EF.IsDesignTime)
        {
            new HostBuilder().Build().Run();
            return;
        }
        _ = new BotCore(["vedal987", "w1n7er", "streamschedule"]);
        Console.ReadLine();
    }
}

internal class BotCore
{
    private BotCore()
    { }

    public static DatabaseContext DBContext { get; } = new(new DbContextOptionsBuilder<DatabaseContext>().UseSqlite("Data Source=StreamSchedule.data").Options);
    public static BotCore Instance { get; private set; }

    public TwitchAPI API { get; }
    public TwitchClient Client { get; }
    private readonly LiveStreamMonitorService _monitor;

    private const string _commandPrefixes = "!$?@#%^&`~><¡¿*-+_=;:'\"\\|/,.？！[]{}()";
     
    private readonly Dictionary<string, bool> _channelLiveState;

    private bool _sameMessage;

    private DateTime _textCommandLastUsed = DateTime.MinValue;

    public List<ChatMessage> MessageCache { get; } = [];
    private const int _cacheSize = 800;

    public static GlobalEmote[]? GlobalEmotes { get; private set; }

    private static int _dbSaveCounter = 0;
    private const int _dbUpdateCountInterval = 10;

    private async Task ConfigLiveMonitorAsync(string[] channelNames)
    {
        _monitor.SetChannelsByName([.. channelNames]);

        _monitor.OnStreamOnline += Monitor_OnLive;
        _monitor.OnStreamOffline += Monitor_OnOffline;
        _monitor.OnChannelsSet += Monitor_OnChannelsSet;
        _monitor.OnServiceStarted += Monitor_OnServiceStarted;
        _monitor.Start();

        await Task.Delay(-1);
    }

    public BotCore(string[] channelNames)
    {
        Instance = this;

        API = new TwitchAPI
        {
            Settings =
            {
                ClientId = Credentials.clientID,
                AccessToken = Credentials.oauth
            }
        };
        _monitor = new LiveStreamMonitorService(API, 5);

        Task.Run(() => ConfigLiveMonitorAsync(channelNames));

        DBContext.Database.EnsureCreated();

        Client = new TwitchClient();
        Client.Initialize(new(Credentials.username, Credentials.oauth), [.. channelNames]);
        Client.OnUnaccountedFor += Client_OnUnaccounted;
        Client.OnLog += Client_OnLog;
        Client.OnJoinedChannel += Client_OnJoinedChannel;
        Client.OnMessageReceived += Client_OnMessageReceived;
        Client.OnConnected += Client_OnConnected;
        Client.Connect();

        _channelLiveState = [];
        foreach (string channel in channelNames)
        {
            _channelLiveState[channel] = new();
        }

        Commands.Commands.InitializeCommands(channelNames, DBContext);
    }

    private async void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        User userSent = User.SyncToDb(e.ChatMessage.UserId, e.ChatMessage.Username, e.ChatMessage.UserType, DBContext);
        
        if (e.ChatMessage.Channel.Equals("vedal987")) 
        {
            _dbSaveCounter++;
            if (_channelLiveState[e.ChatMessage.Channel])
            {
                User.AddMessagesCounter(userSent, online: 1);
            }
            else
            {
                User.AddMessagesCounter(userSent, offline: 1);
            }

            if (_dbSaveCounter >= _dbUpdateCountInterval)
            {
                await DBContext.SaveChangesAsync();
                _dbSaveCounter = 0;
            }
        }

        string bypassSameMessage = _sameMessage ? " \U000e0000" : "";
        string? replyID = null;
        string trimmedMessage = e.ChatMessage.Message;
        if (e.ChatMessage.ChatReply != null)
        {
            replyID = e.ChatMessage.ChatReply.ParentMsgId;
            trimmedMessage = trimmedMessage[(e.ChatMessage.ChatReply.ParentDisplayName.Length + 2)..];
        }
        List<Codepoint> msgAsCodepoints = trimmedMessage.Codepoints().ToList();

        List<Codepoint> firstLetters = msgAsCodepoints.TakeWhile(x =>
            x == Emoji.ZeroWidthJoiner ||
            x == Emoji.ObjectReplacementCharacter ||
            x == Emoji.Keycap ||
            Emoji.IsEmoji(x.AsString()) ||
            Emoji.SkinTones.All.Any(y => x == y) ||
            x == Emoji.VariationSelector ||
            _commandPrefixes.Any(y => y == x)).ToList();

        if (firstLetters.Count == 0) return;

        msgAsCodepoints = msgAsCodepoints.Skip(firstLetters.Count).ToList();

        string restoredMessage = "";
        foreach (var l in msgAsCodepoints)
        {
            restoredMessage += l.AsString();
        }

        trimmedMessage = restoredMessage.TrimStart();

        if (trimmedMessage.Length < 2) return;

        string requestedCommand = trimmedMessage.Split(' ')[0];
        
        List<TextCommand> textCommands = [.. DBContext.TextCommands];

        if (DateTime.Now >= _textCommandLastUsed + TimeSpan.FromSeconds(5) && textCommands.Count > 0)
        {
            foreach (TextCommand command in textCommands)
            {
                if (!command.Name.Equals(requestedCommand, StringComparison.OrdinalIgnoreCase))
                {
                    if (command.Aliases is null) continue;
                    if (!command.Aliases.Any(x => x.Equals(requestedCommand, StringComparison.OrdinalIgnoreCase))) continue;
                }

                if (userSent.Privileges < command.Privileges) return;

                Console.WriteLine($"{TimeOnly.FromDateTime(DateTime.Now)} [{e.ChatMessage.Username}]:[{command.Name}]:[{command.Content}] ");
                Client.SendMessage(e.ChatMessage.Channel, command.Content + bypassSameMessage);
                _sameMessage = !_sameMessage;
                _textCommandLastUsed = DateTime.Now;
                return;
            }
        }

        if (_channelLiveState[e.ChatMessage.Channel]) return;

        MessageCache.Add(e.ChatMessage);
        if (MessageCache.Count > _cacheSize) { MessageCache.RemoveAt(0); }

        foreach (var c in Commands.Commands.CurrentCommands)
        {
            string usedCall = c.Call;
            if (!requestedCommand.Equals(c.Call, StringComparison.OrdinalIgnoreCase))
            {
                var aliases = DBContext.CommandAliases.Find(c.Call.ToLower());
                if (aliases?.Aliases is null || aliases.Aliases.Count == 0) continue;
                if (!aliases.Aliases.Any(x => x.Equals(requestedCommand, StringComparison.OrdinalIgnoreCase))) continue;
                usedCall = requestedCommand;
            }

            if (c.LastUsedOnChannel[e.ChatMessage.Channel] + c.Cooldown > DateTime.Now) return;

            if (userSent.Privileges < c.MinPrivilege) return;

            trimmedMessage = trimmedMessage.Replace(usedCall, "", StringComparison.OrdinalIgnoreCase).Replace("\U000e0000", "");

            CommandResult response = await c.Handle(new(userSent, trimmedMessage, replyID));

            Console.WriteLine($"{TimeOnly.FromDateTime(DateTime.Now)} [{e.ChatMessage.Username}]:[{c.Call}]:[{trimmedMessage}] - [{response}] ");

            if (string.IsNullOrEmpty(response.ToString())) return;

            if (response.reply)
            {
                Client.SendReply(e.ChatMessage.Channel, e.ChatMessage.ChatReply?.ParentMsgId ?? e.ChatMessage.Id, FixNineEleven(response.content) + bypassSameMessage);
            }
            else
            {
                Client.SendMessage(e.ChatMessage.Channel, FixNineEleven(response.content) + bypassSameMessage);
            }

            _sameMessage = !_sameMessage;
            c.LastUsedOnChannel[e.ChatMessage.Channel] = DateTime.Now;
            return;
        }
    }

    #region EVENTS

    private void Monitor_OnChannelsSet(object? sender, OnChannelsSetArgs e)
    {
        string r = "";
        foreach (var c in e.Channels) { r += c + ", "; }
        Console.WriteLine($"channels set {r}");
    }

    private void Monitor_OnServiceStarted(object? sender, OnServiceStartedArgs e)
    {
        Console.WriteLine("monitoring service stated");
    }

    private void Monitor_OnLive(object? sender, OnStreamOnlineArgs args)
    {
        _channelLiveState[args.Channel] = true;
        Console.WriteLine($"{args.Channel} went live");
    }

    private void Monitor_OnOffline(object? sender, OnStreamOfflineArgs args)
    {
        _channelLiveState[args.Channel] = false;
        Console.WriteLine($"{args.Channel} went offline");
    }

    private async void Client_OnUnaccounted(object? sender, OnUnaccountedForArgs e)
    {
        if (e.RawIRC.Contains("moderation"))
        {
            TwitchClient? twitchClient = sender as TwitchClient;
            Console.WriteLine($"{e.RawIRC} {e.Channel} {e.Location}");
            await Task.Delay(2000);
            twitchClient?.SendMessage(e.Channel, "moderation 1984 ");
            return;
        }
        Console.WriteLine($"[{e.Channel}] [{e.RawIRC}]");
    }

    private void Client_OnLog(object? sender, OnLogArgs e)
    {
        //Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
    }

    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine($"{e.BotUsername} Connected ");
        GlobalEmotes ??= API.Helix.Chat.GetGlobalEmotesAsync().Result.GlobalEmotes;
    }

    private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine($"Joined {e.Channel}");
    }

    #endregion EVENTS

    private static string FixNineEleven(string input)
    {
        string[] split = input.Split(' ');
        string result = "";
        foreach (string s in split)
        {
            string temp = "";
            foreach (char c in s)
            {
                if (c.Equals('9') || c.Equals('1'))
                {
                    temp += c;
                }
            }
            if (temp.Contains("911"))
            {
                result += s.Replace("9", "*") + " ";
            }
            else
            {
                result += s + " ";
            }
        }
        return result;
    }
}
