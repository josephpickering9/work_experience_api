using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<Database>));
            if (descriptor != null) services.Remove(descriptor);

            // Create a new service provider that includes Entity Framework services for PostgreSQL
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkNpgsql()
                .BuildServiceProvider();

            // Create a new unique database name or connection string for testing
            var dbName = $"TestDatabase-{Guid.NewGuid()}";
            var connectionString = GetConnectionString(dbName);

            // Add the database context using the PostgreSQL database for testing
            services.AddDbContext<Database>(options =>
            {
                options.UseNpgsql(connectionString).UseInternalServiceProvider(serviceProvider);
            });

            // Ensure the database is created and migrations are applied
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<Database>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }
        
    private static string GetConnectionString(string databaseName)
    {
        return $"Host=localhost;Port=5433;Database={databaseName};Username=testuser;Password=testpassword";
    }
}