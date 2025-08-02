using System.Text;

namespace StreamSchedule.Export;

public static class ExportUtils
{
    private const string charset = "elivELIV1234567890";
    private static readonly int len = charset.Length;
    
    public static string GetSlug(string data = "")
    {
        StringBuilder sb = new();
        Guid g = Guid.CreateVersion7();
        Span<byte> bytes = [.. data.Select(Convert.ToByte), .. g.ToByteArray()];
        
        int value = 0;
        int c = 0;
        foreach (byte b in bytes)
        {
            value += b;
            if (c == 1)
            {
                sb.Append(charset[value % len]);
                value = 0;
                c = 0;
                continue;
            }
            c++;
        }
        return sb.ToString();
    }
}
