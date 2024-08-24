using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Hosting;
using StreamSchedule.Commands;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
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

    //public static IHostBuilder CreateHostBuilder(string[] args)
    //{
    //    Console.WriteLine("Doing Entity Framework migrations stuff, not starting full application");
    //    return Host.CreateDefaultBuilder();
    //}
}

class Body
{
    private TwitchClient _client;
    public static DatabaseContext dbContext = new DatabaseContext(new DbContextOptionsBuilder<DatabaseContext>().UseSqlite("Data Source=StreamSchedule.data").Options);
    private static readonly string commandChars = "!$?";
    public Body()
    {
        dbContext.Database.EnsureCreated();
        
        ConnectionCredentials credentials = new ConnectionCredentials(Credentials.username, Credentials.oauth);
        ClientOptions clientOptions = new()
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };
        WebSocketClient customClient = new(clientOptions);
        _client = new TwitchClient(customClient);
        _client.Initialize(credentials, "w1n7er");

        _client.OnLog += Client_OnLog;
        _client.OnJoinedChannel += Client_OnJoinedChannel;
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnWhisperReceived += Client_OnWhisperReceived;
        _client.OnNewSubscriber += Client_OnNewSubscriber;
        _client.OnConnected += Client_OnConnected;
        _client.Connect();
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
        User u = new()
        {
            Id = int.Parse(e.ChatMessage.UserId),
            Username = e.ChatMessage.Username,
            privileges = e.ChatMessage.UserType > TwitchLib.Client.Enums.UserType.Viewer ? Privileges.None : Privileges.Mod,
        };

        if(dbContext.Users.Find(u.Id) == null)
        {
            dbContext.Users.Add(u);
            dbContext.SaveChanges();
        }

        if (commandChars.Contains(e.ChatMessage.Message[0])) 
        {
            foreach (var c in Commands.Commands.knownCommands)
            {
                Command? i = (Command?) Activator.CreateInstance(c);

                if (i != null && e.ChatMessage.Message[1..].StartsWith(i.Call))
                {
                    User? userSent = dbContext.Users.SingleOrDefault(u => u.Id == int.Parse(e.ChatMessage.UserId));
                    if (userSent != null && userSent.privileges >= i.MinPrivilege)
                    {
                        Console.WriteLine(i.Handle(e.ChatMessage));
                        _client.SendMessage(e.ChatMessage.Channel, i.Handle(e.ChatMessage));
                    }
                    else { _client.SendMessage(e.ChatMessage.Channel, "✋ unauthorized action"); }
                }
            }
        }
    }
    
    private void Client_OnWhisperReceived(object? sender, OnWhisperReceivedArgs e)
    {
        if (e.WhisperMessage.Username == "my_friend")
            _client.SendWhisper(e.WhisperMessage.Username, "Hey! Whispers are so cool!!");
    }

    private void Client_OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {
    //    if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
    //        client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
    //    else
    //        client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
    //
    }
}