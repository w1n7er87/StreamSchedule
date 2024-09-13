using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using StreamSchedule.Commands;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace StreamSchedule;

public class Program
{
    private static void Main(string[] args)
    {
        if (EF.IsDesignTime)
        {
            new HostBuilder().Build().Run();
            return;
        }
        Body.main = new Body(["vedal987", "w1n7er", "streamschedule"]);
        Console.ReadLine();
    }
}

internal class Body
{
    public static DatabaseContext dbContext = new(new DbContextOptionsBuilder<DatabaseContext>().UseSqlite("Data Source=StreamSchedule.data").Options);
    public static Body main;
    private TwitchClient _client;
    public TwitchAPI api;
    private LiveStreamMonitorService _monitor;

    public static List<Command?> CurrentCommands { get; private set; } = [];

    private static readonly string _commandChars = "♿!$?@#%^&`~><¡¿*-+_=;:'\"\\|/,.🫃‽？！‼⁉❢♯";

    private Dictionary<string, bool> _channelLiveState;

    private bool _sameMessage = false;

    public List<ChatMessage> MessageCache { get; private set; } = [];
    private int _cacheSize = 300;

    private async Task ConfigLiveMonitorAsync(string[] channelNames)
    {
        _monitor.SetChannelsByName([.. channelNames]);

        _monitor.OnStreamOnline += OnLive;
        _monitor.OnStreamOffline += OnOffline;
        _monitor.OnChannelsSet += Monitor_OnChannelsSet;
        _monitor.OnServiceStarted += Monitor_OnServiceStarted;
        _monitor.Start(); //Keep at the end!

        await Task.Delay(-1);
    }

    public Body(string[] channelNames)
    {
        api = new TwitchAPI();
        api.Settings.ClientId = Credentials.clientID;
        api.Settings.AccessToken = Credentials.oauth;
        api.Helix.Settings.ClientId = Credentials.clientID;
        api.Helix.Settings.AccessToken = Credentials.oauth;
        _monitor = new LiveStreamMonitorService(api, 5);

        Task.Run(() => ConfigLiveMonitorAsync(channelNames));

        dbContext.Database.EnsureCreated();

        ConnectionCredentials credentials = new ConnectionCredentials(Credentials.username, Credentials.oauth);

        _client = new TwitchClient();
        _client.Initialize(credentials, [.. channelNames]);
        _client.OnLog += Client_OnLog;
        _client.OnJoinedChannel += Client_OnJoinedChannel;
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnConnected += Client_OnConnected;
        _client.Connect();

        _channelLiveState = [];
        foreach (string channel in channelNames)
        {
            _channelLiveState[channel] = new();
        }

        if (CurrentCommands.Count == 0)
        {
            foreach (var c in Commands.Commands.knownCommands)
            {
                CurrentCommands.Add((Command?)Activator.CreateInstance(c));
                foreach (string channel in channelNames)
                {
                    CurrentCommands[^1]?.LastUsedOnChannel.Add(channel, DateTime.Now);
                }
            }
        }
    }

    private void OnLive(object? sender, OnStreamOnlineArgs args)
    {
        _channelLiveState[args.Channel] = true;
        Console.WriteLine($"{args.Channel} went live");
    }

    private void OnOffline(object? sender, OnStreamOfflineArgs args)
    {
        _channelLiveState[args.Channel] = false;
        Console.WriteLine($"{args.Channel} went offline");
    }

    private void Monitor_OnChannelsSet(object? sender, OnChannelsSetArgs e)
    {
        string r = "";
        foreach (var c in e.Channels) { r += c + ", "; }
        Console.WriteLine($"channels set{r}");
    }

    private void Monitor_OnServiceStarted(object? sender, OnServiceStartedArgs e)
    {
        Console.WriteLine("monitoring service stated");
    }

    private void Client_OnLog(object? sender, OnLogArgs e)
    {
        Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
    }

    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine($"Connected {e.AutoJoinChannel}");
    }

    private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine($"Joined {e.Channel.ToString()}");
    }

    private async void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        if (_channelLiveState[e.ChatMessage.Channel]) return;

        User u = new()
        {
            Id = int.Parse(e.ChatMessage.UserId),
            Username = e.ChatMessage.Username,
            privileges = e.ChatMessage.UserType > TwitchLib.Client.Enums.UserType.Viewer ? Privileges.Mod : Privileges.None,
        };

        User userSent = Utils.SyncToDb(u, ref dbContext);

        string bypassSameMessage = _sameMessage ? " \U000e0000" : "";

        if (e.ChatMessage.Message.Length < 3) return;

        MessageCache.Add(e.ChatMessage);
        if (MessageCache.Count > _cacheSize)
        {
            MessageCache.RemoveAt(0);
        }

        string? replyID = null;
        string trimmedMessage = e.ChatMessage.Message;
        if (e.ChatMessage.ChatReply != null)
        {
            replyID = e.ChatMessage.ChatReply.ParentMsgId;
            trimmedMessage = trimmedMessage[(e.ChatMessage.ChatReply.ParentDisplayName.Length + 2)..];
        }

        if (trimmedMessage.Length <= 1) return;

        if (_commandChars.Contains(e.ChatMessage.Message[0]))
        {
            int idx = trimmedMessage[1].Equals(' ') ? 2 : 1;

            foreach (var c in CurrentCommands)
            {
                if (c != null && trimmedMessage[idx..].Split(' ', 3)[0].Equals(c.Call, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (c.LastUsedOnChannel[e.ChatMessage.Channel] + c.Cooldown > DateTime.Now) { return; }

                    if (userSent.privileges >= c.MinPrivilege)
                    {
                        trimmedMessage = trimmedMessage[(idx + c.Call.Length)..];
                        string response = await c.Handle(new(e.ChatMessage, trimmedMessage, replyID, userSent.privileges));
                        _client.SendReply(e.ChatMessage.Channel, e.ChatMessage.ChatReply?.ParentMsgId ?? e.ChatMessage.Id, response + bypassSameMessage);
                    }
                    else
                    {
                        _client.SendMessage(e.ChatMessage.Channel, "✋ unauthorized action" + bypassSameMessage);
                    }

                    _sameMessage = !_sameMessage;
                    c.LastUsedOnChannel[e.ChatMessage.Channel] = DateTime.Now;

                    break;
                }
            }
        }
    }
}
