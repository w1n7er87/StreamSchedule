using NeoSmart.Unicode;

namespace StreamSchedule.HarrisonTemple.Items;

internal class Nothing : Item
{
    protected override string View =>
        Random.Shared.GetItems(
        [
            Emoji.DashingAway.Sequence.AsString,
            Emoji.FallenLeaf.Sequence.AsString,
            Emoji.SkullAndCrossbones.Sequence.AsString,
        ], 1)[0];
    internal override int Reward => Random.Shared.Next(0, 5);
    internal override int Exp => Random.Shared.Next(0, 5);
}
