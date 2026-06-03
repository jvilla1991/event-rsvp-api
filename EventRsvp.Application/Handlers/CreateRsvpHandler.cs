using EventRsvp.Application.DTOs;
using EventRsvp.Application.Services;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Enums;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class CreateRsvpHandler
{
    private readonly IRsvpRepository _rsvpRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IEmailService _emailService;

    public CreateRsvpHandler(
        IRsvpRepository rsvpRepository,
        IEventRepository eventRepository,
        IInviteRepository inviteRepository,
        IEmailService emailService)
    {
        _rsvpRepository = rsvpRepository;
        _eventRepository = eventRepository;
        _inviteRepository = inviteRepository;
        _emailService = emailService;
    }

    public async Task<RsvpResponse> HandleAsync(int eventId, CreateRsvpRequest request, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
            throw new Domain.Exceptions.InvalidRsvpException($"Event with ID {eventId} not found.");

        var trimmedName = request.Name.Trim();
        var proposedTime = request.WillAttend ? null : request.ProposedTime;

        // Upsert: update existing RSVP for this person if one already exists
        var existing = await _rsvpRepository.GetByEventIdAndNameAsync(eventId, trimmedName, cancellationToken);

        Rsvp rsvp;
        if (existing != null)
        {
            existing.WillAttend = request.WillAttend;
            existing.ProposedTime = proposedTime;
            rsvp = await _rsvpRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var newRsvp = new Rsvp
            {
                EventId = eventId,
                Name = trimmedName,
                WillAttend = request.WillAttend,
                ProposedTime = proposedTime,
                CreatedAt = DateTime.UtcNow
            };
            newRsvp.Validate();
            rsvp = await _rsvpRepository.AddAsync(newRsvp, cancellationToken);
        }

        // Update the invite status when the person responds
        // Prefer token-based lookup (more reliable), fall back to name match
        Invite? invite = null;
        if (!string.IsNullOrWhiteSpace(request.InviteToken))
            invite = await _inviteRepository.GetByTokenAsync(request.InviteToken, cancellationToken);

        if (invite == null)
            invite = await _inviteRepository.GetByEventIdAndNameAsync(eventId, trimmedName, cancellationToken);

        if (invite != null)
        {
            invite.Status = request.WillAttend ? InviteStatus.Accepted : InviteStatus.Declined;
            await _inviteRepository.UpdateAsync(invite, cancellationToken);
        }

        // Send notification if the attendee proposed an alternative time
        if (rsvp.ProposedTime.HasValue)
        {
            await _emailService.SendTimeProposalNotificationAsync(
                rsvp.Name,
                eventEntity.Title,
                rsvp.ProposedTime.Value,
                cancellationToken);
        }

        // Send notification if the attendee proposed an alternative time
        if (createdRsvp.ProposedTime.HasValue)
        {
            await _emailService.SendTimeProposalNotificationAsync(
                createdRsvp.Name,
                eventEntity.Title,
                createdRsvp.ProposedTime.Value,
                cancellationToken);
        }

        return new RsvpResponse
        {
            Id = rsvp.Id,
            Name = rsvp.Name,
            WillAttend = rsvp.WillAttend,
            ProposedTime = rsvp.ProposedTime,
            CreatedAt = rsvp.CreatedAt
        };
    }
}
