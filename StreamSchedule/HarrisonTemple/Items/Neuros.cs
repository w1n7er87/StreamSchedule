namespace StreamSchedule.HarrisonTemple.Items;

internal class Neuros : Item
{
    protected override string View => Random.Shared.GetItems(
    [
        "nwero ",
        "nuero ",
        "eliv ",
    ], 1)[0];
    internal override int Reward => Random.Shared.Next(12, 65);
    internal override int Exp => Random.Shared.Next(12, 65);
}
