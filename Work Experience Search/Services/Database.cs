using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;

namespace Work_Experience_Search;

public class Database : DbContext
{
    public Database(DbContextOptions<Database> options) : base(options)
    {
    }

    public DbSet<Project> Project { get; set; }
    public DbSet<Tag> Tag { get; set; }
    public DbSet<Company> Company { get; set; }

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
