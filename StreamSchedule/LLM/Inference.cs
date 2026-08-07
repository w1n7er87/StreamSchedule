using System.Text.RegularExpressions;
using TwitchLib.Client.Models;

namespace StreamSchedule.LLM;

public static partial class Inference
{
    private static readonly Context context;
    public static readonly bool AllGood;
    private static readonly string[] specialTokens = ["[EOM]",  "[BOM]",  "[SPM]",  "[TIME]", ];
    private static readonly int contextSize = 512;
    
    static Inference()
    {
        context = new Context(Model.dim, Model.layers, Model.vocab);
        AllGood = TokenizerBPE.Load(specialTokens.ToList());
        AllGood = Loader.LoadWeights(Model.dim, Model.layers, Model.vocab);
    }

    public static bool Start()
    {
        BotCore.Nlog.Info($"inference model loaded {AllGood}");
        return true;
    }

    public static string Answer(string user, string? callerMessageID, string? callerContent, float temperature = 0.65f, int? ctx = null)
    {
        int ctxSize = ctx ?? contextSize;
        ctxSize = int.Clamp(ctxSize, 64, 2048);
        if (!string.IsNullOrWhiteSpace(callerContent)) BotCore.MessageCache.ReplaceMessage(callerMessageID, null, callerContent);
        else BotCore.MessageCache.Remove(callerMessageID);
        string result = Prompt(temperature, ctxSize, user);
        foreach (string specialToken in specialTokens) { result = result.Replace(specialToken, ""); }
        BotCore.MessageCache.AddFakeMessage(user, result);
        return result;
    }

    public static string Speak(string user, float temperature = 0.65f)
    {
        string result = Prompt(temperature, contextSize, user);
        BotCore.MessageCache.AddFakeMessage(user, result);
        return result;
    }

    private static string Prompt(float temperature, int ctx, string seedUser = "streamschedule")
    {
        List<int> tokenizedPrompt = [];

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
                        tokenizedPrompt.InsertRange(0, TokenizerBPE.Encode($"[BOM]@{hold.Value.u}: {hold.Value.m} [EOM]"));
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
                    tokenizedPrompt.InsertRange(0, TokenizerBPE.Encode($"[BOM]@{prev.Username}: {CleanTokens(prev.Message)} [EOM]"));
                    hold = (m.Username, CleanTokens(m.Message));
                    i--;
                    if (i < 0)
                    {
                        tokenizedPrompt.InsertRange(0, TokenizerBPE.Encode($"[BOM]@{hold.Value.u}: {hold.Value.m} [EOM]"));
                        break;
                    }

                    continue;
                }

                tokenizedPrompt.InsertRange(0, TokenizerBPE.Encode($"[BOM]@{hold.Value.u}: {hold.Value.m} [EOM]"));
                hold = (m.Username, CleanTokens(m.Message));
            }

            i--;
            if (i >= 0) continue;
            tokenizedPrompt.InsertRange(0, TokenizerBPE.Encode($"[BOM]@{hold.Value.u}: {hold.Value.m} [EOM]"));
            break;
        }

        tokenizedPrompt.AddRange(TokenizerBPE.Encode($"[BOM]@{seedUser}:"));
        
        BotCore.Nlog.Info($"{tokenizedPrompt.Count} tokens generated for prompt T:{temperature}");
        //BotCore.Nlog.Info($"decoded prompt:{TokenizerBPE.Decode(tokenizedPrompt)}");
        return Generator.Generate(context, tokenizedPrompt, 50, temperature);
    }

    private static string CleanTokens(string message)
    {
        string m = RemoveSpaces().Replace(message, " ");
        foreach (string specialToken in specialTokens) { m = m.Replace(specialToken, ""); }
        return m;
    }
    
    [GeneratedRegex(@"\s+")] private static partial Regex RemoveSpaces();
}
