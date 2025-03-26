using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NeoSmart.Unicode;
using NLog;
using StreamSchedule.Commands;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using StreamSchedule.EmoteMonitors;
using StreamSchedule.Extensions;
using StreamSchedule.GraphQL;
using System.Diagnostics;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using OutgoingMessage = StreamSchedule.Data.OutgoingMessage;

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
            Monitoring.Init();

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

    public static readonly List<ChatMessage> MessageCache = [];
    private const int _cacheSize = 800;
    public static int MessageLengthLimit = 260;

    private static long _lastSave;

    public static List<PermittedTerm> PermittedTerms { get; set; } = [];

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

        ChannelLiveState = [];
        foreach (string channel in channelNames) 
        {
            OutQueuePerChannel.Add(channel, []);
            ChannelLiveState.Add(channel, false);
            Task.Run(() => OutPump(channel));
        }

        Commands.Commands.InitializeCommands(channelNames, DBContext);

        GQLClient = new GraphQLClient();

        Client = new();
        Client.Initialize(new(Credentials.username, Credentials.oauth), [.. channelNames]);
        Client.OnUnaccountedFor += Client_OnUnaccounted;
        Client.OnJoinedChannel += Client_OnJoinedChannel;
        Client.OnMessageReceived += Client_OnMessageReceived;
        Client.OnConnected += Client_OnConnected;
        Client.OnRateLimit += Client_OnRateLimit;
        Client.OnGiftedSubscription += Client_OnGifted;
        Client.Connect();
    }

    private static async void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        long start = Stopwatch.GetTimestamp();

        if(Stopwatch.GetElapsedTime(_lastSave).TotalSeconds >= 10)
        {
            _lastSave = start;
            await DBContext.SaveChangesAsync();
        }

        User userSent = User.SyncToDb(e.ChatMessage.UserId, e.ChatMessage.Username, e.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator, e.ChatMessage.IsVip, DBContext);
        
        if (e.ChatMessage.Channel.Equals("vedal987"))
        {
            if (ChannelLiveState[e.ChatMessage.Channel])
                User.AddMessagesCounter(userSent, online: 1);
            else
                User.AddMessagesCounter(userSent, offline: 1);
        }

        MessageCache.Add(e.ChatMessage);
        if (MessageCache.Count > _cacheSize) MessageCache.RemoveAt(0);

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

        if (!Utils.ContainsPrefix(messageAsCodepoints, out messageAsCodepoints)) return;

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

                Nlog.Info($"({Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms) [{e.ChatMessage.Username}]:[{command.Name}]:[{command.Content}] ");
                OutQueuePerChannel[e.ChatMessage.Channel].Enqueue(new CommandResult(command.Content, reply: false));
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

            if (c.LastUsedOnChannel[e.ChatMessage.Channel] + c.Cooldown > DateTime.Now && userSent.Privileges < Privileges.Mod) return;

            if (userSent.Privileges < c.MinPrivilege) return;
            trimmedMessage = trimmedMessage[usedCall.Length..].Replace("\U000e0000", "").Trim();
            
            Nlog.Info($"{(Silent ? "*silent* " : "")}({Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms) [{e.ChatMessage.Username}]:[{c.Call}]:[{trimmedMessage}]");
            
            CommandResult response = await c.Handle(new(userSent, trimmedMessage, e.ChatMessage.Id, replyID, e.ChatMessage.RoomId, e.ChatMessage.Channel));

            if (string.IsNullOrEmpty(response.ToString()) || Silent) return;

            Nlog.Info($"({Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms) [{response}]");
            
            OutQueuePerChannel[e.ChatMessage.Channel].Enqueue(new OutgoingMessage(response, e.ChatMessage.ChatReply?.ParentMsgId ?? e.ChatMessage.Id));

            if (userSent.Privileges < Privileges.Mod) c.LastUsedOnChannel[e.ChatMessage.Channel] = DateTime.Now;
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
            OutQueuePerChannel[e.Channel].Enqueue(new CommandResult($"Thanks for the sub, {e.GiftedSubscription.Login} PogChamp", reply: false));
    }

    private static void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Nlog.Info($"Joined {e.Channel}");
    }

    private static void Client_OnRateLimit(object? sender, OnRateLimitArgs e)
    {
        Nlog.Info($"rate limited {e.Message}");
    }

    private static void Client_OnUnaccounted(object? sender, OnUnaccountedForArgs e)
    {
        if (e.RawIRC.Contains("automod", StringComparison.InvariantCultureIgnoreCase) || e.RawIRC.Contains("moderation", StringComparison.InvariantCultureIgnoreCase))
        {
            Nlog.Info($"{e.RawIRC} {e.Channel} {e.Location}");
            OutQueuePerChannel[e.Channel].Enqueue(new CommandResult("moderation 1984 ", reply: false));
            return;
        }
        Nlog.Info($"[{e.Channel}] [{e.RawIRC}]");
    }

    #endregion EVENTS

    public static Dictionary<string, Queue<OutgoingMessage>> OutQueuePerChannel { get; } = [];

    private static async Task OutPump(string channel)
    {
        bool sameMessageFlip = false;
        string sameMessageBypass = sameMessageFlip ? " \U000e0000" : "";
        Queue <OutgoingMessage> q = OutQueuePerChannel[channel];

        while (true)
        {
            if (q.Count <= 0) continue;
            OutgoingMessage response = q.Peek();
            _ = await SendLongMessage(channel, response.ReplyID, response.Result.ToString() + sameMessageBypass, response.Result.requiresFilter);
            sameMessageFlip = !sameMessageFlip;
            await Task.Delay(1100);
            q.Dequeue();
        }
    }

    private static async Task<bool> SendLongMessage(string channel, string? replyID, string message, bool requiresFilter = false)
    {
        string[] parts = requiresFilter
            ? Utils.Filter(message).Split(' ', StringSplitOptions.TrimEntries)
            : message.Split(' ', StringSplitOptions.TrimEntries);
        
        string accumulatedBelowLimit = "";

        foreach (string part in parts)
        {
            if (accumulatedBelowLimit.Length + part.Length + 1 <= MessageLengthLimit)
            {
                accumulatedBelowLimit += part + ' ';
                continue;
            }

            if (part.Length > MessageLengthLimit)
            {
                if (!string.IsNullOrWhiteSpace(accumulatedBelowLimit))
                {
                    SendShortMessage(accumulatedBelowLimit);
                    accumulatedBelowLimit = "";
                    await Task.Delay(1100);
                }
                SendShortMessage(part);
                await Task.Delay(1100);
                continue;
            }

            SendShortMessage(accumulatedBelowLimit);
            accumulatedBelowLimit = part + ' ';
            await Task.Delay(1100);
        }

        SendShortMessage(accumulatedBelowLimit);

        return true;

        void SendShortMessage(string msg)
        {
            if (replyID is not null) Client.SendReply(channel, replyID, msg);
            else Client.SendMessage(channel, msg);
        }
    }
}
