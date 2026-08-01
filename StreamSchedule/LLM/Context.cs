namespace StreamSchedule.LLM;

public class Context
{
    public readonly int Layers;
    public readonly int VocabSize;

    public readonly float[][] StatesA;
    public readonly float[][] StatesB;
    public readonly LayerScratchpad[] LayerScratches;
    public readonly float[] CurrentInput;
    public readonly float[] LayerOutput;
    public readonly float[] LogitsScratch;

    public Context(int dim, int layers, int vocabSize)
    {
        Layers = layers;
        VocabSize = vocabSize;

        StatesA = new float[layers][];
        StatesB = new float[layers][];
        LayerScratches = new LayerScratchpad[layers];

        for (int i = 0; i < layers; i++)
        {
            StatesA[i] = new float[dim];
            StatesB[i] = new float[dim];
            LayerScratches[i] = new LayerScratchpad(dim);
        }

        CurrentInput = new float[dim];
        LayerOutput = new float[dim];
        LogitsScratch = new float[vocabSize];
    }

    public class LayerScratchpad(int dim)
    {
        public readonly float[] Temp1 = new float[dim];
        public readonly float[] Temp2 = new float[dim];
        public readonly float[] AcceptGate = new float[dim];
        public readonly float[] Key = new float[dim];
        public readonly float[] Value = new float[dim];
        public readonly float[] ExpKey = new float[dim];
        public readonly float[] DecayFactor = new float[dim];
    }

    public void ResetStates()
    {
        for (int i = 0; i < StatesA.Length; i++)
        {
            Array.Clear(StatesA[i], 0, StatesA.Length);
            Array.Clear(StatesB[i], 0, StatesB.Length);
        }
        Array.Clear(CurrentInput, 0, CurrentInput.Length);
        Array.Clear(LayerOutput, 0, LayerOutput.Length);
        Array.Clear(LogitsScratch, 0, LogitsScratch.Length);
    }
}