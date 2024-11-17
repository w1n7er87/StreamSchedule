namespace StreamSchedule.HarrisonTemple.Items;

internal abstract class Item
{
    protected abstract string View { get; }
    internal abstract int Reward { get; }
    internal abstract int Exp { get; }
    
    public override string ToString() => View;
    
}
