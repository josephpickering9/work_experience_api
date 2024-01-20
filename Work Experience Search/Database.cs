namespace Work_Experience_Search;

using System.Text.RegularExpressions;
using models;
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

    }
}
