namespace StreamSchedule.GraphQL.Data;

public class Emote
{
    public User? Owner { get; set; }
    public EmoteBitsBadgeTierSummary? BitsBadgeTierSummary { get; set; }
    public string Suffix { get; set; }
    public string Token { get; set; }
    public string Text { get; set; }
    public string ID { get; set; }
    public EmoteType Type { get; set; }
    public SubscriptionSummaryTier Tier { get; set; }
}
