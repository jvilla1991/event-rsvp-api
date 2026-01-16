using EventRsvp.Application.Handlers;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EventRsvp.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CreateRsvpHandler>();
        services.AddScoped<GetRsvpsHandler>();
        services.AddScoped<GetRsvpsByEventIdHandler>();
        services.AddScoped<GetEventsHandler>();
        services.AddScoped<GetEventHandler>();
        services.AddScoped<CreateEventHandler>();
        services.AddScoped<UpdateEventHandler>();
        services.AddScoped<DeleteEventHandler>();
        services.AddScoped<LoginHandler>();
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

        return services;
    }
}

