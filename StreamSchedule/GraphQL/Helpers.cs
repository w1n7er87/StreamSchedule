﻿using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.GraphQL;

public static class Helpers
{
    public static string EmoteTypeToString(EmoteType emoteType)
    {
        return emoteType switch
        {
            EmoteType.CHANNEL_POINTS => "channel points",
            EmoteType.BITS_BADGE_TIERS => "bit reward",
            EmoteType.SUBSCRIPTIONS => "sub",
            EmoteType.PRIME => "Twitch prime",
            EmoteType.TURBO => "Twitch turbo",
            EmoteType.TWO_FACTOR => "2FA",
            EmoteType.SMILIES => "Twitch smiles",
            EmoteType.GLOBALS => "Twitch global",
            EmoteType.LIMITED_TIME => "limited time",
            EmoteType.HYPE_TRAIN => "hype train",
            EmoteType.MEGA_COMMERCE => "mega commerce",
            EmoteType.ARCHIVE => "archived",
            EmoteType.UNKNOWN or _ => "unknown",
        };
    } 
}
