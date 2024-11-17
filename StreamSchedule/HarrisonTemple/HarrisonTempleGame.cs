using System.Reflection;
using StreamSchedule.HarrisonTemple.Items;

namespace StreamSchedule.HarrisonTemple;

public static class HarrisonTempleGame
{
    private static List<Item> Items { get;}
    static HarrisonTempleGame()
    {
        Items = [];
        foreach (Type t in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(Item))).ToList())
        {
            Items.Add((Item)Activator.CreateInstance(t)!);
        }
    }
    
    public static Room GenerateRoom()
    {
        Item randomItem = Random.Shared.GetItems(Items.ToArray(), 1)[0];
        string[] decorations = Random.Shared.GetItems(Environment.Decorations, 2);
        string[] creatures = Random.Shared.GetItems(Environment.Creatures, 2);
        
        string content = $"[{decorations[0]}_{creatures[0]}_({randomItem})_{decorations[1]}_{creatures[1]}]";

        return new Room(content, randomItem.Reward, randomItem.Exp);
    }
}
