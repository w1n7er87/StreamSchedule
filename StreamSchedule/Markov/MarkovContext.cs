using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StreamSchedule.Markov.Data;

namespace StreamSchedule.Markov;

public class MarkovContext : DbContext
{
    public MarkovContext(DbContextOptions<MarkovContext> options) : base(options)
    {
    }

    public DbSet<LinkStored> Links => Set<LinkStored>();
    public DbSet<WordCountPair> NextWords => Set<WordCountPair>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LinkStored>()
            .HasMany(l => l.NextWords)
            .WithOne(n => n.Link)
            .HasForeignKey(n => n.LinkID);
    }
}

public class MarkovContextFactory : IDesignTimeDbContextFactory<MarkovContext>
{
    public MarkovContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder<MarkovContext>();
        dbContextOptionsBuilder.UseSqlite("Data Source=bin/Release/net9.0/Markov.data");
        return new MarkovContext((DbContextOptions<MarkovContext>)dbContextOptionsBuilder.Options);
    }
}

