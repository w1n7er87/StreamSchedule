using NeoSmart.Unicode;

namespace StreamSchedule.HarrisonTemple.Items;

internal class Precious : Item
{
    protected override string View => Random.Shared.GetItems([
        Emoji.MoneyBag.Sequence.AsString,
        Emoji.MoneyWithWings.Sequence.AsString,
        Emoji.Coin.Sequence.AsString,
        Emoji.GemStone.Sequence.AsString,
        Emoji.Ring.Sequence.AsString,
        Emoji.Crown.Sequence.AsString,
        Emoji.CreditCard.Sequence.AsString,
        Emoji.Cookie.Sequence.AsString,
    ], 1)[0];
    internal override int Reward => Random.Shared.Next(12, 25);
    internal override int Exp => Random.Shared.Next(12, 25);
}
