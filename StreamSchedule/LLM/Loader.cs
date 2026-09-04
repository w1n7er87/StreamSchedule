namespace StreamSchedule.LLM;

public static class Loader
{
    private static readonly string directoryPath = Path.Combine(AppContext.BaseDirectory, "save");
    private const string ModelFileName = "model.bin";
    
    public static (bool succ, int d, int l, int v, long count) LoadWeights()
    {
        string modelPath = Path.Combine(directoryPath, ModelFileName);
        if (!Directory.Exists(directoryPath) || !File.Exists(modelPath))
        {
            Console.WriteLine("no save for weights");
            return (false, 0, 0, 0, 0);
        }
        
        long paramCount = 0;
        
        using var modelReader = new BinaryReader(File.OpenRead(modelPath));
        
        int d = modelReader.ReadInt32();
        int l = modelReader.ReadInt32();
        int v = modelReader.ReadInt32();
        Model.Initialize(d, l, v);
        
        paramCount += ReadArray(modelReader, Model.OutputProjection);
        paramCount += ReadArray(modelReader, Model.OutputBiases);
        paramCount += ReadArray(modelReader, Model.Embedding);

        for (int i = 0; i < l; i++)
        {
            paramCount += ReadArray(modelReader, Model.TM_WAccept[i]);
            paramCount += ReadArray(modelReader, Model.TM_WKey[i]);
            paramCount += ReadArray(modelReader, Model.TM_WValue[i]);
            paramCount += ReadArray(modelReader, Model.TM_WDecay[i]);
            paramCount += ReadArray(modelReader, Model.TM_WBonus[i]);
            paramCount += ReadArray(modelReader, Model.TM_WMixK[i]);
            paramCount += ReadArray(modelReader, Model.TM_WMixV[i]);
            paramCount += ReadArray(modelReader, Model.TM_WMixR[i]);
            paramCount += ReadArray(modelReader, Model.TM_LNWeight[i]);
            paramCount += ReadArray(modelReader, Model.TM_LNBias[i]);
            paramCount += ReadArray(modelReader, Model.CM_WKey[i]);
            paramCount += ReadArray(modelReader, Model.CM_WValue[i]);
            paramCount += ReadArray(modelReader, Model.CM_WReception[i]);
            paramCount += ReadArray(modelReader, Model.CM_WMixK[i]);
            paramCount += ReadArray(modelReader, Model.CM_WMixR[i]);
        }
        Model.CalculateDecay();
        Model.ParamCount = paramCount;
        return (true, d, l, v, paramCount);
    }

    private static long ReadArray(BinaryReader reader, float[] destination)
    {
        for (long i = 0; i < destination.LongLength; i++) { destination[i] = reader.ReadSingle(); }
        return destination.LongLength;
    }
}