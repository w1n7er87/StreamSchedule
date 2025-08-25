using Microsoft.EntityFrameworkCore;
using StreamSchedule.WebExport.Data;

namespace StreamSchedule.WebExport;

public class PagesContext : DbContext
{
    public PagesContext(DbContextOptions<PagesContext> options) : base(options) { }

    public DbSet<Content> PageContent => Set<Content>();
    public DbSet<EmbeddedStyle> Styles => Set<EmbeddedStyle>();
}
