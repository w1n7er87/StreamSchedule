using StreamSchedule.Data;

namespace StreamSchedule.Export.Conversions;

public static class Conversions
{
    public static string EmoteToHtml(Emote emote)
    {
        return $"""
               <div class="emote">
                   <p class="emoteTier">{emote.Cost}</p>
                   <img class="emoteImage" alt="{emote.Token}" src="{emote.ImageUrl}"/>
                   <p class="emoteText">{emote.Token}</p>
               </div>
               """;
    }
}
