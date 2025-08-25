using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using StreamSchedule.GraphQL;

namespace StreamSchedule.Browsing;

public static class Browsing
{
    private static DateTime NextUpdate = DateTime.MinValue;
    public static bool Start => true;

    static Browsing() { Task.Run(Loop); }

    private static async Task Loop()
    {
        while (true)
        {
            if (DateTime.Now > NextUpdate)
            {
                BotCore.Nlog.Info("Obtaining new integrity ... ");
                Integrity i = await ObtainIntegrity();

                if (await GraphQLClient.VerifyIntegrity(i))
                {
                    GraphQLClient.SetIntegrity(i);
                    TimeSpan nextUpdate = new(Random.Shared.Next(12, 16), Random.Shared.Next(0, 45), 0);
                    NextUpdate = DateTime.Now + nextUpdate;
                    BotCore.Nlog.Info($"next planned update is in {nextUpdate:h'h 'm'm '} ");
                }
                else
                {
                    BotCore.Nlog.Info("the token was bad, retrying in 5m ... ");
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    continue;
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(3));
        }
    }

    public static void ScheduleUpdate() => NextUpdate = DateTime.UtcNow;

    private static async Task<Integrity> ObtainIntegrity()
    {
        bool haveIntegrity = false;
        bool haveDeviceID = false;

        string? deviceId = "";
        string? integrityToken = "";

        ChromeOptions options = new();
        options.AddExcludedArguments(
        [
            "enable-automation",
            "disable-background-networking",
            "disable-backgrounding-occluded-windows",
            "disable-prompt-on-repost",
            "disable-sync",
            "test-type"
        ]);

        options.AddArguments(
        [
            "--disk-cache-size=100000000",
            "disable-blink-features=AutomationControlled"
            //"headless"
        ]);
        options.PageLoadStrategy = PageLoadStrategy.Normal;
        options.BrowserVersion = "137";
        
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

        await driver.Navigate().GoToUrlAsync("https://www.google.com/");

        _ = driver.ExecuteScript("open(\"https://www.twitch.tv/signup/\")");

        await driver.Manage().Network.StartMonitoring();

        driver.Manage().Network.AddRequestHandler(userAgentHandler);

        driver.Manage().Network.NetworkRequestSent += (sender, args) =>
        {
            if (!haveIntegrity && args.RequestHeaders.TryGetValue("Client-Integrity", out integrityToken))
                haveIntegrity = true;

            if (!haveDeviceID && args.RequestHeaders.TryGetValue("X-Device-Id", out deviceId))
                haveDeviceID = true;
        };

        driver.SwitchTo().Window(driver.WindowHandles.Last());

        await WaitUntil(() =>
        {
            try
            {
                IWebElement e = driver.FindElement(By.Id("password-input"));
                return e.Displayed;
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(NoSuchElementException))
                    return false;
                throw;
            }
        }, TimeSpan.FromSeconds(10));

        IWebElement login = driver.FindElement(By.Id("signup-username"));
        IWebElement pass = driver.FindElement(By.Id("password-input"));

        pass.Click();
        await Task.Delay(900);

        await Task.Delay(900);

        login.Click();
        await Task.Delay(Random.Shared.Next(900, 1200));

        char[] forsen = ['f', 'o', 'r', 's', 'e', 'n'];

        for (int i = 0; i < forsen.Length; i++)
        {
            login.SendKeys(forsen[Random.Shared.Next(forsen.Length)].ToString());
            await Task.Delay(Random.Shared.Next(800, 1000));
        }

        BotCore.Nlog.Info("waiting for token");

        await WaitUntil(() => haveIntegrity && haveDeviceID, TimeSpan.FromSeconds(5));

        await driver.Manage().Network.StopMonitoring();

        BotCore.Nlog.Info($"\n {integrityToken} \n {deviceId}");
        driver.Quit();
        return new(integrityToken ?? "", deviceId ?? "");
    }

    private static async Task WaitUntil(Func<bool> f, TimeSpan timeOut)
    {
        long start = Stopwatch.GetTimestamp();
        while (!f() && Stopwatch.GetElapsedTime(start) < timeOut) await Task.Delay(200);
    }
}
