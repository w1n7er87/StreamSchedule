using StreamSchedule.Data;
using StreamSchedule.Stocks;
using StreamSchedule.Stocks.Data;

namespace StreamSchedule.Commands;

internal class Stocks : Command
{
    public override string Call => "stocks";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "neuro's portfolio";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Minute);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];
    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        Portfolio? portfolio = await StocksClient.GetPortfolio();
        
        History? latest = portfolio?.history?.OrderBy(x => x?.timestamp).LastOrDefault();
        float? total = portfolio?.account?.equity - 137.15f;
        float? totalCost = portfolio?.account?.originalInvestment;
        float? change = total - totalCost;
        float? changePercent = change / totalCost * 100;
        List<Position?> positionsByProfitLoss = portfolio?.positions?.OrderBy(x => x?.profitLoss).ToList() ?? [];
        List<Position?> positionsByProfitLossPercent = portfolio?.positions?.OrderBy(x => x?.profitLossPercent).ToList() ?? [];
        Position? lossValue = positionsByProfitLoss.FirstOrDefault();
        Position? lossPercent = positionsByProfitLossPercent.FirstOrDefault();
        Position? profitValue = positionsByProfitLoss.LastOrDefault();
        Position? profitPercent = positionsByProfitLossPercent.LastOrDefault();
        
        string positionSummary = $"({lossValue?.symbol}: {FormatMoney(lossValue?.profitLoss)} / {profitValue?.symbol}: {FormatMoney(profitValue?.profitLoss)}) ({lossPercent?.symbol}: {MathF.Round(lossPercent?.profitLossPercent ?? 0, 3)}% / {profitPercent?.symbol}: {MathF.Round(profitPercent?.profitLossPercent ?? 0, 3)}%) ";
        return new CommandResult($"{FormatMoney(total)} ({FormatMoney(change)} / {MathF.Round(changePercent ?? 0, 3)}%) {positionSummary}");
    }
    
    private static string FormatMoney(float? value) => value > 0 ? $"${MathF.Round(Math.Abs(value ?? 0), 3)}" : $"-${MathF.Round(Math.Abs(value ?? 0), 3)}";

}
