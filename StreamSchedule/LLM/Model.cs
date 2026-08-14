using System.Numerics.Tensors;

namespace StreamSchedule.LLM;

public static class Model
{
    private static int dim = 1024;
    private static int layers = 7;
    private static int vocab = 3072;
    public static int DimensionCount => 4 * dim * layers + dim * vocab + dim * vocab;
    public static float[] Embedding { get; set; }
    public static float[] OutputProjection { get; set; }
    public static float[] OutputBiases { get; set; }
    public static float[][] WAccept { get; set; }
    public static float[][] WKey { get; set; }
    public static float[][] WValue { get; set; }
    public static float[][] WDecay { get; set; }

    static Model()
    {
        Embedding = new float[dim * vocab];
        OutputProjection = new float[dim * vocab];
        OutputBiases = new float[vocab];
        WAccept = new float[layers][];
        WKey = new float[layers][];
        WValue = new float[layers][];
        WDecay = new float[layers][];
        for (int i = 0; i < layers; i++)
        {
            WAccept[i] = new float[dim];
            WKey[i] = new float[dim];
            WValue[i] = new float[dim];
            WDecay[i] = new float[dim];
        }
    }

    public static void SetDimensions(int d, int l, int v)
    {
        dim = d;
        layers = l;
        vocab = v;
    }
    
    private static void ExecuteLayerForward(Context ctx)
    {
        Span<float> activeX = stackalloc float[dim];
        ctx.CurrentInput.AsSpan(0, dim).CopyTo(activeX);

        for (int l = 0; l < ctx.Layers; l++)
        {
            Span<float> normOutput = ctx.LayerOutput.AsSpan(0, dim);
            ApplyLayerNorm(activeX, normOutput, ctx.LayerScratches[l]);
            var scratch = ctx.LayerScratches[l];
            Span<float> stateA = ctx.StatesA[l].AsSpan(0, dim);
            Span<float> stateB = ctx.StatesB[l].AsSpan(0, dim);
            ReadOnlySpan<float> wAccept = WAccept[l].AsSpan(0, dim);
            ReadOnlySpan<float> wKey = WKey[l].AsSpan(0, dim);
            ReadOnlySpan<float> wValue = WValue[l].AsSpan(0, dim);
            ReadOnlySpan<float> wDecay = WDecay[l].AsSpan(0, dim);
            TensorPrimitives.Multiply(normOutput, wAccept, scratch.AcceptGate);
            TensorPrimitives.Sigmoid(scratch.AcceptGate, scratch.AcceptGate);
            TensorPrimitives.Multiply(normOutput, wKey, scratch.Key);
            TensorPrimitives.Clamp(scratch.Key, -1.0f, 1.0f, scratch.Key);
            TensorPrimitives.Multiply(normOutput, wValue, scratch.Value);
            TensorPrimitives.Clamp(scratch.Value, -1.0f, 1.0f, scratch.Value);
            TensorPrimitives.Exp(scratch.Key, scratch.ExpKey);
            TensorPrimitives.Multiply(scratch.ExpKey, scratch.Value, scratch.Temp1);
            TensorPrimitives.Multiply(wDecay, stateA, scratch.Temp2);
            TensorPrimitives.Add(scratch.Temp2, scratch.Temp1, stateA);
            TensorPrimitives.Clamp(stateA, -20.0f, 20.0f, stateA);
            TensorPrimitives.Multiply(wDecay, stateB, scratch.Temp2);
            TensorPrimitives.Add(scratch.Temp2, scratch.ExpKey, stateB);
            TensorPrimitives.Clamp(stateB, 0.0001f, 20.0f, stateB);

            for (int i = 0; i < dim; i++)
            {
                float denom = 1.0f + 1e-5f + stateB[i];
                scratch.Temp2[i] = stateA[i] / denom;
            }

            TensorPrimitives.Multiply(scratch.AcceptGate, scratch.Temp2, normOutput);
            TensorPrimitives.Add(activeX, normOutput, activeX);
        }

        // Push the finalized network state calculation back onto CurrentInput for logit evaluation
        activeX.CopyTo(ctx.CurrentInput.AsSpan(0, dim));
    }

    public static void PredictNextTokenStep(Context ctx, int currentTokenId)
    {
        Array.Clear(ctx.LayerOutput, 0, dim);
        int embeddingOffset = currentTokenId * dim;
        Array.Copy(Embedding, embeddingOffset, ctx.CurrentInput, 0, dim);
        ExecuteLayerForward(ctx);
        Span<float> logitsSpan = ctx.LogitsScratch.AsSpan(0, ctx.VocabSize);
        ReadOnlySpan<float> inputVector = ctx.CurrentInput.AsSpan(0, dim);
        for (int v = 0; v < ctx.VocabSize; v++)
        {
            int weightOffset = v * dim;
            ReadOnlySpan<float> rowWeights = OutputProjection.AsSpan(weightOffset, dim);
            float dotProduct = TensorPrimitives.Dot(inputVector, rowWeights);
            logitsSpan[v] = Math.Clamp(dotProduct + OutputBiases[v], -25.0f, 25.0f);
        }
    }

    private static void ApplyLayerNorm(ReadOnlySpan<float> input, Span<float> output, Context.LayerScratchpad scratch)
    {
        float mean = TensorPrimitives.Sum(input) / dim;

        Span<float> diffs = scratch.Temp1.AsSpan(0, dim);
        for (int i = 0; i < dim; i++) diffs[i] = input[i] - mean;

        float varianceSum = TensorPrimitives.Dot(diffs, diffs);
        float variance = varianceSum / dim;
        float invStdDev = 1.0f / MathF.Sqrt(variance + 1e-5f);

        TensorPrimitives.Multiply(diffs, invStdDev, output);
    }
}
