﻿using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.GraphQL;

public static class Helpers
{
    public static string EmoteTypeToString(EmoteType? emoteType) => emoteType switch
    {
        EmoteType.CHANNEL_POINTS => "channel points",
        EmoteType.BITS_BADGE_TIERS => "bits",
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
        EmoteType.FOLLOWER => "follow",
        EmoteType.UNKNOWN or _ => "unknown",
    };

    public static string SubscriptionSummaryTierToString(SubscriptionSummaryTier? subscriptionSummaryTier) => subscriptionSummaryTier switch
    {
        SubscriptionSummaryTier.TIER_1 => "T1",
        SubscriptionSummaryTier.TIER_2 => "T2",
        SubscriptionSummaryTier.TIER_3 => "T3",
        _ => "",
    };

    public static string HypeTrainDifficultyToString(HypeTrainDifficulty? difficulty) => difficulty switch
    {
        HypeTrainDifficulty.EASY => "easy",
        HypeTrainDifficulty.MEDIUM => "medium",
        HypeTrainDifficulty.HARD => "hard",
        HypeTrainDifficulty.SUPER_HARD => "super hard",
        HypeTrainDifficulty.INSANE => "insane",
        HypeTrainDifficulty.UNKNOWN or _ => "",
    };

    public static string UserRolesIsPartnerOrAffiliate(UserRoles? roles)
    {
        return (roles?.IsAffiliate ?? false, roles?.IsPartner ?? false) switch
        {
            (true, _) => "affiliate",
            (_, true) => "partner",
            _ => ""
        };
    }

    public static string UserRolesIsStaff(UserRoles? roles)
    {
        if ((roles?.IsStaff ?? false) || (roles?.IsGlobalMod ?? false) || (roles?.IsSiteAdmin ?? false)) return "staff";
        return "";
    }

    public static string UserErrorReasonToString(GetUserErrorReason? reason) => reason switch
    {
        GetUserErrorReason.DEACTIVATED => "Deleted",
        GetUserErrorReason.DMCA => "BAND DMCA",
        GetUserErrorReason.TOS_INDEFINITE => "BAND TOS",
        GetUserErrorReason.TOS_TEMPORARY => "temporary BAND TOS",
        _ => "",
    };
}
