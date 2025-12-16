using EventRsvp.Application.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace EventRsvp.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CreateRsvpHandler>();
        services.AddScoped<GetRsvpsHandler>();

        return services;
    }
}

