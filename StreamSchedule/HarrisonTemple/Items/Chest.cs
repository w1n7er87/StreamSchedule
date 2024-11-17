using NeoSmart.Unicode;

namespace StreamSchedule.HarrisonTemple.Items;

internal class Chest : Item
{
    protected override string View => Emoji.Package.Sequence.AsString;
    internal override int Exp => Random.Shared.Next(4,25);
    internal override int Reward => Random.Shared.Next(4,25);
    
}
