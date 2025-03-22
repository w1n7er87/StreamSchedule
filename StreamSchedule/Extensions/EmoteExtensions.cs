using TwitchLib.Api.Helix.Models.Chat.Emotes;
using Emote = StreamSchedule.Data.Emote;

namespace StreamSchedule.Extensions;

public static class ChannelEmoteExtensions
{
    private const string _animated = "animated";
    private const char _a = 'A';
    private const char _s = 'S';

    public static bool DeserializeChangedEmote(this ChannelEmote emote, ChannelEmote other, out string deserializedAddedEmoteWithChangesWithRespectToTheOldOne)
    {
        deserializedAddedEmoteWithChangesWithRespectToTheOldOne = "";
        if (emote.Name.Equals(other.Name)) return false;
        
        string image = emote.Images.Url1X.Equals(other.Images.Url1X) ? "" : " 🖼️ => 🖼️ ";
        string tier = emote.Tier.Equals(other.Tier) ? TierToString(emote.Tier) : $"{TierToString(other.Tier)} => {TierToString(emote.Tier)}";
        string type = emote.EmoteType.Equals(other.EmoteType) ? TypeToString(emote.EmoteType) : $"{TypeToString(other.EmoteType)} => {TypeToString(emote.EmoteType)}";
        string format = FormatToString(emote.Format, other.Format);
        deserializedAddedEmoteWithChangesWithRespectToTheOldOne = $"{emote.Name} ({image}{tier}{type}{format})";
        return true;

        string TierToString(string tr)
        {
            return tr switch
            {
                "1000" => "T1",
                "2000" => "T2",
                "3000" => "T3",
                _ => ""
            };
        }

        string TypeToString(string tp)
        {
            return tp switch
            {
                "bitstier" => "B",
                "follower" => "F",
                _ => ""
            };
        }

        string FormatToString(string[] formatA, string[] formatB)
        {
            char a = formatA.Contains(_animated) ? _a : _s;
            char b = formatB.Contains(_animated) ? _a : _s;
            string uuh = (a == _a ^ b == _a) ? $"{a} => {b}" : b.ToString();
            return uuh;
        }
    }

    public static string EmoteToString(this ChannelEmote? e)
    {
        return $"{e?.Name} ({e?.Tier switch
        {
            "1000" => "T1",
            "2000" => "T2",
            "3000" => "T3",
            _ => ""
        }}{e?.EmoteType switch
        {
            "bitstier" => "B",
            "follower" => "F",
            _ => ""
        }}{((e?.Format.Contains("animated") ?? false) ? "A" : "")})";
    }
}
