using Newtonsoft.Json;
using StreamSchedule.Stocks.Data;

namespace StreamSchedule.Stocks;

public static class StocksClient
{
    private const string Endpoint = "https://api.github.com/repos/VedalAI/neuro-stocks-data/contents/portfolio.json?{0}";
    
    private static readonly HttpClient httpClient = new(new SocketsHttpHandler()
        { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });

    static StocksClient()
    {
        httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 OPR/124.0.0.0 (Edition Yx 05)");
    }

    public static async Task<Portfolio?> GetPortfolio()
    {
        try
        {
                var response = await httpClient.GetAsync(string.Format(Endpoint, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                Response? stock = JsonConvert.DeserializeObject<Response>(await response.Content.ReadAsStringAsync());
                Portfolio? portfolio = JsonConvert.DeserializeObject<Portfolio>(stock?.Decode() ?? "");
                return portfolio;
        }
        catch (Exception e)
        {
            BotCore.Nlog.Info($"failed to get portfolio: {e.Message}");
            return null;
        }
    }
}
