using Work_Experience_Search.Services;

namespace Work_Experience_Search;

using System.Text.RegularExpressions;
using Models;
using Microsoft.EntityFrameworkCore;

public class Database : DbContext
{
    public Database(DbContextOptions<Database> options) : base(options)
    {
    }
    
    public DbSet<Project> Project { get; set; }
    public DbSet<Tag> Tag { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Tag>()
            .Property(e => e.Type)
            .HasConversion(
                v => v.ToDescriptionString(),
                v => EnumExtensions.FromDescriptionString<TagType>(v));
    }
}
