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

    public List<ChatMessage> GetList()
    {
        lock (locker)
        {
            ChatMessage[] result = new ChatMessage[cache.Count];
            cache.CopyTo(result);
            return result.ToList();
        }
    }

    public ChatMessage? Find(Predicate<ChatMessage> match)
    {
        lock (locker) { return cache.Find(match); }
    }

    public ChatMessage? Random()
    {
        lock (locker) { return cache.Count <= 0 ? null : cache[System.Random.Shared.Next(0, cache.Count)]; }
    }

    public void AddFakeMessage(string user, string content) => Add(new ChatMessage("faker", "871501999", user, user, "FFFFFF", System.Drawing.Color.White, new EmoteSet("", ""), content, UserType.Viewer, "vedal987", "", false, 0, "", false, false, false, false, false, false, false, Noisy.NotSet, "", "", [], null, 0, 0));
}
