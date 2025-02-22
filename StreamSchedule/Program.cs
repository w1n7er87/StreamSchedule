using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NeoSmart.Unicode;
using NLog;
using StreamSchedule.Commands;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using StreamSchedule.Extensions;
using StreamSchedule.GraphQL;
using StreamSchedule.Markov;
using System.Diagnostics;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Chat.Emotes;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace StreamSchedule;

public static class Program
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            logger.Fatal(e.ExceptionObject.ToString());
        };

        if (EF.IsDesignTime)
        {
            new HostBuilder().Build().Run();
            return;
        }

        try
        {
            int[] channelIDs = [85498365, 78135490, 871501999];

            DatabaseContext dbContext = new(new DbContextOptionsBuilder<DatabaseContext>()
                .UseSqlite("Data Source=StreamSchedule.data").Options);
            dbContext.Database.EnsureCreated();

            List<string> channelNames = [];

            foreach (int id in channelIDs)
            {
                User u = dbContext.Users.Find(id)!;
                channelNames.Add(u.Username!);
            }

            BotCore.Init(channelNames, dbContext, logger);
            Scheduling.Init();

            Markov.Markov.AddMessage(MarkovSampleText.Text);

            Console.ReadLine();
        }
        catch (Exception e)
        {
            logger.Error(e, e.ToString());
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}

internal static class BotCore
{
    public static DatabaseContext DBContext { get; private set; } = null!;
    public static TwitchAPI API { get; private set; } = null!;
    public static TwitchClient Client { get; private set; } = null!;
    public static Logger Nlog { get; private set; } = null!;
    public static GraphQLClient GQLClient { get; private set; } = null!;

    public static bool Silent { get; set; }

    private static LiveStreamMonitorService Monitor { get; set; } = null!;
    private static Dictionary<string, bool> ChannelLiveState { get; set; } = null!;

    private static DateTime _textCommandLastUsed = DateTime.MinValue;
    private static bool _sameMessage;

    public static readonly List<ChatMessage> MessageCache = [];
    private const int _cacheSize = 800;

    private static readonly List<Codepoint> _emojiSpecialCharacters = [Emoji.ZeroWidthJoiner, Emoji.ObjectReplacementCharacter, Emoji.Keycap, Emoji.VariationSelector];
    private const string _commandPrefixes = "!$?@#%^&`~><¡¿*-+_=;:'\"\\|/,.？！[]{}()";

    private static int _dbSaveCounter = 0;
    private const int _dbUpdateCountInterval = 10;
    public static int MessageLengthLimit = 348;

    private static async Task ConfigLiveMonitorAsync(List<string> channelNames)
    {
        Monitor.SetChannelsByName(channelNames);

        Monitor.OnStreamOnline += Monitor_OnLive;
        Monitor.OnStreamOffline += Monitor_OnOffline;
        Monitor.Start();

        await Task.Delay(-1);
    }

    public static void Init(List<string> channelNames, DatabaseContext dbContext, Logger logger)
    {
        DBContext = dbContext;
        Nlog = logger;

        API = new()
        {
            Settings =
            {
                ClientId = Credentials.clientID,
                AccessToken = Credentials.oauth
            }
        };

        Monitor = new(API, 5);

        Task.Run(() => ConfigLiveMonitorAsync(channelNames));

        Client = new();
        Client.Initialize(new(Credentials.username, Credentials.oauth), [.. channelNames]);
        Client.OnUnaccountedFor += Client_OnUnaccounted;
        Client.OnJoinedChannel += Client_OnJoinedChannel;
        Client.OnMessageReceived += Client_OnMessageReceived;
        Client.OnConnected += Client_OnConnected;
        Client.OnRateLimit += Client_OnRateLimit;
        Client.OnGiftedSubscription += Client_OnGifted;
        Client.Connect();

        ChannelLiveState = [];
        foreach (string channel in channelNames) ChannelLiveState[channel] = false;

        Commands.Commands.InitializeCommands(channelNames, DBContext);

        GQLClient = new GraphQLClient();
    }

    private static async void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        long start = Stopwatch.GetTimestamp();

        User userSent = User.SyncToDb(e.ChatMessage.UserId, e.ChatMessage.Username, e.ChatMessage.UserType, DBContext);

        if (e.ChatMessage.Channel.Equals("vedal987"))
        {
            _dbSaveCounter++;
            if (ChannelLiveState[e.ChatMessage.Channel])
                User.AddMessagesCounter(userSent, 1);
            else
                User.AddMessagesCounter(userSent, offline: 1);

            if (_dbSaveCounter >= _dbUpdateCountInterval)
            {
                await DBContext.SaveChangesAsync();
                _dbSaveCounter = 0;
            }
        }

        MessageCache.Add(e.ChatMessage);
        if (MessageCache.Count > _cacheSize) MessageCache.RemoveAt(0);

        string bypassSameMessage = _sameMessage ? " \U000e0000" : "";

        ReadOnlySpan<Codepoint> messageAsCodepoints = [.. e.ChatMessage.Message.Codepoints()];

        string? replyID = null;
        if (e.ChatMessage.ChatReply != null)
        {
            replyID = e.ChatMessage.ChatReply.ParentMsgId;
            try
            {
                messageAsCodepoints = messageAsCodepoints[(e.ChatMessage.Message.Split(" ")[0].Codepoints().Count() + 1)..];
            }
            catch (Exception ex)
            {
                Nlog.Error($"[{e.ChatMessage.ChatReply.ParentDisplayName}|{e.ChatMessage.ChatReply.ParentUserLogin}] {e.ChatMessage.Message} {ex}");
                return;
            }
        }

        if (!ContainsPrefix(messageAsCodepoints, out messageAsCodepoints))
        {
            if (userSent.MessagesOnline > 100 ||  userSent.MessagesOffline > 100) Markov.Markov.AddMessage(messageAsCodepoints.ToStringRepresentation().Replace("\U000e0000", ""));
            return;
        }

        if (messageAsCodepoints.Length < 2) return;

        string trimmedMessage = messageAsCodepoints.ToStringRepresentation();
        string requestedCommand = trimmedMessage.Split(' ')[0];

        List<TextCommand> textCommands = Commands.Commands.CurrentTextCommands;

        if (DateTime.Now >= _textCommandLastUsed + TimeSpan.FromSeconds(5) && !Silent && textCommands.Count > 0)
        {
            foreach (TextCommand command in textCommands)
            {
                if (!requestedCommand.Equals(command.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (command.Aliases is null) continue;
                    if (!command.Aliases.Any(x => x.Equals(requestedCommand, StringComparison.OrdinalIgnoreCase))) continue;
                }

                if (userSent.Privileges < command.Privileges) return;

                Nlog.Info($"({Stopwatch.GetElapsedTime(start):s\\.fffffff}) [{e.ChatMessage.Username}]:[{command.Name}]:[{command.Content}] ");
                SendLongMessage(e.ChatMessage.Channel, null, command.Content + bypassSameMessage);
                _sameMessage = !_sameMessage;
                _textCommandLastUsed = DateTime.Now;
                return;
            }
        }

        if (ChannelLiveState[e.ChatMessage.Channel] && userSent.Privileges < Privileges.Mod) return;

        foreach (Command c in Commands.Commands.CurrentCommands)
        {
            ReadOnlySpan<char> usedCall = c.Call;
            if (!requestedCommand.Equals(c.Call, StringComparison.OrdinalIgnoreCase))
            {
                CommandAlias? aliases = DBContext.CommandAliases.Find(c.Call.ToLower());
                if (aliases?.Aliases is null || aliases.Aliases.Count == 0) continue;
                if (!aliases.Aliases.Any(x => x.Equals(requestedCommand, StringComparison.OrdinalIgnoreCase))) continue;
                usedCall = requestedCommand;
            }

            if (c.LastUsedOnChannel[e.ChatMessage.Channel] + c.Cooldown > DateTime.Now) return;

            if (userSent.Privileges < c.MinPrivilege) return;

            trimmedMessage = trimmedMessage[usedCall.Length..].Replace("\U000e0000", "").TrimStart();
            CommandResult response = await c.Handle(new(userSent, trimmedMessage, e.ChatMessage.Id, replyID, e.ChatMessage.RoomId, e.ChatMessage.Channel));

            Nlog.Info($"{(Silent ? "*silent* " : "")}({Stopwatch.GetElapsedTime(start):s\\.fffffff}) [{e.ChatMessage.Username}]:[{c.Call}]:[{trimmedMessage}] - [{response}] ");

            if (string.IsNullOrEmpty(response.ToString()) || Silent) return;

            SendLongMessage(e.ChatMessage.Channel, response.reply ? e.ChatMessage.ChatReply?.ParentMsgId ?? e.ChatMessage.Id : null, response.ToString() + bypassSameMessage);

            _sameMessage = !_sameMessage;
            c.LastUsedOnChannel[e.ChatMessage.Channel] = DateTime.Now;
            return;
        }
    }

    #region EVENTS

    private static void Monitor_OnLive(object? sender, OnStreamOnlineArgs args)
    {
        ChannelLiveState[args.Channel] = true;
        Nlog.Info($"{args.Channel} went live");
    }

    private static void Monitor_OnOffline(object? sender, OnStreamOfflineArgs args)
    {
        ChannelLiveState[args.Channel] = false;
        Nlog.Info($"{args.Channel} went offline");
    }

    private static void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        Nlog.Info($"{e.BotUsername} Connected ");
    }

    private static void Client_OnGifted(object? sender, OnGiftedSubscriptionArgs e)
    {
        if (e.GiftedSubscription.MsgParamRecipientUserName.Equals(Client.TwitchUsername.ToLower()))
        {
            SendLongMessage(e.Channel, null, $"Thanks for the sub, {e.GiftedSubscription.Login} PogChamp");
        }
    }

    private static void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Nlog.Info($"Joined {e.Channel}");
    }

    private static void Client_OnRateLimit(object? sender, OnRateLimitArgs e)
    {
        Nlog.Info($"rate limited {e.Message}");
    }

    private static async void Client_OnUnaccounted(object? sender, OnUnaccountedForArgs e)
    {
        if (e.RawIRC.Contains("automod", StringComparison.InvariantCultureIgnoreCase) || e.RawIRC.Contains("moderation", StringComparison.InvariantCultureIgnoreCase))
        {
            TwitchClient? twitchClient = sender as TwitchClient;
            Nlog.Info($"{e.RawIRC} {e.Channel} {e.Location}");
            await Task.Delay(2000);
            twitchClient?.SendMessage(e.Channel, "moderation 1984 ");
            return;
        }
        Nlog.Info($"[{e.Channel}] [{e.RawIRC}]");
    }

    #endregion EVENTS

    public static async void SendLongMessage(string channel, string? replyID, string message)
    {
        string[] parts = message.Split(' ');
        string result = "";

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            if (result.Length + part.Length <= MessageLengthLimit)
            {
                result += part + ' ';
            }
            else
            {
                if (replyID is not null) Client.SendReply(channel, replyID, result);
                else Client.SendMessage(channel, result);
                result = part + ' ';
                await Task.Delay(1100);
            }
        }
        if (replyID is not null) Client.SendReply(channel, replyID, result);
        else Client.SendMessage(channel, result);
    }

    private static bool ContainsPrefix(ReadOnlySpan<Codepoint> input, out ReadOnlySpan<Codepoint> prefixTrimmedInput)
    {
        int count = 0;
        foreach (Codepoint codepoint in input)
        {
            if (Emoji.IsEmoji(codepoint.AsString()) ||
                Emoji.SkinTones.All.Any(x => x == codepoint) ||
                _emojiSpecialCharacters.Any(x => x == codepoint) ||
                _commandPrefixes.Any(x => x == codepoint)
               )
            {
                count++;
                continue;
            }

            if (codepoint.Equals(' ')) count++;
            break;
        }

        if (count == 0)
        {
            prefixTrimmedInput = input;
            return false;
        }

        prefixTrimmedInput = input[count..];
        return true;
    }
}
