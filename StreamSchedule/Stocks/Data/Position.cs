namespace StreamSchedule.Stocks.Data;

public class Position
{
    public string? symbol;
    public int? quantity;
    public float? marketValue;
    public float? costBasis;
    public float? currentPrice;
    public float? lastDayPrice;
    public float? changeToday;

    public float? profitLoss => marketValue - costBasis;
    public float? profitLossPercent => profitLoss / costBasis;
    
}
