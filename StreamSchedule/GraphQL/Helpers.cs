using StreamSchedule.GraphQL.Data;

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

    public static string UserRolesToLongString(UserRoles? roles)
    {
        string result = "";
        if (roles is null) return result;
        if (roles.IsExtensionsDeveloper ?? false) result += "extension dev ";
        if (roles.IsParticipatingDJ ?? false) result += "DJ ";
        if (roles.IsGlobalMod ?? false) result += "global mod ";
        if (roles.IsSiteAdmin ?? false) result += "site admin ";
        if (roles.IsStaff ?? false) result += "staff ";
        if (roles.IsPreAffiliate ?? false) result += "pre-affiliate ";
        if (roles.IsAffiliate ?? false) result += "affiliate ";
        if (roles.IsPartner ?? false) result += "partner ";
        if (roles.IsMonetized ?? false) result += "monetized ";
        return result;
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
