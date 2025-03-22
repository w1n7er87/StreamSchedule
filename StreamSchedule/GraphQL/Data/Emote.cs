namespace StreamSchedule.GraphQL.Data;

public class Emote
{
    public string? ID { get; set; }
    public User? Owner { get; set; }
    public User? Artist { get; set; }
    public EmoteBitsBadgeTierSummary? BitsBadgeTierSummary { get; set; }
    public SubscriptionSummaryTier? SubscriptionTier { get; set; }
    public EmoteType? Type { get; set; }
    public EmoteAssetType? AssetType { get; set; }
    public string? Suffix { get; set; }
    public string? Token { get; set; }
    public string? Text { get; set; }
}
