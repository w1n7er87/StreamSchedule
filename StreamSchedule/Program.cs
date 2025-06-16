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
            
            List<User> JoinedUsers = [];
            JoinedUsers.AddRange(channelIDs.Select(id => dbContext.Users.Find(id)!));

            BotCore.Init(JoinedUsers, dbContext, logger);
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
    public static TwitchClient ChatClient { get; private set; } = null!;
    public static Logger Nlog { get; private set; } = null!;
    public static GraphQLClient GQLClient { get; private set; } = null!;

    public static bool Silent { get; set; }

    private static LiveStreamMonitorService Monitor { get; set; } = null!;
    private static Dictionary<string, bool> ChannelLiveState { get; set; } = null!;

    private static DateTime _textCommandLastUsed = DateTime.MinValue;

    public static readonly List<ChatMessage> MessageCache = [];
    private const int _cacheSize = 800;
    public static int MessageLengthLimit = 270;

    private static long _lastSave;

    public const string BotID = "871501999";
    
    public static Dictionary<string, Queue<OutgoingMessage>> OutQueuePerChannel { get; } = [];

    private static async Task ConfigLiveMonitorAsync(List<string> channelNames)
    {
        Monitor.SetChannelsByName(channelNames);

        Monitor.OnStreamOnline += MonitorOnLive;
        Monitor.OnStreamOffline += MonitorOnOffline;
        Monitor.Start();

        await Task.Delay(-1);
    }

    public static void Init(List<User> joinedUsers, DatabaseContext dbContext, Logger logger)
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

        Monitor = new(API, 1);

        Task.Run(() => ConfigLiveMonitorAsync(joinedUsers.Select(u => u.Username).ToList()!));

        ChannelLiveState = [];
        foreach (User user in joinedUsers) 
        {
            OutQueuePerChannel.Add(user.Username!, []);
            ChannelLiveState.Add(user.Username!, false);
            Task.Run(() => OutPump(user));
        }

        Commands.Commands.InitializeCommands([.. joinedUsers.Select(u => u.Username!)], DBContext);

        GQLClient = new GraphQLClient();

        ChatClient = new TwitchClient();
        ChatClient.Initialize(new(Credentials.username, Credentials.oauth), [.. joinedUsers.Select(u => u.Username!)]);
        ChatClient.OnUnaccountedFor += ChatClientOnUnaccounted;
        ChatClient.OnJoinedChannel += ChatClientOnJoinedChannel;
        ChatClient.OnMessageReceived += ChatClientOnMessageReceived;
        ChatClient.OnConnected += ChatClientOnConnected;
        ChatClient.OnRateLimit += ChatClientOnRateLimit;
        ChatClient.OnGiftedSubscription += ChatClientOnGifted;
        ChatClient.Connect();
    }

    private static async void ChatClientOnMessageReceived(object? sender, OnMessageReceivedArgs e)
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

        if (ChannelLiveState[e.ChatMessage.Channel] && userSent.Privileges < Privileges.Mod) return;
        
        ReadOnlySpan<Codepoint> messageAsCodepoints = [.. e.ChatMessage.Message.Codepoints()];

        string? replyID = null;
        if (e.ChatMessage.ChatReply != null)
        {
            replyID = e.ChatMessage.ChatReply.ParentMsgId;
            try
            {
                // stripping leading mandatory @username in reply messages 
                // this sometimes throws array index for some reason, still can't figure out why 
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

        if (DateTime.Now >= _textCommandLastUsed + TimeSpan.FromSeconds((int)Cooldowns.Long) && !Silent && textCommands.Count > 0)
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

    private static void MonitorOnLive(object? sender, OnStreamOnlineArgs args)
    {
        ChannelLiveState[args.Channel] = true;
        Nlog.Info($"{args.Channel} went live");
    }

    private static void MonitorOnOffline(object? sender, OnStreamOfflineArgs args)
    {
        ChannelLiveState[args.Channel] = false;
        Nlog.Info($"{args.Channel} went offline");
    }

    private static void ChatClientOnConnected(object? sender, OnConnectedArgs e)
    {
        Nlog.Info($"{e.BotUsername} Connected ");
    }

    private static void ChatClientOnGifted(object? sender, OnGiftedSubscriptionArgs e)
    {
        if (e.GiftedSubscription.MsgParamRecipientUserName.Equals(ChatClient.TwitchUsername.ToLower()))
            OutQueuePerChannel[e.Channel].Enqueue(new CommandResult($"Thanks for the sub, {e.GiftedSubscription.Login} PogChamp", reply: false));
    }

    private static void ChatClientOnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Nlog.Info($"Joined {e.Channel}");
    }

    private static void ChatClientOnRateLimit(object? sender, OnRateLimitArgs e)
    {
        Nlog.Info($"rate limited {e.Message}");
    }

    private static void ChatClientOnUnaccounted(object? sender, OnUnaccountedForArgs e)
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


    private static async Task OutPump(User channel)
    {
        bool sameMessageFlip = false;
        string sameMessageBypass = sameMessageFlip ? " \U000e0000" : "";

        while (true)
        {
            if (OutQueuePerChannel[channel.Username!].Count <= 0) { await Task.Delay(50); continue; }

            OutgoingMessage response = OutQueuePerChannel[channel.Username!].Peek();
            _ = await SendLongMessage(channel, response.ReplyID, response.Result.ToString() + sameMessageBypass, response.Result.requiresFilter);
            sameMessageFlip = !sameMessageFlip;
            await Task.Delay(1100);
            OutQueuePerChannel[channel.Username!].Dequeue();
        }
    }

    private static async Task<bool> SendLongMessage(User channel, string? replyID, string message, bool requiresFilter = false)
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
            if (replyID is not null) ChatClient.SendReply(channel.Username, replyID, msg);
            else ChatClient.SendMessage(channel.Username, msg);
        }
    }
}
