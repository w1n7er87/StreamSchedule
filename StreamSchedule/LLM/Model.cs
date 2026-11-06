using System.Numerics.Tensors;

namespace StreamSchedule.LLM;

public static class Model
{
    private static int dim = 1;
    private static int layers = 1;
    private static int vocab = 1;
    public static long ParamCount = 0;

    public static float[] Embedding;
    public static float[] OutputProjection;
    public static float[] OutputBiases;
    public static float[][] TM_WAccept;
    public static float[][] TM_WKey;
    public static float[][] TM_WValue;
    public static float[][] TM_WDecay;
    public static float[][] TM_WBonus;
    public static float[][] TM_WMixK;
    public static float[][] TM_WMixV;
    public static float[][] TM_WMixR;
    public static float[][] TM_LNWeight;
    public static float[][] TM_LNBias;
    public static float[][] CM_WKey;
    public static float[][] CM_WValue;
    public static float[][] CM_WReception;
    public static float[][] CM_WMixK;
    public static float[][] CM_WMixR;
    private static float[][] BoundDecay;

    public static void Initialize(int d, int l, int v)
    {
        dim = d;
        layers = l;
        vocab = v;
        Embedding = new float[v * d];
        OutputProjection = new float[v * d];
        OutputBiases = new float[v];

        TM_WAccept = AllocateLayers(d * d);
        TM_WKey = AllocateLayers(d * d);
        TM_WValue = AllocateLayers(d * d);
        TM_WDecay = AllocateLayers(d);
        TM_WBonus = AllocateLayers(d);
        TM_WMixK = AllocateLayers(d);
        TM_WMixV = AllocateLayers(d);
        TM_WMixR = AllocateLayers(d);
        TM_LNWeight = AllocateLayers(d);
        TM_LNBias = AllocateLayers(d);
        CM_WKey = AllocateLayers(d * 2 * d);
        CM_WValue = AllocateLayers(d * d * 2);
        CM_WReception = AllocateLayers(d * d);
        CM_WMixK = AllocateLayers(d);
        CM_WMixR = AllocateLayers(d);
        BoundDecay = new float[l][];
    }

    private static float[][] AllocateLayers(int size)
    {
        float[][] arr = new float[layers][];
        for (int i = 0; i < layers; i++) arr[i] = new float[size];
        return arr;
    }

    public static void CalculateDecay()
    {
        for (int i = 0; i < layers; i++)
        {
            BoundDecay[i] = new float[dim];
            for (int j = 0; j < dim; j++)
            {
                float sigmoidDecay = 1.0f / (1.0f + MathF.Exp(-TM_WDecay[i][j]));
                BoundDecay[i][j] = (sigmoidDecay * 6.0f);
            }
        }
    }

    private static void ExecuteLayerForward(Context ctx)
    {
        Span<float> x = ctx.CurrentInput.AsSpan(0, dim);

        Span<float> scratchDim = ctx.ScratchDimB.AsSpan(0, dim);
        Span<float> scratchDim4 = ctx.ScratchDim4.AsSpan(0, dim * 2);

        Span<float> kX = ctx.ScratchMixK.AsSpan(0, dim);
        Span<float> vX = ctx.ScratchMixV.AsSpan(0, dim);
        Span<float> rX = ctx.ScratchMixR.AsSpan(0, dim);
        Span<float> gateR = ctx.ScratchGateR.AsSpan(0, dim);
        Span<float> gateK = ctx.ScratchGateK.AsSpan(0, dim);
        Span<float> gateV = ctx.ScratchGateV.AsSpan(0, dim);
        Span<float> tmOutput = ctx.ScratchTMOut.AsSpan(0, dim);

        Span<float> normalizedXBuffer = ctx.ScratchXBuffer.AsSpan(0, dim);

        for (int l = 0; l < layers; l++)
        {
            Span<float> stateTimeX = ctx.StatesTimeX[l].AsSpan(0, dim);
            Span<float> numState = ctx.StatesNum[l].AsSpan(0, dim);
            Span<float> denState = ctx.StatesDen[l].AsSpan(0, dim);
            Span<float> maxState = ctx.StatesMax[l].AsSpan(0, dim);
            ApplyParameterFreeLayerNormIsolated(x, normalizedXBuffer);

            ComputeMix(normalizedXBuffer, stateTimeX, TM_WMixK[l], kX);
            ComputeMix(normalizedXBuffer, stateTimeX, TM_WMixV[l], vX);
            ComputeMix(normalizedXBuffer, stateTimeX, TM_WMixR[l], rX);

            normalizedXBuffer.CopyTo(stateTimeX);

            MatrixVectorMultiply(rX, TM_WAccept[l], gateR, dim, dim);
            TensorPrimitives.Sigmoid(gateR, gateR);

            MatrixVectorMultiply(kX, TM_WKey[l], gateK, dim, dim);
            MatrixVectorMultiply(vX, TM_WValue[l], gateV, dim, dim);

            ReadOnlySpan<float> bonusWeights = TM_WBonus[l].AsSpan(0, dim);

            for (int i = 0; i < dim; i++)
            {
                float kt = gateK[i];
                float vt = gateV[i];
                float bonus = bonusWeights[i];

                float boundedDecay = BoundDecay[l][i];
                float decayFactor = MathF.Exp(-boundedDecay);

                float globalMax = MathF.Max(maxState[i], kt);

                float expMaxOldDiff = MathF.Max(-30.0f, MathF.Min(0.0f, maxState[i] - globalMax));
                float expMaxOld = MathF.Exp(expMaxOldDiff);

                float finalNum = vt + (expMaxOld * numState[i]);
                float finalDen = 1.0f + (expMaxOld * denState[i]);

                float bonusDiff = MathF.Max(-30.0f, MathF.Min(30.0f, (kt + bonus) - globalMax));
                float expBonus = MathF.Exp(bonusDiff);

                float outputNum = finalNum + (expBonus * vt);
                float outputDen = finalDen + expBonus;

                if (outputDen < 1e-7f) outputDen = 1e-7f;
                tmOutput[i] = outputNum / outputDen;

                numState[i] = finalNum * decayFactor;
                denState[i] = finalDen * decayFactor;
                maxState[i] = globalMax;
            }

            TensorPrimitives.Multiply(gateR, tmOutput, tmOutput);
            ApplyLayerNorm(tmOutput, TM_LNWeight[l], TM_LNBias[l], scratchDim);
            TensorPrimitives.Add(x, scratchDim, x);
            ApplyParameterFreeLayerNormIsolated(x, normalizedXBuffer);
            Span<float> stateChannelX = ctx.StatesChannelX[l].AsSpan(0, dim);
            ComputeMix(normalizedXBuffer, stateChannelX, CM_WMixK[l], kX);
            ComputeMix(normalizedXBuffer, stateChannelX, CM_WMixR[l], rX);
            normalizedXBuffer.CopyTo(stateChannelX);
            MatrixVectorMultiply(rX, CM_WReception[l], gateR, dim, dim);
            TensorPrimitives.Sigmoid(gateR, gateR);
            MatrixVectorMultiply(kX, CM_WKey[l], scratchDim4, dim * 2, dim);
            TensorPrimitives.Max(scratchDim4, 0.0f, scratchDim4);
            TensorPrimitives.Multiply(scratchDim4, scratchDim4, scratchDim4);
            MatrixVectorMultiply(scratchDim4, CM_WValue[l], scratchDim, dim, dim * 2);
            TensorPrimitives.Multiply(gateR, scratchDim, scratchDim);
            TensorPrimitives.Add(x, scratchDim, x);
        }
    }

    private static void ComputeMix(ReadOnlySpan<float> x, ReadOnlySpan<float> sx, ReadOnlySpan<float> wMix, Span<float> outMix)
    {
        for (int i = 0; i < dim; i++) { outMix[i] = x[i] * wMix[i] + sx[i] * (1.0f - wMix[i]); }
    }

    private static void MatrixVectorMultiply(ReadOnlySpan<float> vec, ReadOnlySpan<float> matrix, Span<float> result, int outRows, int inCols)
    {
        for (int r = 0; r < outRows; r++) { result[r] = TensorPrimitives.Dot(vec, matrix.Slice(r * inCols, inCols)); }
    }

    private static void ApplyLayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> weight, ReadOnlySpan<float> bias, Span<float> output)
    {
        int n = input.Length;
        float mean = TensorPrimitives.Sum(input) / n;
        TensorPrimitives.Subtract(input, mean, output);
        float varianceSum = TensorPrimitives.Dot(output, output);
        float variance = varianceSum / n;
        float invStdDev = 1.0f / MathF.Sqrt(variance + 1e-5f);
        TensorPrimitives.Multiply(output, invStdDev, output);
        TensorPrimitives.Multiply(output, weight, output);
        TensorPrimitives.Add(output, bias, output);
    }

    private static void ApplyParameterFreeLayerNormIsolated(ReadOnlySpan<float> source, Span<float> destination, float epsilon = 1e-5f)
    {
        int n = source.Length;
        float mean = TensorPrimitives.Sum(source) / n;
        TensorPrimitives.Subtract(source, mean, destination);
        float varianceSum = TensorPrimitives.Dot(destination, destination);
        float variance = varianceSum / n;
        float invStdDev = 1.0f / MathF.Sqrt(variance + epsilon);
        TensorPrimitives.Multiply(destination, invStdDev, destination);
    }

    public static void PredictNextTokenStep(Context ctx, int currentTokenId)
    {
        int embeddingOffset = currentTokenId * dim;
        Array.Copy(Embedding, embeddingOffset, ctx.CurrentInput, 0, dim);
        ExecuteLayerForward(ctx);

        Span<float> logitsSpan = ctx.LogitsScratch.AsSpan(0, vocab);
        ReadOnlySpan<float> inputVector = ctx.CurrentInput.AsSpan(0, dim);

        MatrixVectorMultiply(inputVector, OutputProjection, logitsSpan, vocab, dim);
        TensorPrimitives.Add(logitsSpan, OutputBiases, logitsSpan);
    }
}
