using EventRsvp.Domain.Interfaces;
using EventRsvp.Infrastructure.Data;
using EventRsvp.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventRsvp.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // Check environment variable directly (set by test setup)
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isTesting = environment == "Testing";

        // Use in-memory database for testing
        if (isTesting)
        {
            var dbName = Environment.GetEnvironmentVariable("TEST_DB_NAME") ?? "TestDb";
            services.AddDbContext<EventRsvpDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        }
        else
        {
            services.AddDbContext<EventRsvpDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        services.AddScoped<IRsvpRepository, RsvpRepository>();

        return services;
    }
}

