using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Markov2;

public static partial class BuildFromFiles
{
    private static readonly string path = AppContext.BaseDirectory + "chat";
    private static List<User> users = [];
    
    public static void Build()
    {
        DatabaseContext dbContext = new(new DbContextOptionsBuilder<DatabaseContext>().UseSqlite("Data Source=StreamSchedule.data").Options);
        string[] files = Directory.GetFiles(path);
        if (files.Length == 0) return;
        
        users = dbContext.Users.AsNoTracking().ToList();
        
        List<string> days = [];
        foreach (string file in files)
        {
            using StreamReader sr = new(file);
            days.Add(sr.ReadToEnd());
        }
        
        List<string> filtered = days.SelectMany(d => d.Split("\n", StringSplitOptions.RemoveEmptyEntries))
            .Where(m => !(m.Contains("has been timed out")
                          || m.Contains("subscribed with Prime") 
                          || m.Contains("gifted a Tier") 
                          || m.Contains("They've gifted") 
                          || m.Contains("subscribed at Tier") 
                          || m.Contains("is paying forward the") 
                          || m.Contains("consecutive streams and sparked") 
                          || m.Contains("Chat has been cleared")
                          || m.Contains("raiders from")
                          || m.Contains("'s community!")
                          || m.Contains("has been banned")
                          || m.Contains("converted from a Prime sub")
                          || m.Contains("is continuing the Gift"))).ToList();

        BotCore.Nlog.Info($"formatting total {days.Count} {days.Sum(d => d.Split("\n", StringSplitOptions.RemoveEmptyEntries).Length)} days {filtered.Count:N0} filtered messages");
        
        List<string> messages = filtered.Chunk(2000)
            .AsParallel()
            .Select(b => b.Select(Format)
                .Where(um => !(um.user.Privileges == Privileges.Banned || um.user.MessagesOffline < 50 || um.user.MessagesOnline < 50 || um.user.Username!.Equals("streamschedule") || um.user.Username!.Equals("fossabot")))
                .Select(um => um.message))
            .SelectMany(m => m)
            .ToList();
        
        BotCore.Nlog.Info($"{messages.Count:N0} messages formatted");

        foreach (string t in messages) { Markov.TokenizationQueue.Enqueue(t); }
        BotCore.Nlog.Info($"tokenization queue length: {Markov.TokenizationQueue.Count}");
        Markov.Save();
    }

    private static (User user, string message) Format(string message)
    {
        string m = Questions().Replace(Exclamations().Replace(Spaces().Replace(message[32 ..].Replace("󠀀", "").Replace("󠀀", "").Replace("͏", "").Replace("\r", ""), " "), "!!!"), "???");
        string u = string.Join(null, m.TakeWhile(c => c != ':'));
        User user = GetUser(u);
        return (user, m[(u.Length + 2) ..]);
    }

    private static User GetUser(string username)
    {

        if (string.IsNullOrEmpty(username)) return new() { Username = "blank", Privileges = Privileges.Banned, MessagesOffline = 0, MessagesOnline = 0 };

        User? u = users.FirstOrDefault(x => x.Username == username);

        if (u is not null) return u;

        User? byOldName = users.Where(x => x.PreviousUsernames != null).FirstOrDefault(x => x.PreviousUsernames!.Contains(username));
        
        if (byOldName is not null) return byOldName;
        return new() { Username = "blank", Privileges = Privileges.Banned, MessagesOffline = 0, MessagesOnline = 0 };
    }
    
    [GeneratedRegex(@"\?{4,}")] private static partial Regex Questions();
    [GeneratedRegex(@"!{4,}")] private static partial Regex Exclamations();
    [GeneratedRegex(@"\s+")] private static partial Regex Spaces();

}
