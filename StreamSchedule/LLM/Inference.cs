using System.Text.RegularExpressions;
using TwitchLib.Client.Models;

namespace StreamSchedule.LLM;

public static partial class Inference
{
    private static readonly Context context;
    public static readonly bool AllGood;
    public const int baseContext = 256;
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
        BotCore.Nlog.Info($"inference model loaded {AllGood} {d}-{l}-{v} {d * l + d * v + d * v} params");
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

        List<string> results = result.Split("[SPM]").ToList();

        foreach (string specialToken in TokenizerBPE.CustomTokens)
        {
            for (int i = 0; i < results.Count; i++)
            {
                results[i] = results[i].Replace(specialToken, "");
            }
        }
        return results.ToArray();
    }

    public static string[] Speak(string user, float temperature = 0.65f)
    {
        string result = BuildAndPrompt(temperature, baseContext, true, user);
        BotCore.MessageCache.AddFakeMessage(user, result);
        
        List<string> results = result.Split("[SPM]").ToList();

        foreach (string specialToken in TokenizerBPE.CustomTokens)
        {
            for (int i = 0; i < results.Count; i++)
            {
                results[i] = results[i].Replace(specialToken, "");
            }
        }
        return results.ToArray();
    }

    private static string BuildAndPrompt(float temperature, int ctx, bool shrt, string seedUser = "streamschedule")
    {
        List<int> tokenizedPrompt = [];
        List<string> preTokenization = [];
        
        List<ChatMessage> cache = BotCore.MessageCache.GetList();
        int i = cache.Count - 2;

        (string u, string m)? hold = null;

        while (tokenizedPrompt.Count < ctx)
        {
            int p = i + 1;
            if (cache.Count == 0 || i < 0) break;

            ChatMessage m = cache[i];
            ChatMessage prev = cache[p];

            if (m.Username.Equals(prev.Username))
            {
                if (hold is null)
                {
                    hold = (m.Username, $"{CleanTokens(m.Message)} [SPM] {CleanTokens(prev.Message)}");
                    i--;
                    if (i < 0)
                    {
                        preTokenization.Add($"@{hold.Value.u}: {hold.Value.m}");
                        break;
                    }
                    continue;
                }
                if (!hold.Value.u.Equals(m.Username)) continue;
                hold = (m.Username, $"{CleanTokens(m.Message)} [SPM] {hold.Value.m}");
            }
            else
            {
                if (hold is null)
                {
                    preTokenization.Add($"@{prev.Username}: {CleanTokens(prev.Message)}");
                    hold = (m.Username, CleanTokens(m.Message));
                    i--;
                    if (i < 0)
                    {
                        preTokenization.Add($"@{hold.Value.u}: {hold.Value.m}");
                        break;
                    }
                    continue;
                }
                preTokenization.Add($"@{hold.Value.u}: {hold.Value.m}");
                hold = (m.Username, CleanTokens(m.Message));
            }

            i--;
            if (i >= 0) continue;
            preTokenization.Add($"@{hold.Value.u}: {hold.Value.m}");
            break;
        }

        for (int j = 0; j < preTokenization.Count; j++) { preTokenization[j] = LengthToken(preTokenization[j]) + " [EOM]"; }

        preTokenization.Reverse();
        tokenizedPrompt = TokenizerBPE.Encode(string.Join(" ", preTokenization));
        
        string length = shrt? "[SHR]" : "[LNG]";
        tokenizedPrompt.AddRange(TokenizerBPE.Encode($" {length} @{seedUser}: "));
        BotCore.Nlog.Info($"{tokenizedPrompt.Count} tokens generated for prompt T:{temperature}");
        BotCore.Nlog.Info($"decoded prompt:{TokenizerBPE.Decode(tokenizedPrompt)}");
        return Generator.Generate(context, tokenizedPrompt, 50, temperature);
    }

    private static string CleanTokens(string message)
    {
        string m = Exclamations().Replace(Questions().Replace(RemoveSpaces().Replace(message, " "), "???"), "!!!");
        foreach (string specialToken in TokenizerBPE.CustomTokens) { m = m.Replace(specialToken, ""); }
        return m;
    }

    private static string LengthToken(string message) => message.Insert(0, message.Length > 70 ? "[LNG] " : "[SHR] ");

    [GeneratedRegex(@"\s+")] private static partial Regex RemoveSpaces();
    [GeneratedRegex(@"\?{4,}")] private static partial Regex Questions();
    [GeneratedRegex(@"!{4,}")] private static partial Regex Exclamations();
}
