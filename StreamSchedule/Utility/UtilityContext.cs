using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StreamSchedule.Utility.Data;

namespace StreamSchedule.Utility;


public class UtilityContext : DbContext
{
    public UtilityContext(DbContextOptions<UtilityContext> options) : base(options) { }
    
    internal DbSet<Integrity> Integrities => Set<Integrity>();
    
    public static UtilityContext GetInstance() 
    {
        UtilityContext instance = new(new DbContextOptionsBuilder<UtilityContext>().UseSqlite("Data Source=Utility.data").Options);
        instance.Database.EnsureCreated();
        return instance;
    }
}


public class UtilityContextFactory : IDesignTimeDbContextFactory<UtilityContext>
{
    public UtilityContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder ob = new DbContextOptionsBuilder<UtilityContext>();
        ob.UseSqlite("Data Source=bin/Release/net9.0/Utility.data");
        return new((DbContextOptions<UtilityContext>)ob.Options);
    }
}
