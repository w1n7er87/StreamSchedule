using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StreamSchedule.Markov2.Data;

namespace StreamSchedule.Markov2;

public class MarkovContext : DbContext
{
    public MarkovContext(DbContextOptions<MarkovContext> options) : base(options) { }
    
    internal DbSet<Token> Tokens => Set<Token>();
    internal DbSet<TokenPair> TokenPairs => Set<TokenPair>();
    internal DbSet<Token> TokensOnline => Set<Token>();
    internal DbSet<TokenPair> TokenPairsOnline => Set<TokenPair>();
}

public class MarkovContextFactory : IDesignTimeDbContextFactory<MarkovContext>
{
    public MarkovContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder ob = new DbContextOptionsBuilder<MarkovContext>();
        ob.UseSqlite("Data Source=bin/Release/net9.0/Markov2.data");
        return new((DbContextOptions<MarkovContext>)ob.Options);
    }

}    