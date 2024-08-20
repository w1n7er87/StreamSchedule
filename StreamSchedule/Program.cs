using Microsoft.EntityFrameworkCore.Internal;
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
        Body b = new Body(new DatabaseContextFactory().CreateDbContext(Array.Empty<string>()));
        Console.ReadLine();
    }
}

class Body
{
    private TwitchClient _client;
    private DatabaseContext _context;
    public Body(DatabaseContext context)
    {
        _context = context;
        _context.Database.EnsureCreated();
        
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
        //client.SendMessage(e.Channel, "miniUuh ");
    }

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        User u = new()
        {
            Id = int.Parse(e.ChatMessage.UserId),
            Username = e.ChatMessage.Username,
            privileges = Privileges.None
        };

        if(_context.Users.Find(u.Id) == null)
        {
            Console.WriteLine("new user");
            _context.Users.Add(u);
            _context.SaveChanges();
        }
        else { Console.WriteLine("known user"); }


        if (e.ChatMessage.Message.StartsWith("hi"))
        {
            _client.SendMessage(e.ChatMessage.Channel, e.ChatMessage.Message);
            //client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromSeconds(5));
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