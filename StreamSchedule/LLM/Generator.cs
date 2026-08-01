using System.Numerics.Tensors;
using System.Text;

namespace StreamSchedule.LLM;

public static class Generator
{
      public static string Generate(Context ctx, List<int> promptInput, int tokensToGenerate, float temperature = 0.7f)
    {
        if (promptInput.Count == 0) return string.Empty;
        
        ctx.ResetStates();
        foreach (int token in promptInput) { Model.PredictNextTokenStep(ctx, token); }

        var rand = Random.Shared;
        Decoder streamDecoder = Encoding.UTF8.GetDecoder();
        char[] charBuffer = new char[256];
        var responseAccumulator = new StringBuilder();

        float invExponent = 1.0f / Math.Max(temperature, 0.01f);

        Span<(int Index, float Prob)> tokenScores = stackalloc (int Index, float Prob)[ctx.VocabSize];
        Span<(int Index, float Prob)> validScores = stackalloc (int Index, float Prob)[ctx.VocabSize];

        for (int i = 0; i < tokensToGenerate; i++)
        {
            Span<float> originalLogits = ctx.LogitsScratch.AsSpan(0, ctx.VocabSize);
            
            float maxLogit = TensorPrimitives.Max(originalLogits);
            float globalSum = 0f;

            for (int v = 0; v < ctx.VocabSize; v++)
            {
                float expValue = MathF.Exp((originalLogits[v] - maxLogit) * invExponent);
                
                if (TokenizerBPE.CustomTokenIDs.Contains(v)) { expValue *= 0.2f; }

                tokenScores[v] = (v, expValue);
                globalSum += expValue;
            }

            if (globalSum < 1e-15f) globalSum = 1e-15f;

            int validCount = 0;
            for (int v = 0; v < ctx.VocabSize; v++)
            {
                var item = tokenScores[v];
                if (item.Index >= 32 || item.Index == TokenizerBPE.EOMID)
                {
                    validScores[validCount++] = (item.Index, item.Prob / globalSum);
                }
            }

            Span<(int Index, float Prob)> activeSubset = validScores[..validCount];
            activeSubset.Sort((a, b) => b.Prob.CompareTo(a.Prob));

            int topK = Math.Min(5, validCount);
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
            if (chosenId == TokenizerBPE.EOMID) break;

            byte[] tokenBytes = TokenizerBPE.GetRawBytesFromID(chosenId);
            if (tokenBytes.Length > 0)
            {
                int charsDecoded = streamDecoder.GetChars(tokenBytes, 0, tokenBytes.Length, charBuffer, 0, false);
                if (charsDecoded > 0) { responseAccumulator.Append(charBuffer, 0, charsDecoded); }
            }
            Model.PredictNextTokenStep(ctx, chosenId);
            //if (chosenId == TokenizerBPE.EOMID) break;
        }

        int finalFlushCount = streamDecoder.GetChars(Array.Empty<byte>(), 0, 0, charBuffer, 0, true);
        if (finalFlushCount > 0) { responseAccumulator.Append(charBuffer, 0, finalFlushCount); }

        return responseAccumulator.ToString();
    }

}