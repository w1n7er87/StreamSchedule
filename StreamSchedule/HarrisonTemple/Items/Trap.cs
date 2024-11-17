using NeoSmart.Unicode;

namespace StreamSchedule.HarrisonTemple.Items;

internal class Trap : Item
{
    protected override string View => Random.Shared.GetItems(
    [
        Emoji.Bomb.Sequence.AsString,
        Emoji.MouseTrap.Sequence.AsString,
        Emoji.BowAndArrow.Sequence.AsString,
        Emoji.Firecracker.Sequence.AsString,
        Emoji.Fire.Sequence.AsString,
        Emoji.ChartDecreasing.Sequence.AsString,
        Emoji.Pill.Sequence.AsString,
    ], 1)[0];
    internal override int Reward => -Random.Shared.Next(49);
    internal override int Exp => -Random.Shared.Next(49);
}
