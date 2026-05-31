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
        services.AddScoped<GetPollsByEventIdHandler>();
        services.AddScoped<CreatePollHandler>();
        services.AddScoped<UpdatePollHandler>();
        services.AddScoped<DeletePollHandler>();
        services.AddScoped<SubmitPollVoteHandler>();
        services.AddScoped<GetPollResultsHandler>();
        services.AddScoped<CreateInviteHandler>();
        services.AddScoped<GetInvitesByEventIdHandler>();
        services.AddScoped<ViewInviteHandler>();
        services.AddScoped<DeleteInviteHandler>();
        services.AddScoped<GetAttendanceByEventIdHandler>();
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

        return services;
    }
}

