using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

            // Add a new database context using an in-memory database for testing
            services.AddDbContext<Database>(options => { options.UseInMemoryDatabase("InMemoryDbForTesting"); });

            // Mock other services that you are using
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<Database>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting")
                    .UseInternalServiceProvider(serviceProvider);
            });

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<Database>();
                db.Database.EnsureCreated();
            }
        });
    }
}
