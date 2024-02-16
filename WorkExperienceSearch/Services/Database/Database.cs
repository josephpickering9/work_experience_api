using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public class Database(DbContextOptions<Database> options) : DbContext(options)
{
    public DbSet<Project> Project { get; set; }
    public DbSet<ProjectImage> ProjectImage { get; set; }
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

        modelBuilder
            .Entity<ProjectImage>()
            .Property(e => e.Type)
            .HasConversion(
                v => v.ToDescriptionString(),
                v => EnumExtensions.FromDescriptionString<ImageType>(v));

        modelBuilder.Entity<Project>()
            .HasIndex(b => b.Slug)
            .IsUnique();

        modelBuilder.Entity<Tag>()
            .HasIndex(b => b.Slug)
            .IsUnique();

        modelBuilder.Entity<Company>()
            .HasIndex(b => b.Slug)
            .IsUnique();
    }
}
