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
        CM_WKey = AllocateLayers(d * 4 * d);
        CM_WValue = AllocateLayers(d * d * 4);
        CM_WReception = AllocateLayers(d * d);
        CM_WMixK = AllocateLayers(d);
        CM_WMixR = AllocateLayers(d);
        BoundDecay = new float[l][];
    }

    public static void CalculateDecay()
    {
        for (int i = 0; i < layers; i++)
        {
            BoundDecay[i] = new float[dim];
            for (int j = 0; j < dim; j++)
            {
                float sigmoidDecay = 1.0f / (1.0f + MathF.Exp(-TM_WDecay[i][j]));
                BoundDecay[i][j] = -(sigmoidDecay * 6.0f);
            }
        }
    }
    
    private static float[][] AllocateLayers(int size)
    {
        float[][] arr = new float[layers][];
        for (int i = 0; i < layers; i++) arr[i] = new float[size];
        return arr;
    }

    private static void ExecuteLayerForward(Context ctx)
    {
        Span<float> x = ctx.CurrentInput.AsSpan(0, dim);

        Span<float> scratchDim = ctx.ScratchDimB.AsSpan(0, dim);
        Span<float> scratchDim4 = ctx.ScratchDim4.AsSpan(0, dim * 4);

        Span<float> kX = ctx.ScratchMixK.AsSpan(0, dim);
        Span<float> vX = ctx.ScratchMixV.AsSpan(0, dim);
        Span<float> rX = ctx.ScratchMixR.AsSpan(0, dim);
        Span<float> gateR = ctx.ScratchGateR.AsSpan(0, dim);
        Span<float> gateK = ctx.ScratchGateK.AsSpan(0, dim);
        Span<float> gateV = ctx.ScratchGateV.AsSpan(0, dim);
        Span<float> tmOutput = ctx.ScratchTMOut.AsSpan(0, dim);

        for (int l = 0; l < layers; l++)
        {
            Span<float> stateTimeX = ctx.StatesTimeX[l].AsSpan(0, dim);
            Span<float> numState = ctx.StatesNum[l].AsSpan(0, dim);
            Span<float> denState = ctx.StatesDen[l].AsSpan(0, dim);
            Span<float> maxState = ctx.StatesMax[l].AsSpan(0, dim);

            ComputeMix(x, stateTimeX, TM_WMixK[l], kX);
            ComputeMix(x, stateTimeX, TM_WMixV[l], vX);
            ComputeMix(x, stateTimeX, TM_WMixR[l], rX);

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
                
                float maxCurrent = MathF.Max(maxState[i], kt + bonus);
                float expMaxOld = MathF.Exp(maxState[i] - maxCurrent);
                float expCurrent = MathF.Exp(kt + bonus - maxCurrent);

                float num = expMaxOld * numState[i] + expCurrent * vt;
                float den = expMaxOld * denState[i] + expCurrent;

                if (MathF.Abs(den) < 1e-9f) den = 1e-9f;
                tmOutput[i] = num / den;
                
                float boundedDecay = BoundDecay[l][i];

                float maxNext = MathF.Max(maxState[i] + boundedDecay, kt);
                float expDecay = MathF.Exp(maxState[i] + boundedDecay - maxNext);
                float expK = MathF.Exp(kt - maxNext);

                numState[i] = expDecay * numState[i] + expK * vt;
                denState[i] = expDecay * denState[i] + expK;
                maxState[i] = maxNext;
            }

            x.CopyTo(stateTimeX);

            TensorPrimitives.Multiply(gateR, tmOutput, tmOutput);
            ApplyLayerNorm(tmOutput, TM_LNWeight[l], TM_LNBias[l], scratchDim);
            TensorPrimitives.Add(x, scratchDim, x);
            Span<float> stateChannelX = ctx.StatesChannelX[l].AsSpan(0, dim);
            ComputeMix(x, stateChannelX, CM_WMixK[l], kX);
            ComputeMix(x, stateChannelX, CM_WMixR[l], rX);
            x.CopyTo(stateChannelX);
            MatrixVectorMultiply(rX, CM_WReception[l], gateR, dim, dim);
            TensorPrimitives.Sigmoid(gateR, gateR);
            MatrixVectorMultiply(kX, CM_WKey[l], scratchDim4, dim * 4, dim);
            TensorPrimitives.Max(scratchDim4, 0.0f, scratchDim4);
            TensorPrimitives.Multiply(scratchDim4, scratchDim4, scratchDim4);
            MatrixVectorMultiply(scratchDim4, CM_WValue[l], scratchDim, dim, dim * 4);
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
        float mean = TensorPrimitives.Sum(input) / dim;
        float varianceSum = 0.0f;
        for (int i = 0; i < dim; i++)
        {
            float diff = input[i] - mean;
            varianceSum += diff * diff;
        }

        float variance = varianceSum / dim;
        float invStdDev = 1.0f / MathF.Sqrt(variance + 1e-5f);

        for (int i = 0; i < dim; i++) { output[i] = (input[i] - mean) * invStdDev * weight[i] + bias[i]; }
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
