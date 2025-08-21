namespace StreamSchedule.GraphQL.Data;

public record Emote( 
    string? ID,
    User? Owner,
    User? Artist,
    EmoteBitsBadgeTierSummary? BitsBadgeTierSummary,
    SubscriptionSummary?[]? SubscriptionSummaries,
    EmoteType? Type,
    EmoteAssetType? AssetType,
    string? Suffix,
    string? Token,
    string? Text)
{
    public SubscriptionSummaryTier GetTier()
    {
        if (SubscriptionSummaries is not null && SubscriptionSummaries.Length != 0)
        {
            SubscriptionSummaryTier[] tiers = [.. SubscriptionSummaries.Where(x => x?.Tier != null).Select(x => (SubscriptionSummaryTier)x!.Tier!)];
            if (tiers is [SubscriptionSummaryTier.TIER_3]) return SubscriptionSummaryTier.TIER_3;
            if (tiers.Length == 2 && tiers.Contains(SubscriptionSummaryTier.TIER_2) && tiers.Contains(SubscriptionSummaryTier.TIER_3)) return SubscriptionSummaryTier.TIER_2;
        }
        return SubscriptionSummaryTier.TIER_1;
    }
}
