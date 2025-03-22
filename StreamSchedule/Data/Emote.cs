using StreamSchedule.GraphQL.Data;
using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace StreamSchedule.Data;

public enum EmoteCostType
{
    Unknown,
    Subscription,
    Bits,
    Follow,
}

public record EmoteCost(EmoteCostType Type, int Value)
{
    public override string ToString()
    {
        return $"{Type switch
        {
            EmoteCostType.Unknown => "",
            EmoteCostType.Subscription => "Sub",
            EmoteCostType.Bits => "Bits",
            EmoteCostType.Follow => "Follow",
            _ => "",
        }} {(Value > 0 ? Value.ToString() : "")}";
    }
}

public record Emote(string ID, EmoteCost Cost, string Token, bool Animated)
{
    public static implicit operator Emote(ChannelEmote channelEmote)
    {
        return new Emote(ID: channelEmote.Id, Cost: new EmoteCost(
            Type: channelEmote.EmoteType switch
            {
                "bitstier" => EmoteCostType.Bits, "subscription" => EmoteCostType.Subscription,
                "follow" => EmoteCostType.Follow, _ => EmoteCostType.Unknown
            },
            Value: int.TryParse(channelEmote.Tier, out int tierValue) ? tierValue : 0
        ), Token: channelEmote.Name, channelEmote.EmoteType.Contains("animated"));
    }

    public static implicit operator Emote(GraphQL.Data.Emote gqlEmote)
    {
        return new Emote(ID: gqlEmote.ID ?? "", new EmoteCost(
            Type: gqlEmote.Type switch
            {
                EmoteType.BITS_BADGE_TIERS => EmoteCostType.Bits,
                EmoteType.SUBSCRIPTIONS => EmoteCostType.Subscription,
                EmoteType.FOLLOWER => EmoteCostType.Follow,
                _ => EmoteCostType.Unknown,
            } ,
            Value: gqlEmote.Type switch
            {
                EmoteType.BITS_BADGE_TIERS => gqlEmote.BitsBadgeTierSummary?.Threshold ?? 0,
                EmoteType.SUBSCRIPTIONS => gqlEmote.SubscriptionTier switch {
                    SubscriptionSummaryTier.TIER_1 => 1,
                    SubscriptionSummaryTier.TIER_2 => 2,
                    SubscriptionSummaryTier.TIER_3 => 3,
                    _ => 0
                },
                EmoteType.FOLLOWER => 0,
                _ => 0,
                
            }), Token: gqlEmote.Token ?? "", gqlEmote.AssetType is EmoteAssetType.ANIMATED);
    }

    public override string ToString()
    {

        return $"{Token} ({Cost}{(Animated ? "A" : "")})";
    }
};
