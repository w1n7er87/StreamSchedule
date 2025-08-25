using System.Text;
using StreamSchedule.WebExport.Data;

namespace StreamSchedule.WebExport;

public static class ExportUtils
{
    private const string charset = "elivELIV1234567890";
    private static readonly int len = charset.Length;
    public const string EmotesUrlBase = "https://w1n7er.win/ee/";

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

    public static void UpdateStyles()
    {
        List<EmbeddedStyle> styles =
            [.. BotCore.PagesDB.Styles.Where(x => Templates.Templates.EmoteUpdatesStyleName == x.Name)];
        if (styles.Count == 0 ||
            styles.FirstOrDefault(x => x.Version == Templates.Templates.EmoteUpdatesStyleVersion) is null)
        {
            BotCore.PagesDB.Styles.Add(Templates.Templates.EmoteUpdatesStyle);
            BotCore.PagesDB.SaveChanges();
        }
    }
}
