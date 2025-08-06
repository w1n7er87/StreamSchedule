using StreamSchedule.Data;

namespace StreamSchedule.Export.Conversions;

public static class Conversions
{
    public static string EmoteToHtml(Emote emote)
    {
        return $"""
               <div class="emote">
                   <p class="emoteTier">{emote.Cost}</p>
                   <img class="emoteImage" alt="{emote.Token}" src="{emote.URL}"/>
                   <p class="emoteText">{emote.Token}</p>
               </div>
               """;
    }
}
