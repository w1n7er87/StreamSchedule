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
    DateTime? CreatedAt
    )
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

    public string Prefix => Token?[..^(Suffix?.Length ?? 0)] ?? "";

}
