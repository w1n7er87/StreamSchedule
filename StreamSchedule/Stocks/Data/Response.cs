using System.Text;

namespace StreamSchedule.Stocks.Data;

internal class Response
{
    public string? content;
    
    public string? Decode()
    {
        if (content is null) return null;

        content = content.Replace("\\n", "\n");
        return Encoding.UTF8.GetString(Convert.FromBase64String(content));
    }
}
