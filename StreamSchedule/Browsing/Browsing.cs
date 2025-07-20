using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace StreamSchedule.Browsing;

public static class Browsing
{
    private static DateTime NextUpdate = DateTime.Now;
    private static readonly Dictionary<string, string> Headers = [];
    
    public static async Task Init()
    {
        await ObtainIntegrity();
        
        while (true)
        {
            if(DateTime.Now > NextUpdate)
                await ObtainIntegrity();
            await Task.Delay(TimeSpan.FromSeconds(60));
        }
    }
    
    public static async Task ObtainIntegrity()
    {
        BotCore.Nlog.Info("Obtaining new integrity");
        bool haveIntegrity = false;
        bool haveDeviceID = false;
        
        ChromeOptions options = new();
        options.AddExcludedArguments(
        [
            "enable-automation",
            "disable-background-networking",
            "disable-backgrounding-occluded-windows",
            "disable-prompt-on-repost",
            "disable-sync",
            "test-type",
        ]);

        options.AddArguments(
        [
            "disable-blink-features=AutomationControlled",
            //"headless"
        ]);
        
        options.PageLoadStrategy = PageLoadStrategy.Eager;
        
        BotCore.Nlog.Info("ceating driver");

        ChromeDriver driver = new(options);
        
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

        NetworkRequestHandler userAgentHandler = new NetworkRequestHandler()
        {
            RequestMatcher = (data) => data.Headers?.TryGetValue("User-Agent", out _) ?? false,
            RequestTransformer = (data) =>
            {
                data.Headers!["User-Agent"] = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{Random.Shared.Next(130, 136)}.0.0.0 Safari/537.36 OPR/{Random.Shared.Next(115, 120)}.0.0.0 (Edition Yx 05)";
                return data;
            }
        };
        
        driver.Manage().Network.AddRequestHandler(userAgentHandler);
        
        driver.Manage().Network.NetworkRequestSent += (sender, args) =>
        {
            if (args.RequestHeaders.TryGetValue("Client-Integrity", out string? integrity))
            {
                Headers.TryAdd("Client-Integrity", integrity);
                haveIntegrity = true;
            }

            if (args.RequestHeaders.TryGetValue("X-Device-Id", out string? deviceId))
            {
                Headers.TryAdd("X-Device-Id", deviceId);
                haveDeviceID = true;
            }
        };
        
        BotCore.Nlog.Info("starting navigation");

        await driver.Navigate().GoToUrlAsync("https://www.google.com/");
        
        _ = driver.ExecuteScript("open(\"https://www.twitch.tv/signup/\")");
        await driver.Manage().Network.StartMonitoring();
        
        driver.SwitchTo().Window(driver.WindowHandles.Last());
        
        IWebElement login = driver.FindElement(By.Id("signup-username"));
        IWebElement pass = driver.FindElement(By.Id("password-input"));
        
        pass.Click();
        await Task.Delay(900);
        pass.SendKeys("forsen123");
        await Task.Delay(900);
        login.Click();
        await Task.Delay(900);

        char[] forsen = ['f', 'o', 'r', 's', 'e', 'n'];
        
        for (int i = 0; i < forsen.Length; i++)
        {
            login.SendKeys(forsen[Random.Shared.Next(forsen.Length)].ToString());
            await Task.Delay(Random.Shared.Next(300, 900));
        }
        
        wait.Until((a) => haveIntegrity && haveDeviceID);
        
        BotCore.Nlog.Info($"reached the destination. \nheaders: {string.Join(" ", Headers.Select(x => $"{x.Key}: {x.Value}"))}");
        GraphQL.GraphQLClient.UpdateHeaders(Headers);
        
        await driver.Manage().Network.StopMonitoring();
        driver.Quit();
        TimeSpan nextUpdate = new TimeSpan(Random.Shared.Next(14, 23), Random.Shared.Next(14, 23), 0);
        
        BotCore.Nlog.Info($"next update is in {nextUpdate:h'h 'm'm '} ");
        
        NextUpdate = DateTime.Now + nextUpdate;
    }
}
