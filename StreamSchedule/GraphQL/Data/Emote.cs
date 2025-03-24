namespace StreamSchedule.GraphQL.Data;

public class Emote
{
    public string? ID { get; set; }
    public User? Owner { get; set; }
    public User? Artist { get; set; }
    public EmoteBitsBadgeTierSummary? BitsBadgeTierSummary { get; set; }
    public List<SubscriptionSummary?> SubscriptionSummaries { get; set; } = [];
    public EmoteType? Type { get; set; }
    public EmoteAssetType? AssetType { get; set; }
    public string? Suffix { get; set; }
    public string? Token { get; set; }
    public string? Text { get; set; }

    public SubscriptionSummaryTier GetTier()
    {
        if (SubscriptionSummaries.Count != 0)
        {
            SubscriptionSummaryTier[] tiers = [.. SubscriptionSummaries.Where(x => x is not null && x.Tier is not null).Select(x => (SubscriptionSummaryTier)x!.Tier!)];
            if (tiers.Length == 1 && tiers[0] is SubscriptionSummaryTier.TIER_3) return SubscriptionSummaryTier.TIER_3;
            if (tiers.Length == 2 && tiers.Contains(SubscriptionSummaryTier.TIER_2) && tiers.Contains(SubscriptionSummaryTier.TIER_3)) return SubscriptionSummaryTier.TIER_2;
        }
        return SubscriptionSummaryTier.TIER_1;
    }
}
