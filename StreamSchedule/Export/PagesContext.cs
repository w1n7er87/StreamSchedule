using Microsoft.EntityFrameworkCore;
using StreamSchedule.Export.Data;

namespace StreamSchedule.Export;

public class PagesContext : DbContext
{
    public PagesContext(DbContextOptions<PagesContext> options) : base(options)
    {
        
    }
    
    public DbSet<Content> PageContent => Set<Content>();
    public DbSet<EmbeddedStyle> Styles => Set<EmbeddedStyle>();
}
