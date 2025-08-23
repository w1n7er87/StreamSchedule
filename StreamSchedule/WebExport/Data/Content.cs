namespace StreamSchedule.Export.Data;

public class Content
{
    public int Id { get; set; }
    public string? Slug { get; set; }
    public string? EmbeddedStyleName { get; set; }
    public int EmbeddedStyleVersion { get; set; } = 0;
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? HtmlContent { get; set; }
    public DateTime? CreatedAt { get; set; }
}
