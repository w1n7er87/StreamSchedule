using System.Text.RegularExpressions;

namespace StreamSchedule.LLM;

public static partial class Inference
{
    private static readonly Context context;
    private static readonly bool AllGood;
    
    static Inference()
    {
        context = new Context(Model.dim, Model.layers, Model.vocab);
        AllGood = TokenizerBPE.Load(["[EOM]", "[U]"]);
        AllGood = Loader.LoadWeights(Model.dim, Model.layers, Model.vocab);
    }

    public static bool Start()
    {
        BotCore.Nlog.Info($"inference model loaded {AllGood}");
        return true;
    }

    public static string Generate(string user, int maxContext = 320, float temperature = 0.8f)
    {
        BotCore.Nlog.Info($"request to generate,");
        List<int> tokenizedPrompt = [];
        int id = BotCore.MessageCache.Count - 1;
        while (tokenizedPrompt.Count < maxContext)
        {
            if (BotCore.MessageCache.Count == 0) break;
            
            string m = RemoveUnicode().Replace(BotCore.MessageCache[id].Message, "");
            m = RemoveSpaces().Replace(m, " ");
            tokenizedPrompt.InsertRange(0, TokenizerBPE.Encode($"[U]{BotCore.MessageCache[id].Username}: {m} [EOM]"));
            id--;
            if(id < 0) break;
        }

        List<string> users = ["victormunro", "stany_d", "lonk46", "crunchyplutonium", "eliv", "w1n7er"];
        if (string.IsNullOrEmpty(user)) user = users[Random.Shared.Next(users.Count)];
        tokenizedPrompt.AddRange(TokenizerBPE.Encode($"[U]{user}: "));
        
        BotCore.Nlog.Info($"{tokenizedPrompt.Count} tokens generated for prompt ");
        //BotCore.Nlog.Info($"decoded prompt: {TokenizerBPE.Decode(tokenizedPrompt)}");
        return Generator.Generate(context, tokenizedPrompt, 50, temperature);
    }
    
    [GeneratedRegex(@"\p{C}")]
    private static partial Regex RemoveUnicode();
    [GeneratedRegex(@"\s+")]
    private static partial Regex RemoveSpaces();
}
