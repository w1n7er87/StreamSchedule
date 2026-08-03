using System.Text.RegularExpressions;
using TwitchLib.Client.Models;

namespace StreamSchedule.LLM;

public static partial class Inference
{
    private static readonly Context context;
    public static readonly bool AllGood;
    private static readonly string[] specialTokens = ["[EOM]", "[U]", "[AUT]", "[TIME]"];
    private static readonly int contextSize = 2048;
    
    static Inference()
    {
        context = new Context(Model.dim, Model.layers, Model.vocab);
        AllGood = TokenizerBPE.Load(specialTokens.ToList());
        AllGood = Loader.LoadWeights(Model.dim, Model.layers, Model.vocab);
    }

    public static bool Start()
    {
        BotCore.Nlog.Info($"inference model loaded {AllGood}");
        Model.Loaded = AllGood;
        return true;
    }

    public static string Generate(string user, string? cleanMessage, string? callerMessageID, bool emptyRequest, bool automated, float temperature = 0.65f)
    {
        List<int> tokenizedPrompt = [];
        
        if (!emptyRequest)
            BotCore.MessageCache.ReplaceMessage(callerMessageID, null, cleanMessage);
        else
            BotCore.MessageCache.Remove(callerMessageID);
        
        List<ChatMessage> cache = BotCore.MessageCache.GetList();
        int id = cache.Count - 1;
        while (tokenizedPrompt.Count < contextSize)
        {
            if (cache.Count == 0) break;
            string m = cache[id].Message;
            
            if ((callerMessageID?.Equals(cache[id].Id) ?? false) && emptyRequest)
            {
                id--;
                continue;
            }
            
            m = RemoveSpaces().Replace(RemoveUnicode().Replace(m, ""), " ");
            foreach (string token in specialTokens) { m = m.Replace(token, " uuh "); }
            tokenizedPrompt.InsertRange(0, TokenizerBPE.Encode($"[U]{cache[id].Username}: {m} [EOM]"));
            id--;
            if(id < 0) break;
        }

        tokenizedPrompt.AddRange(TokenizerBPE.Encode($"{(automated ? "[AUT]" : "")}[U]{user}: "));
        string result = Generator.Generate(context, tokenizedPrompt, 50, temperature);
        foreach (string specialToken in specialTokens) { result = result.Replace(specialToken, ""); }
        BotCore.MessageCache.AddFakeMessage(user, result);
        BotCore.Nlog.Info($"{tokenizedPrompt.Count} tokens generated for prompt ");
        BotCore.Nlog.Info($"decoded prompt: {TokenizerBPE.Decode(tokenizedPrompt)}");
        return result;
    }
    
    [GeneratedRegex(@"\p{C}")]
    private static partial Regex RemoveUnicode();
    [GeneratedRegex(@"\s+")]
    private static partial Regex RemoveSpaces();
}
