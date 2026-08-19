namespace StreamSchedule.LLM;

public class Context
{
    public readonly int VocabSize;
    public readonly float[] CurrentInput;
    public readonly float[] LogitsScratch;
    public readonly float[][] StatesTimeX;
    public readonly float[][] StatesChannelX;
    public readonly float[][] StatesNum;
    public readonly float[][] StatesDen;
    public readonly float[][] StatesMax;
    public readonly float[] ScratchDimB;
    public readonly float[] ScratchDim4;
    public readonly float[] ScratchMixK;
    public readonly float[] ScratchMixV;
    public readonly float[] ScratchMixR;
    public readonly float[] ScratchGateR;
    public readonly float[] ScratchGateK;
    public readonly float[] ScratchGateV;
    public readonly float[] ScratchTMOut;

    public Context(int d, int l, int v)
    {
        VocabSize = v;
        CurrentInput = new float[d];
        LogitsScratch = new float[v];

        StatesTimeX = new float[l][];
        StatesChannelX = new float[l][];
        StatesNum = new float[l][];
        StatesDen = new float[l][];
        StatesMax = new float[l][];

        for (int i = 0; i < l; i++)
        {
            StatesTimeX[i] = new float[d];
            StatesChannelX[i] = new float[d];
            StatesNum[i] = new float[d];
            StatesDen[i] = new float[d];
            StatesMax[i] = new float[d];
            Array.Fill(StatesMax[i], -1e30f);
        }

        ScratchDimB = new float[d];
        ScratchDim4 = new float[d * 4];
        ScratchMixK = new float[d];
        ScratchMixV = new float[d];
        ScratchMixR = new float[d];

        ScratchGateR = new float[d];
        ScratchGateK = new float[d];
        ScratchGateV = new float[d];

        ScratchTMOut = new float[d];
    }

    public void ClearContext()
    {
        Array.Clear(CurrentInput, 0, CurrentInput.Length);
        Array.Clear(LogitsScratch, 0, LogitsScratch.Length);

        for (int i = 0; i < StatesTimeX.Length; i++)
        {
            Array.Clear(StatesTimeX[i], 0, StatesTimeX[i].Length);
            Array.Clear(StatesChannelX[i], 0, StatesChannelX[i].Length);
            Array.Clear(StatesNum[i], 0, StatesNum[i].Length);
            Array.Clear(StatesDen[i], 0, StatesDen[i].Length);
            Array.Fill(StatesMax[i], -1e30f);
        }
    }
}