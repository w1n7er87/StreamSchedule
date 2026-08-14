using System.Text.RegularExpressions;
using TwitchLib.Client.Models;

namespace StreamSchedule.LLM;

public static partial class Inference
{
    private static readonly Context context;
    public static readonly bool AllGood;
    public const int baseContext = 320;
    public const int minContext = 48;
    public const int maxContext = 1024;

    static Inference()
    {
        (bool succ, int d, int l, int v) = Loader.LoadWeights();
        AllGood = succ;
        if (succ) { Model.SetDimensions(d, l , v); }
        context = new Context(d, l, v);
        AllGood = TokenizerBPE.Load();
        if(AllGood) Generator.FillIds();
        BotCore.Nlog.Info($"inference model loaded {AllGood} {d}-{l}-{v} {Model.DimensionCount:N0} params");
    }

    public static bool Start => true;

    public static string[] Answer(string user, bool shorM,  string? callerMessageID, string? callerContent, float temperature = 0.65f, int? ctx = null)
    {
        int ctxSize = ctx ?? baseContext;
        ctxSize = int.Clamp(ctxSize, minContext, maxContext);
        if (!string.IsNullOrWhiteSpace(callerContent)) BotCore.MessageCache.ReplaceMessage(callerMessageID, null, callerContent);
        else BotCore.MessageCache.Remove(callerMessageID);
        string result = BuildAndPrompt(temperature, ctxSize, shorM, user);
        BotCore.MessageCache.AddFakeMessage(user, result);

        string[] results = result.Split("[SPM]");

        foreach (string specialToken in TokenizerBPE.CustomTokens)
        {
            for (int i = 0; i < results.Length; i++) results[i] = results[i].Replace(specialToken, "");
        }
        return results;
    }

    public static string[] Speak(string user, float temperature = 0.65f)
    {
        string result = BuildAndPrompt(temperature, baseContext, true, user);
        BotCore.MessageCache.AddFakeMessage(user, result);
        
        string[] results = result.Split("[SPM]");

        foreach (string specialToken in TokenizerBPE.CustomTokens)
        {
            for (int i = 0; i < results.Length; i++) results[i] = results[i].Replace(specialToken, "");
        }
        return results;
    }

    private static string BuildAndPrompt(float temperature, int ctx, bool shrt, string seedUser = "streamschedule")
    {
        List<int> tokenizedPrompt = [];

        List<ChatMessage> cache = BotCore.MessageCache.ToList();
        cache.Reverse();

        (string user, string message)? stash = null;

        for (int i = 0; i < cache.Count; i++) 
        {
            string mm = CleanTokens(cache[i].Message);
            if (i == 0)
            {
                stash = (cache[i].Username, mm);
                if (cache.Count == 1) prependPrompt(Format(stash));
                continue;
            }

            if (cache[i].Username.Equals(stash!.Value.user))
            {
                stash = (cache[i].Username, $"{mm} [SPM] {stash.Value.message}");
            }
            else
            {
                if (prependPrompt(Format(stash))) break;
                stash = (cache[i].Username, mm);
            }

            if (i == cache.Count - 1) { prependPrompt(Format(stash)); }
        }

        string length = shrt ? "[SHR]" : "[LNG]";
        tokenizedPrompt.AddRange(TokenizerBPE.Encode($"{length} @{seedUser}: "));
        BotCore.Nlog.Info($"{tokenizedPrompt.Count} tokens generated for prompt T:{temperature}");
        BotCore.Nlog.Info($"decoded prompt:{TokenizerBPE.Decode(tokenizedPrompt)}");
        return Generator.Generate(context, tokenizedPrompt, 50, temperature);

        bool prependPrompt(string formatted)
        {
            tokenizedPrompt = [..TokenizerBPE.Encode(formatted), ..tokenizedPrompt];
            return tokenizedPrompt.Count > ctx;
        }
    }

    private static string CleanTokens(string message)
    {
        string m = Exclamations().Replace(Questions().Replace(RemoveSpaces().Replace(message, " "), "???"), "!!!");
        foreach (string specialToken in TokenizerBPE.CustomTokens) { m = m.Replace(specialToken, ""); }
        return m;
    }

    private static string LengthToken(string message) => message.Insert(0, message.Length > 70 ? "[LNG] " : "[SHR] ");

    private static string Format((string user, string message)? userMessage) => LengthToken($"@{userMessage?.user!}: {userMessage?.message!} [EOM] ");

    [GeneratedRegex(@"\s+")] private static partial Regex RemoveSpaces();
    [GeneratedRegex(@"\?{4,}")] private static partial Regex Questions();
    [GeneratedRegex(@"!{4,}")] private static partial Regex Exclamations();
}
