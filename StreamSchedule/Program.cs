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
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

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
        Body b = new Body();
        Console.ReadLine();
    }
}

internal class Body
{
    private TwitchClient _client;
    private TwitchAPI _api;
    private LiveStreamMonitorService _monitor;
    public static DatabaseContext dbContext = new DatabaseContext(new DbContextOptionsBuilder<DatabaseContext>().UseSqlite("Data Source=StreamSchedule.data").Options);

    private static readonly string _commandChars = "!$?";
    private bool _isOnline = false;
    
    private DateTime _lastMessageSent = DateTime.Now;

    public static List<Command?> currentCommands = [];
    public static int messagesIgnoreDelayMS = 350;

    private async Task ConfigLiveMonitorAsync()
    {
        _monitor.SetChannelsById(["85498365"]);

        _monitor.OnStreamOnline += OnLive;
        _monitor.OnStreamOffline += OnOffline;
        _monitor.OnChannelsSet += Monitor_OnChannelsSet;
        _monitor.OnServiceStarted += Monitor_OnServiceStarted;
        _monitor.Start(); //Keep at the end!

        await Task.Delay(-1);
    }

    public Body()
    {
        _api = new TwitchAPI();
        _api.Settings.ClientId = Credentials.clientID;
        _api.Settings.AccessToken = Credentials.oauth;
        _api.Helix.Settings.ClientId = Credentials.clientID;
        _api.Helix.Settings.AccessToken = Credentials.oauth;

        _monitor = new LiveStreamMonitorService(_api, 10);

        Task.Run(() => ConfigLiveMonitorAsync());

        dbContext.Database.EnsureCreated();

        ConnectionCredentials credentials = new ConnectionCredentials(Credentials.username, Credentials.oauth);
        ClientOptions clientOptions = new()
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };
        WebSocketClient customClient = new(clientOptions);
        _client = new TwitchClient(customClient);
        _client.Initialize(credentials, "vedal987");

        _client.OnLog += Client_OnLog;
        _client.OnJoinedChannel += Client_OnJoinedChannel;
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnWhisperReceived += Client_OnWhisperReceived;
        _client.OnConnected += Client_OnConnected;
        _client.Connect();

        foreach (var c in Commands.Commands.knownCommands)
        {
            currentCommands.Add((Command?)Activator.CreateInstance(c));
        }
    }

    private void OnLive(object? sender, OnStreamOnlineArgs args)
    {
        _isOnline = true;
        Console.WriteLine("went live");
    }
    
    private void OnOffline(object? sender, OnStreamOfflineArgs args)
    {
        _isOnline = false;
        Console.WriteLine("went offline");
    }
    private void Monitor_OnChannelsSet(object? sender, OnChannelsSetArgs e)
    {
        Console.WriteLine($"channels set{e.Channels[0]}");
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

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        if (_isOnline) return;

        User u = new()
        {
            Id = int.Parse(e.ChatMessage.UserId),
            Username = e.ChatMessage.Username,
            privileges = e.ChatMessage.UserType > TwitchLib.Client.Enums.UserType.Viewer ? Privileges.Mod : Privileges.None,
        };

        if (dbContext.Users.Find(u.Id) == null)
        {
            dbContext.Users.Add(u);
            dbContext.SaveChanges();
        }

        if (DateTime.Now - _lastMessageSent < TimeSpan.FromMilliseconds(messagesIgnoreDelayMS)) return;

        if (_commandChars.Contains(e.ChatMessage.Message[0]))
        {
            foreach (var c in currentCommands)
            {
                if (c != null && e.ChatMessage.Message[1..].StartsWith(c.Call))
                {
                    User? userSent = dbContext.Users.SingleOrDefault(u => u.Id == int.Parse(e.ChatMessage.UserId));
                    if (userSent != null && userSent.privileges >= c.MinPrivilege)
                    {
                        string response = c.Handle(new(e.ChatMessage, userSent.privileges));
                        Console.WriteLine(response);
                        _client.SendMessage(e.ChatMessage.Channel, response);
                        _lastMessageSent = DateTime.Now;
                    }
                    else { _client.SendMessage(e.ChatMessage.Channel, "✋ unauthorized action"); }
                }
            }
        }
    }

    private void Client_OnWhisperReceived(object? sender, OnWhisperReceivedArgs e)
    {
        User u = new()
        {
            Id = int.Parse(e.WhisperMessage.UserId),
            Username = e.WhisperMessage.Username,
            privileges = e.WhisperMessage.UserType > TwitchLib.Client.Enums.UserType.Viewer ? Privileges.Mod : Privileges.None ,
        };

        if (dbContext.Users.Find(u.Id) == null)
        {
            dbContext.Users.Add(u);
            dbContext.SaveChanges();
        }

        if (_commandChars.Contains(e.WhisperMessage.Message[0]))
        {
            foreach (var c in currentCommands)
            {
                if (c != null && e.WhisperMessage.Message[1..].StartsWith(c.Call))
                {
                    User? userSent = dbContext.Users.SingleOrDefault(u => u.Id == int.Parse(e.WhisperMessage.UserId));
                    if (userSent != null && userSent.privileges >= c.MinPrivilege)
                    {
                        string response = c.Handle(new(e.WhisperMessage, userSent.privileges));
                        Console.WriteLine(response);
                        _client.SendWhisper(e.WhisperMessage.Username, response);
                    }
                    else { _client.SendWhisper(e.WhisperMessage.Username, "✋ unauthorized action"); }
                }
            }
        }
    }
}
