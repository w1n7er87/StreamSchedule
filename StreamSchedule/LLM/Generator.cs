using System.Numerics.Tensors;
using System.Reflection;
using System.Text;

namespace StreamSchedule.LLM;

public static class Generator
{
    private static int Eom;
    private static readonly Dictionary<int, Tool> Tools = [];
    private const int K = 6;
    public static void FillIds()
    {
        if (TokenizerBPE.CustomTokenToID.TryGetValue("[EOM]", out int id)) Eom = id;
        
        List<Type> tools = [.. Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(Tool)))];

        foreach (Type tool in tools)
        {
            Tool? t = (Tool?)Activator.CreateInstance(tool);
            if (t is not null && TokenizerBPE.CustomTokens.Contains(t.Token))
                Tools[TokenizerBPE.CustomTokenToID[t.Token]] = t;
        }
    }
    
    public static string Generate(Context ctx, List<int> promptInput, int tokensToGenerate, float temperature = 0.7f)
    {
        if (promptInput.Count == 0) return string.Empty;
        
        ctx.ClearContext();
        foreach (int token in promptInput) { Model.PredictNextTokenStep(ctx, token); }

        var rand = Random.Shared;
        Decoder streamDecoder = Encoding.UTF8.GetDecoder();
        char[] charBuffer = new char[256];
        var responseAccumulator = new StringBuilder();

        float invExponent = 1.0f / Math.Max(temperature, 0.01f);

        Span<(int Index, float Prob)> tokenScores = stackalloc (int Index, float Prob)[ctx.VocabSize];
        Span<(int Index, float Prob)> validScores = stackalloc (int Index, float Prob)[ctx.VocabSize];

        float repetitionPenalty = 1.2f;
        Span<int> recentTokensHistory = stackalloc int[6];
        int historyCount = 0;
        
        int terminationOffset = 5;
        int softRampStartTokenIndex = tokensToGenerate - terminationOffset;

        for (int i = 0; i < tokensToGenerate; i++)
        {
            Span<float> originalLogits = ctx.LogitsScratch.AsSpan(0, ctx.VocabSize);
            
            for (int h = 0; h < historyCount; h++)
            {
                int repeatedId = recentTokensHistory[h];
                
                if (TokenizerBPE.CustomTokenIDs.Contains(repeatedId)) continue;

                float logit = originalLogits[repeatedId];
                originalLogits[repeatedId] = logit > 0f ? logit / repetitionPenalty : logit * repetitionPenalty;
            }
            
            if (i >= softRampStartTokenIndex)
            {
                float runwayProgress = (float)(i - softRampStartTokenIndex + 1) / terminationOffset;
                float currentEom = originalLogits[Eom];
                if (currentEom > 0f) originalLogits[Eom] = currentEom * (1.0f + runwayProgress * 2.0f);
                else originalLogits[Eom] = currentEom + (runwayProgress * 15.0f);
            }
            
            float maxLogit = TensorPrimitives.Max(originalLogits);
            float globalSum = 0f;

            for (int v = 0; v < ctx.VocabSize; v++)
            {
                float expValue = MathF.Exp((originalLogits[v] - maxLogit) * invExponent);
                
                //if (TokenizerBPE.CustomTokenIDs.Contains(v) && v!= Eom) { expValue *= 0.2f; }

                tokenScores[v] = (v, expValue);
                globalSum += expValue;
            }

            if (globalSum < 1e-15f) globalSum = 1e-15f;

            int validCount = 0;
            for (int v = 0; v < ctx.VocabSize; v++)
            {
                var item = tokenScores[v];
                
                if (item.Index >= 32 || item.Index == Eom) { validScores[validCount++] = (item.Index, item.Prob / globalSum); }
            }

            Span<(int Index, float Prob)> activeSubset = validScores[..validCount];
            activeSubset.Sort((a, b) => b.Prob.CompareTo(a.Prob));

            int topK = Math.Min(K, validCount);
            float topKSum = 0f;
            for (int k = 0; k < topK; k++) topKSum += activeSubset[k].Prob;
            if (topKSum < 1e-15f) topKSum = 1e-15f;

            double roll = rand.NextDouble();
            float cumulative = 0.0f;
            int chosenId = activeSubset[0].Index;

            for (int k = 0; k < topK; k++)
            {
                cumulative += activeSubset[k].Prob / topKSum;
                if (roll <= cumulative)
                {
                    chosenId = activeSubset[k].Index;
                    break;
                }
            }

            //if (chosenId == Eom) break;

            if (Tools.TryGetValue(chosenId, out Tool? t))
            {
                string toolResult = t.Execute();
                List<int> syncTokens = TokenizerBPE.Encode(toolResult);
                foreach (int token in syncTokens) { Model.PredictNextTokenStep(ctx, token); }
                responseAccumulator.Append(toolResult);
                continue;
            }
  
            byte[] tokenBytes = TokenizerBPE.GetRawBytesFromID(chosenId);
            if (tokenBytes.Length > 0)
            {
                int charsDecoded = streamDecoder.GetChars(tokenBytes, 0, tokenBytes.Length, charBuffer, 0, false);
                if (charsDecoded > 0) { responseAccumulator.Append(charBuffer, 0, charsDecoded); }
            }

            if (historyCount < recentTokensHistory.Length)
                recentTokensHistory[historyCount++] = chosenId;
            else
            {
                for (int h = 0; h < recentTokensHistory.Length - 1; h++) { recentTokensHistory[h] = recentTokensHistory[h + 1]; }
                recentTokensHistory[^1] = chosenId;
            }
            Model.PredictNextTokenStep(ctx, chosenId);
        }

        int finalFlushCount = streamDecoder.GetChars([], 0, 0, charBuffer, 0, true);
        if (finalFlushCount > 0) { responseAccumulator.Append(charBuffer, 0, finalFlushCount); }

        return responseAccumulator.ToString();
    }
}
