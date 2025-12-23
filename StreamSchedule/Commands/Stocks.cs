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
        
        return new CommandResult($"{FormatMoney(total)} ({FormatMoney(change)} / {changePercent:0.000}%) {(DateTime.UtcNow - DateTimeOffset.FromUnixTimeSeconds(latest?.timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime):m'm 's's '} ago");
    }

    private static string FormatMoney(float? value) => value > 0 ? $"${Math.Abs(value ?? 0):0.000}" : $"-${Math.Abs(value ?? 0):0.000}";

}
