using StreamSchedule.Data;

namespace StreamSchedule.WebExport.Conversions;

public static class Conversions
{
    public static string EmoteCostToHtmlRarityClass(EmoteCost cost)
    {
        return cost.Type switch
        {
            EmoteCostType.Unknown => "rarity0",
            EmoteCostType.Subscription => cost.Value switch
            {
                1 => "rarity1",
                2 => "rarity2",
                3 => "rarity3",
                _ => "rarity0"
            },
            EmoteCostType.Bits => cost.Value switch
            {
                < 100 => "rarity0",
                < 1000 => "rarity1",
                < 5000 => "rarity2",
                < 10000 => "rarity3",
                < 100000 => "rarity4",
                _ => "rarity5"
            },
            EmoteCostType.Follow => "rarity0",
            _ => "rarity0"
        };
    }
    
    public static string EmoteToHtml(Emote emote)
    {
        string rarity = EmoteCostToHtmlRarityClass(emote.Cost);
        return $"""
               <div class="emote">
                   <p class="emoteTier {rarity}">{emote.Cost}</p>
                   <img class="emoteImage {rarity}" alt="{emote.Token}" src="{emote.URL}"/>
                   <p class="emoteText">{emote.Token}</p>
               </div>
               """;
    }
}
