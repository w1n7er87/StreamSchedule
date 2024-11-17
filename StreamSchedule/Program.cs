using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NeoSmart.Unicode;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using System.Diagnostics;
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

        string[] channelNames = ["vedal987", "w1n7er", "streamschedule"];

        DatabaseContext dbContext = new(new DbContextOptionsBuilder<DatabaseContext>().UseSqlite("Data Source=StreamSchedule.data").Options);

        List<User> channels = [];

        foreach (var name in channelNames)
        {
            User? u = dbContext.Users.First(x => x.Username!.Equals(name));
            if (u is not  null) { channels.Add(u); }
        }

        BotCore.Init(channelNames, dbContext);
        Scheduling.Init([dbContext.Users.First(x => x.Username!.Equals("vedal987"))]);
        Console.ReadLine();
    }
}

internal static class BotCore
{
    public static DatabaseContext DBContext { get; private set; }
    public static TwitchAPI API { get; private set; }
    public static TwitchClient Client { get; private set; }
    
    public static bool Silent { get; set; }
    
    private static LiveStreamMonitorService Monitor { get; set; }
    private static Dictionary<string, bool> ChannelLiveState { get; set; }

    private static DateTime _textCommandLastUsed = DateTime.MinValue;
    private static bool _sameMessage;

    public static readonly List<ChatMessage> MessageCache = [];
    private const int _cacheSize = 800;

    private static readonly List<Codepoint> _emojiSpecialCharacters = [Emoji.ZeroWidthJoiner, Emoji.ObjectReplacementCharacter, Emoji.Keycap, Emoji.VariationSelector];
    private const string _commandPrefixes = "!$?@#%^&`~><¡¿*-+_=;:'\"\\|/,.？！[]{}()";

    private static int _dbSaveCounter = 0;
    private const int _dbUpdateCountInterval = 10;

    public static GlobalEmote[]? GlobalEmotes { get; set; }

    private static async Task ConfigLiveMonitorAsync(string[] channelNames)
    {
        Monitor.SetChannelsByName([.. channelNames]);

        Monitor.OnStreamOnline += Monitor_OnLive;
        Monitor.OnStreamOffline += Monitor_OnOffline;
        Monitor.OnChannelsSet += Monitor_OnChannelsSet;
        Monitor.OnServiceStarted += Monitor_OnServiceStarted;
        Monitor.Start();

        await Task.Delay(-1);
    }

    public static void Init(string[] channelNames, DatabaseContext dbContext)
    {
        DBContext = dbContext;

        API = new TwitchAPI
        {
            Settings =
            {
                ClientId = Credentials.clientID,
                AccessToken = Credentials.oauth
            }
        };

        Monitor = new LiveStreamMonitorService(API, 5);

        Task.Run(() => ConfigLiveMonitorAsync(channelNames));

        DBContext.Database.EnsureCreated();

        Client = new TwitchClient();
        Client.Initialize(new(Credentials.username, Credentials.oauth), [.. channelNames]);
        Client.OnUnaccountedFor += Client_OnUnaccounted;
        Client.OnJoinedChannel += Client_OnJoinedChannel;
        Client.OnMessageReceived += Client_OnMessageReceived;
        Client.OnConnected += Client_OnConnected;
        Client.Connect();

        ChannelLiveState = [];
        foreach (string channel in channelNames)
        {
            ChannelLiveState[channel] = new();
        }

        Commands.Commands.InitializeCommands(channelNames, DBContext);
    }

    private static async void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        long start = Stopwatch.GetTimestamp();

        User userSent = User.SyncToDb(e.ChatMessage.UserId, e.ChatMessage.Username, e.ChatMessage.UserType, DBContext);

        if (e.ChatMessage.Channel.Equals("vedal987"))
        {
            _dbSaveCounter++;
            if (ChannelLiveState[e.ChatMessage.Channel])
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

        MessageCache.Add(e.ChatMessage);
        if (MessageCache.Count > _cacheSize) { MessageCache.RemoveAt(0); }

        string bypassSameMessage = _sameMessage ? " \U000e0000" : "";
        string? replyID = null;
        string trimmedMessage = e.ChatMessage.Message;
        if (e.ChatMessage.ChatReply != null)
        {
            replyID = e.ChatMessage.ChatReply.ParentMsgId;
            trimmedMessage = trimmedMessage[(e.ChatMessage.ChatReply.ParentUserLogin.Length + 2)..];
        }

        if (!ContainsPrefix(trimmedMessage, out trimmedMessage)) { return; }

        if (trimmedMessage.Length < 2) return;

        string requestedCommand = trimmedMessage.Split(' ')[0];

        List<TextCommand> textCommands = [.. DBContext.TextCommands];

        if (DateTime.Now >= _textCommandLastUsed + TimeSpan.FromSeconds(5) && !Silent && textCommands.Count > 0)
        {
            foreach (TextCommand command in textCommands)
            {
                if (!command.Name.Equals(requestedCommand, StringComparison.OrdinalIgnoreCase))
                {
                    if (command.Aliases is null) continue;
                    if (!command.Aliases.Any(x => x.Equals(requestedCommand, StringComparison.OrdinalIgnoreCase))) continue;
                }

                if (userSent.Privileges < command.Privileges) return;

                Console.WriteLine($"{TimeOnly.FromDateTime(DateTime.Now)} ({Stopwatch.GetElapsedTime(start):s\\.fffffff}) [{e.ChatMessage.Username}]:[{command.Name}]:[{command.Content}] ");
                Client.SendMessage(e.ChatMessage.Channel, command.Content + bypassSameMessage);
                _sameMessage = !_sameMessage;
                _textCommandLastUsed = DateTime.Now;
                return;
            }
        }

        if (ChannelLiveState[e.ChatMessage.Channel] && userSent.Privileges < Privileges.Mod) return;

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

            CommandResult response = await c.Handle(new(userSent, trimmedMessage, replyID, e.ChatMessage.RoomId));

            Console.WriteLine($"{TimeOnly.FromDateTime(DateTime.Now)} {(Silent? "*silent* " : "")}({Stopwatch.GetElapsedTime(start):s\\.fffffff}) [{e.ChatMessage.Username}]:[{c.Call}]:[{trimmedMessage}] - [{response}] ");

            if (string.IsNullOrEmpty(response.ToString())) return;
            
            if (Silent) return;
            
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

    private static void Monitor_OnChannelsSet(object? sender, OnChannelsSetArgs e)
    {
        string r = "";
        foreach (var c in e.Channels) { r += c + ", "; }
        Console.WriteLine($"channels set {r}");
    }

    private static void Monitor_OnServiceStarted(object? sender, OnServiceStartedArgs e)
    {
        Console.WriteLine("monitoring service stated");
    }

    private static void Monitor_OnLive(object? sender, OnStreamOnlineArgs args)
    {
        ChannelLiveState[args.Channel] = true;
        Console.WriteLine($"{args.Channel} went live");
    }

    private static void Monitor_OnOffline(object? sender, OnStreamOfflineArgs args)
    {
        ChannelLiveState[args.Channel] = false;
        Console.WriteLine($"{args.Channel} went offline");
    }

    private static async void Client_OnUnaccounted(object? sender, OnUnaccountedForArgs e)
    {
        if (e.RawIRC.Contains("automod", StringComparison.InvariantCultureIgnoreCase) || e.RawIRC.Contains("moderation", StringComparison.InvariantCultureIgnoreCase))
        {
            TwitchClient? twitchClient = sender as TwitchClient;
            Console.WriteLine($"{e.RawIRC} {e.Channel} {e.Location}");
            await Task.Delay(2000);
            twitchClient?.SendMessage(e.Channel, "moderation 1984 ");
            return;
        }
        Console.WriteLine($"[{e.Channel}] [{e.RawIRC}]");
    }

    private static void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine($"{e.BotUsername} Connected ");
        GlobalEmotes ??= API.Helix.Chat.GetGlobalEmotesAsync().Result.GlobalEmotes;
    }

    private static void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
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

    private static bool ContainsPrefix(string input, out string prefixTrimmedInput)
    {
        prefixTrimmedInput = "";
        List<Codepoint> msgAsCodepoints = input.Codepoints().ToList();

        List<Codepoint> firstLetters = msgAsCodepoints.TakeWhile(x =>
            Emoji.IsEmoji(x.AsString()) ||
            _emojiSpecialCharacters.Any(y => y == x) ||
            Emoji.SkinTones.All.Any(y => y == x) ||
            _commandPrefixes.Any(y => y == x)).ToList();

        if (firstLetters.Count == 0) return false;

        msgAsCodepoints = msgAsCodepoints.Skip(firstLetters.Count).ToList();

        foreach (var l in msgAsCodepoints)
        {
            prefixTrimmedInput += l.AsString();
        }

        prefixTrimmedInput = prefixTrimmedInput.TrimStart();
        return true;
    }
}
