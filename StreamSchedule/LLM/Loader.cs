namespace StreamSchedule.LLM;

public static class Loader
{
    private static readonly string directoryPath = Path.Combine(AppContext.BaseDirectory, "save");
    private const string ModelFileName = "model.bin";

    public static (bool succ, int d, int l, int v) LoadWeights()
    {
        string modelPath = Path.Combine(directoryPath, ModelFileName);
        if (!Directory.Exists(directoryPath) || !File.Exists(modelPath))
        {
            Console.WriteLine($"no save for weights");
            return (false, 0, 0, 0);
        }

        using var modelReader = new BinaryReader(File.OpenRead(modelPath));

        int d = modelReader.ReadInt32();
        int l = modelReader.ReadInt32();
        int v = modelReader.ReadInt32();

        long totalOutputElements = (long)v * d;
        Model.OutputProjection = new float[totalOutputElements];
        Model.OutputBiases = new float[v];

        for (long i = 0; i < totalOutputElements; i++) { Model.OutputProjection[i] = modelReader.ReadSingle(); }
        for (int i = 0; i < v; i++) { Model.OutputBiases[i] = modelReader.ReadSingle(); }

        long totalEmbedElements = (long)v * d;
        Model.Embedding = new float[totalEmbedElements];

        for (long i = 0; i < totalEmbedElements; i++) { Model.Embedding[i] = modelReader.ReadSingle(); }

        Model.WAccept = new float[l][];
        Model.WDecay = new float[l][];
        Model.WKey = new float[l][];
        Model.WValue = new float[l][];

        for (int i = 0; i < l; i++)
        {
            Model.WAccept[i] = new float[d];
            Model.WDecay[i] = new float[d];
            Model.WKey[i] = new float[d];
            Model.WValue[i] = new float[d];
            for (int j = 0; j < d; j++) Model.WAccept[i][j] = modelReader.ReadSingle();
            for (int j = 0; j < d; j++) Model.WDecay[i][j] = modelReader.ReadSingle();
            for (int j = 0; j < d; j++) Model.WKey[i][j] = modelReader.ReadSingle();
            for (int j = 0; j < d; j++) Model.WValue[i][j] = modelReader.ReadSingle();

        }
        return (true, d, l, v);
    }
}