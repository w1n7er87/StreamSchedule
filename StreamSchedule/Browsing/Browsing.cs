using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace StreamSchedule.Browsing;

public static class Browsing
{
    private static DateTime NextUpdate = DateTime.MinValue;
    private static readonly Dictionary<string, string> Headers = [];
    
    public static async Task Init()
    {
        while (true)
        {
            if (DateTime.Now > NextUpdate)
            {
                BotCore.Nlog.Info("Obtaining new integrity ... ");
                Integrity i = await ObtainIntegrity();
                if (!string.IsNullOrEmpty(i.Token) && !string.IsNullOrEmpty(i.DeviceID))
                {
                    Headers["Client-Integrity"] = i.Token;
                    Headers["X-Device-Id"] = i.DeviceID;
                    
                    GraphQL.GraphQLClient.UpdateHeaders(Headers);

                    TimeSpan nextUpdate = new TimeSpan(Random.Shared.Next(6, 8), Random.Shared.Next(14, 23), 0);
                    BotCore.Nlog.Info($"next update is in {nextUpdate:h'h 'm'm '} ");
                    NextUpdate = DateTime.Now + nextUpdate;
                }
            }
                
            await Task.Delay(TimeSpan.FromSeconds(180));
        }
    }
    
    private static async Task<Integrity> ObtainIntegrity()
    {
        bool haveIntegrity = false;
        bool haveDeviceID = false;
        
        Integrity token = new();
        string? deviceId = "";
        string? integrity = "";
        
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
        
        options.BrowserVersion = "138";
        
        options.AddArguments(
        [
            "disable-blink-features=AutomationControlled",
            //"headless"
        ]);
        
        options.PageLoadStrategy = PageLoadStrategy.Eager;
        
        BotCore.Nlog.Info("ceating driver");

        ChromeDriver driver = new(options);
        
        NetworkRequestHandler userAgentHandler = new()
        {
            RequestMatcher = (data) => data.Headers?.TryGetValue("user-agent", out _) ?? false,
            RequestTransformer = (data) =>
            {
                data.Headers!["user-agent"] = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{Random.Shared.Next(130, 138)}.0.0.0 Safari/537.36 OPR/{Random.Shared.Next(115, 120)}.0.0.0 (Edition Yx 05)";
                return data;
            }
        };
        
        BotCore.Nlog.Info("starting navigation");
        
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        await driver.Navigate().GoToUrlAsync("https://www.google.com/");
        
        _ = driver.ExecuteScript("open(\"https://www.twitch.tv/signup/\")");
        
        await driver.Manage().Network.StartMonitoring();
        driver.Manage().Network.AddRequestHandler(userAgentHandler);
        
        driver.Manage().Network.NetworkRequestSent += (sender, args) =>
        {
            if (!haveIntegrity && args.RequestHeaders.TryGetValue("Client-Integrity", out integrity)) 
                haveIntegrity = true;

            if (!haveDeviceID && args.RequestHeaders.TryGetValue("X-Device-Id", out deviceId))
                haveDeviceID = true;
        };
        
        driver.SwitchTo().Window(driver.WindowHandles.Last());

        wait.Until( (d) =>
        {
            try
            {
                IWebElement e = d.FindElement(By.Id("password-input"));
                return e.Displayed;
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(NoSuchElementException))
                    return false;
                throw;
            }
        });

        IWebElement login = driver.FindElement(By.Id("signup-username"));
        IWebElement pass = driver.FindElement(By.Id("password-input"));

        pass.Click();
        await Task.Delay(900);
        
        pass.SendKeys("forsen123");
        await Task.Delay(900);
        
        login.Click();
        await Task.Delay(Random.Shared.Next(900, 1200));
        
        char[] forsen = ['f', 'o', 'r', 's', 'e', 'n'];
        
        for (int i = 0; i < forsen.Length; i++)
        {
            login.SendKeys(forsen[Random.Shared.Next(forsen.Length)].ToString());
            await Task.Delay(Random.Shared.Next(600, 900));
        }
        
        BotCore.Nlog.Info("waiting for token");

        await WaitUntil(() => haveIntegrity && haveDeviceID, TimeSpan.FromSeconds(5));
        
        await driver.Manage().Network.StopMonitoring();

        token.Token = integrity ?? "";
        token.DeviceID = deviceId ?? "";

        BotCore.Nlog.Info($"\n {integrity} \n {deviceId}");
        driver.Quit();
        return token;
    }

    private static async Task WaitUntil(Func<bool> f, TimeSpan timeOut)
    {
        long start = Stopwatch.GetTimestamp();
        
        while (!f() && Stopwatch.GetElapsedTime(start) < timeOut)
        {
            await Task.Delay(200);
        }
    }
}
