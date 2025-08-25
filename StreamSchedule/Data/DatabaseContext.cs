using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    internal DbSet<User> Users => Set<User>();
    internal DbSet<Models.Stream> Streams => Set<Models.Stream>();
    internal DbSet<TextCommand> TextCommands => Set<TextCommand>();
    internal DbSet<CommandAlias> CommandAliases => Set<CommandAlias>();
    internal DbSet<EmoteMonitorChannel> EmoteMonitorChannels => Set<EmoteMonitorChannel>();
    internal DbSet<PermittedTerm> PermittedTerms => Set<PermittedTerm>();
}

public class DatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        dbContextOptionsBuilder.UseSqlite("Data Source=bin/Release/net9.0/StreamSchedule.data");
        return new((DbContextOptions<DatabaseContext>)dbContextOptionsBuilder.Options);
    }
}
