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

        services.AddDbContext<EventRsvpDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IRsvpRepository, RsvpRepository>();

        return services;
    }
}

