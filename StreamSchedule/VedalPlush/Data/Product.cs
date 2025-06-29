namespace StreamSchedule.VedalPlush.Data;

internal class Product
{
    public bool? AvailableForSale { get; set; }
    public int? Quantity { get; set; }
    public List<Metafield?>? MetafieldsWithReference { get; set; }
}
