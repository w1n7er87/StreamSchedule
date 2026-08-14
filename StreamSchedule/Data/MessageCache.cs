using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;

namespace StreamSchedule.Data;

public class MessageCache
{
    private readonly int capacity;
    private readonly List<ChatMessage> cache;
    private readonly Lock locker = new Lock();

    private MessageCache()
    {
        capacity = 500;
        cache = new List<ChatMessage>(capacity);
    }

    public MessageCache(int capacity)
    {
        this.capacity = capacity;
        cache = new List<ChatMessage>(capacity);
    }

    public void Add(ChatMessage message)
    {
        lock (locker)
        {
            cache.Add(message);
            if (cache.Count > capacity) { cache.RemoveAt(0); }
        }
    }

    public List<ChatMessage> TakeLast(int count)
    {
        lock (locker)
        {
            int amount = Math.Max(Math.Min(count, cache.Count), 1);
            List<ChatMessage> result = cache[^amount ..];
            result.Reverse();
            return result;
        }
    }

    public List<ChatMessage> ToList()
    {
        lock (locker) { return cache.ToList(); }
    }

    public ChatMessage? Find(Predicate<ChatMessage> match)
    {
        lock (locker) { return cache.Find(match); }
    }

    public ChatMessage? Random()
    {
        lock (locker) { return cache.Count <= 0 ? null : cache[System.Random.Shared.Next(0, cache.Count)]; }
    }

    public void ReplaceMessage(string? id, string? username, string? content)
    {
        if (string.IsNullOrEmpty(id)) return;
        lock (locker)
        {
            for (int i = 0; i < cache.Count; i++)
            {
                if(!cache[i].Id.Equals(id)) continue;
                ChatMessage m = cache[i];
                cache[i] = new ChatMessage(m.BotUsername, m.UserId, username ?? m.Username, username ?? m.Username, m.ColorHex, m.Color, m.EmoteSet, content ?? m.Message, m.UserType, m.Channel, m.Id, m.IsSubscriber, m.SubscribedMonthCount, m.RoomId, m.IsTurbo, m.IsModerator, m.IsMe, m.IsBroadcaster, m.IsVip, m.IsPartner, m.IsStaff, m.Noisy, m.RawIrcMessage, m.EmoteReplacedMessage, m.BadgeInfo, m.CheerBadge, m.Bits, m.BitsInDollars);
            } 
        }
    }

    public void Remove(string? id)
    {
        if(string.IsNullOrEmpty(id)) return;
        lock (locker)
        {
            cache.RemoveAll(m => m.Id.Equals(id));
        }
    }
    
    public void AddFakeMessage(string user, string content) => Add(new ChatMessage("faker", "871501999", user, user, "FFFFFF", System.Drawing.Color.White, new EmoteSet("", ""), content, UserType.Viewer, "vedal987", "", false, 0, "", false, false, false, false, false, false, false, Noisy.NotSet, "", "", [], null, 0, 0));
}
