using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Models.Stream> Streams => Set<Models.Stream>();
    public DbSet<TextCommand> TextCommands => Set<TextCommand>();
    public DbSet<CommandAlias> CommandAliases => Set<CommandAlias>();
    public DbSet<EmoteMonitorChannel> EmoteMonitorChannels => Set<EmoteMonitorChannel>();
    public DbSet<PermittedTerm> PermittedTerms => Set<PermittedTerm>();
}

public class DatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        dbContextOptionsBuilder.UseSqlite("Data Source=bin/Release/net9.0/StreamSchedule.data");
        return new DatabaseContext((DbContextOptions<DatabaseContext>)dbContextOptionsBuilder.Options);
    }
}
