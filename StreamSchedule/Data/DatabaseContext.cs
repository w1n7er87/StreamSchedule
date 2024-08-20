﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) 
    {
    }

    public DbSet<User> Users => Set<User>();
}

public class DatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        dbContextOptionsBuilder.UseSqlite("Data Source=StreamSchedule.data");
        return new DatabaseContext((DbContextOptions<DatabaseContext>)dbContextOptionsBuilder.Options);
    }
}