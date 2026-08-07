using System.Text;
using System.Text.RegularExpressions;

namespace StreamSchedule.LLM;

public static class TokenizerBPE
{
    private static readonly Dictionary<int, byte[]> _idToToken = new();
    private static readonly Dictionary<string, int> _tokenToId = new();
    private static readonly Dictionary<(int, int), int> _merges = new();
    public static List<string> CustomTokens = [];
    public static readonly List<int> CustomTokenIDs = [];
    public static readonly Dictionary<string, int> CustomTokenToID = [];
    
    public static List<int> Encode(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];

        List<int> finalizedIds = [];

        string pattern = "(" + string.Join("|", CustomTokens.Select(Regex.Escape)) + ")";
        string[] substrings = Regex.Split(text, pattern);

        foreach (string sub in substrings)
        {
            if (string.IsNullOrEmpty(sub)) continue;

            if (_tokenToId.TryGetValue(sub, out int lockedId)) { finalizedIds.Add(lockedId); }
            else
            {
                byte[] rawBytes = Encoding.UTF8.GetBytes(sub);
                List<int> fragmentIds = rawBytes.Select(b => (int)b).ToList();

                while (fragmentIds.Count >= 2)
                {
                    (int, int)? bestPairToMerge = null;
                    int lowestRuleId = int.MaxValue;

                    for (int i = 0; i < fragmentIds.Count - 1; i++)
                    {
                        var pair = (fragmentIds[i], fragmentIds[i + 1]);
                        if (!_merges.TryGetValue(pair, out int ruleId)) continue;
                        if (ruleId >= lowestRuleId) continue;
                        lowestRuleId = ruleId;
                        bestPairToMerge = pair;
                    }

                    if (bestPairToMerge == null) break;

                    List<int> mergedList = [];
                    int targetLeft = bestPairToMerge.Value.Item1;
                    int targetRight = bestPairToMerge.Value.Item2;
                    int replacementId = lowestRuleId;

                    int j = 0;
                    while (j < fragmentIds.Count)
                    {
                        if (j < fragmentIds.Count - 1 && fragmentIds[j] == targetLeft && fragmentIds[j + 1] == targetRight)
                        {
                            mergedList.Add(replacementId);
                            j += 2;
                        }
                        else
                        {
                            mergedList.Add(fragmentIds[j]);
                            j++;
                        }
                    }
                    fragmentIds = mergedList;
                }

                finalizedIds.AddRange(fragmentIds);
            }
        }

        return finalizedIds;
    }

    public static byte[] GetRawBytesFromID(int id) => _idToToken.TryGetValue(id, out byte[]? bytes) ? bytes : [];

    public static string Decode(List<int> ids)
    {
        if (ids.Count == 0) return string.Empty;
        List<byte> outputBytes = [];
        foreach (int id in ids)
        {
            if (_idToToken.TryGetValue(id, out byte[]? bytes))
                outputBytes.AddRange(bytes);
            else
                outputBytes.Add((byte)'?'); 
        }
        return Encoding.UTF8.GetString(outputBytes.ToArray());
    }
    
    public static bool Load()
    {
        string inputDirectory = AppContext.BaseDirectory+"/save/";
        string vocabPath = Path.Combine(inputDirectory, "tokenizer.vocab");
        string mergesPath = Path.Combine(inputDirectory, "tokenizer.merges");
        string customTokensPath = Path.Combine(inputDirectory, "tokenizer.customTokens");

        if (!File.Exists(vocabPath) || !File.Exists(mergesPath) || !File.Exists(customTokensPath))
        {
            Console.WriteLine("incomplete or missing save for tokenizer");
            return false;
        }
        
        using (var sr = new StreamReader(vocabPath, Encoding.UTF8))
        {
            while (sr.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('\t');
                int id = int.Parse(parts[0]);
                byte[] rawTokenBytes = Convert.FromBase64String(parts[1]);
                _idToToken[id] = rawTokenBytes;
                string stringRepresentation = Encoding.UTF8.GetString(rawTokenBytes);
                _tokenToId[stringRepresentation] = id;
            }
        }

        using (var sr = new StreamReader(mergesPath, Encoding.UTF8))
        {
            while (sr.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('\t');
                int leftId = int.Parse(parts[0]);
                int rightId = int.Parse(parts[1]);
                int ruleResultId = int.Parse(parts[2]);
                _merges[(leftId, rightId)] = ruleResultId;
            }
        }
        
        using (var sr = new StreamReader(customTokensPath, Encoding.UTF8))
        {
            CustomTokens = [];
            while (sr.ReadLine() is { } line)
            {
                CustomTokens.Add(line);
                if (_tokenToId.TryGetValue(line, out int id))
                {
                    CustomTokenIDs.Add(id);
                    CustomTokenToID[line] = id;
                }
            }
        }
    
        Console.WriteLine($"Tokenizer loaded {_idToToken.Count} rows. {string.Join(" ",CustomTokens)}");
        return true;
    }
}