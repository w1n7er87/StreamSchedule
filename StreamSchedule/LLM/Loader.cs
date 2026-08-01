namespace StreamSchedule.LLM;

public static class Loader
{
    private static readonly string directoryPath = Path.Combine(AppContext.BaseDirectory, "save");
    private const string OutputFileName = "outputWeights.bin";
    private const string EmbeddingFileName = "embedding.bin";
    private const string LayerWeightsFileName = "layer{0}.bin";

    public static bool LoadWeights( int dim, int layers, int vocabSize )
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"no save for weights");
            return false;
        }
        
        string outputPath = Path.Combine(directoryPath, OutputFileName);
        using (var br = new BinaryReader(File.OpenRead(outputPath)))
        {
            long totalOutputElements = (long)vocabSize * dim;
            Model.OutputProjection = new float[totalOutputElements];
            Model.OutputBiases = new float[vocabSize];

            for (long i = 0; i < totalOutputElements; i++) { Model.OutputProjection[i] = br.ReadSingle(); }

            if (br.BaseStream.Position < br.BaseStream.Length)
            {
                for (int i = 0; i < vocabSize; i++)
                {
                    if (br.BaseStream.Position >= br.BaseStream.Length) break;
                    Model.OutputBiases[i] = br.ReadSingle();
                }
            }
        }

        string embedPath = Path.Combine(directoryPath, EmbeddingFileName);
        using (var br = new BinaryReader(File.OpenRead(embedPath)))
        {
            long totalEmbedElements = (long)vocabSize * dim;
            Model.Embedding = new float[totalEmbedElements];

            for (long i = 0; i < totalEmbedElements; i++) { Model.Embedding[i] = br.ReadSingle(); }
        }

        Model.WAccept = new float[layers][];
        Model.WDecay = new float[layers][];
        Model.WKey = new float[layers][];
        Model.WValue = new float[layers][];

        for (int i = 0; i < layers; i++)
        {
            string layerPath = Path.Combine(directoryPath, string.Format(LayerWeightsFileName, i));
            if (!File.Exists(layerPath))
            {
                Console.WriteLine($"no save for layers");
                return false;
            }

            using (var br = new BinaryReader(File.OpenRead(layerPath)))
            {
                Model.WAccept[i] = new float[dim];
                Model.WDecay[i] = new float[dim];
                Model.WKey[i] = new float[dim];
                Model.WValue[i] = new float[dim];
                for (int j = 0; j < dim; j++) Model.WAccept[i][j] = br.ReadSingle();
                for (int j = 0; j < dim; j++) Model.WDecay[i][j] = br.ReadSingle();
                for (int j = 0; j < dim; j++) Model.WKey[i][j] = br.ReadSingle();
                for (int j = 0; j < dim; j++) Model.WValue[i][j] = br.ReadSingle();
            }
        }
        
        return true;
    }
}