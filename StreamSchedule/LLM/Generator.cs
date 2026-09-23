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

        float currentTemperature = Math.Max(temperature, 0.01f);

        Span<(int Index, float Prob)> tokenScores = stackalloc (int Index, float Prob)[ctx.VocabSize];

        float repetitionPenalty = 1.2f;
        Span<int> recentTokensHistory = stackalloc int[6];
        int historyCount = 0;

        int terminationOffset = 5;
        int softRampStartTokenIndex = tokensToGenerate - terminationOffset;

        for (int i = 0; i < tokensToGenerate; i++)
        {
            Span<float> logits = ctx.LogitsScratch.AsSpan(0, ctx.VocabSize);

            for (int h = 0; h < historyCount; h++)
            {
                int repeatedId = recentTokensHistory[h];
                if (TokenizerBPE.CustomTokenIDs.Contains(repeatedId)) continue;

                float logit = logits[repeatedId];
                logits[repeatedId] = logit > 0f ? logit / repetitionPenalty : logit * repetitionPenalty;
            }

            if (i >= softRampStartTokenIndex)
            {
                float runwayProgress = (float)(i - softRampStartTokenIndex + 1) / terminationOffset;
                float currentEom = logits[Eom];
                if (currentEom > 0f) logits[Eom] = currentEom * (1.0f + runwayProgress * 2.0f);
                else logits[Eom] = currentEom + (runwayProgress * 15.0f);
            }

            for (int v = 0; v < ctx.VocabSize; v++)
            {
                if (TokenizerBPE.CustomTokenIDs.Contains(v) && v != Eom) { logits[v] -= 50.0f; }
            }

            for (int v = 0; v < ctx.VocabSize; v++) { logits[v] /= currentTemperature; }

            float maxLogit = TensorPrimitives.Max(logits);

            for (int v = 0; v < ctx.VocabSize; v++)
            {
                float expValue = MathF.Exp(logits[v] - maxLogit);
                tokenScores[v] = (v, expValue);
            }

            int candidateCount = Math.Min(50, ctx.VocabSize);

            tokenScores.Sort((a, b) => b.Prob.CompareTo(a.Prob));

            int finalValidSubsetCount = 0;
            float topPSum = 0.0f;
            float targetTopP = 0.85f;
            int absoluteMinimumTokens = 4;

            for (int k = 0; k < candidateCount; k++)
            {
                topPSum += tokenScores[k].Prob;
                finalValidSubsetCount++;
                if (topPSum >= targetTopP && finalValidSubsetCount >= absoluteMinimumTokens)
                    break;
            }

            if (topPSum < 1e-15f) topPSum = 1e-15f;

            double roll = rand.NextDouble();
            float cumulative = 0.0f;
            int chosenId = tokenScores[0].Index;

            for (int k = 0; k < finalValidSubsetCount; k++)
            {
                cumulative += tokenScores[k].Prob / topPSum;
                if (roll <= cumulative)
                {
                    chosenId = tokenScores[k].Index;
                    break;
                }
            }

            if (chosenId == Eom) break;

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

            if (historyCount < recentTokensHistory.Length) recentTokensHistory[historyCount++] = chosenId;
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
