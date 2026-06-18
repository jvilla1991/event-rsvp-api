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

        if (!Enum.TryParse<RsvpStatus>(request.Status, ignoreCase: true, out var status)
            || !Enum.IsDefined(typeof(RsvpStatus), status))
        {
            throw new Domain.Exceptions.InvalidRsvpException(
                $"Invalid RSVP status '{request.Status}'. Expected Yes, No, or Maybe.");
        }

        var trimmedName = request.Name.Trim();
        // A proposed time only makes sense when the person is not a definite Yes.
        var proposedTime = status == RsvpStatus.Yes ? null : request.ProposedTime;

        // Upsert: update existing RSVP for this person if one already exists
        var existing = await _rsvpRepository.GetByEventIdAndNameAsync(eventId, trimmedName, cancellationToken);

        // Enforce the attending cap. Only a new "Yes" can push us over the limit; we subtract
        // this person's own existing "Yes" so a returning attendee can re-save without being
        // wrongly rejected as full.
        if (status == RsvpStatus.Yes && eventEntity.AttendingLimit.HasValue)
        {
            var yesCount = await _rsvpRepository.GetYesCountByEventIdAsync(eventId, cancellationToken);
            var alreadyCounted = existing?.Status == RsvpStatus.Yes ? 1 : 0;
            if (yesCount - alreadyCounted >= eventEntity.AttendingLimit.Value)
            {
                throw new Domain.Exceptions.InvalidRsvpException(
                    "This event is full. No more \"Yes\" responses can be accepted.");
            }
        }

        Rsvp rsvp;
        if (existing != null)
        {
            existing.Status = status;
            existing.ProposedTime = proposedTime;
            rsvp = await _rsvpRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var newRsvp = new Rsvp
            {
                EventId = eventId,
                Name = trimmedName,
                Status = status,
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
            invite.Status = ToInviteStatus(status);
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

        return new RsvpResponse
        {
            Id = rsvp.Id,
            Name = rsvp.Name,
            Status = rsvp.Status.ToString(),
            ProposedTime = rsvp.ProposedTime,
            CreatedAt = rsvp.CreatedAt
        };
    }

    /// <summary>
    /// Maps an attendee's RSVP response onto the invite lifecycle status.
    /// </summary>
    private static InviteStatus ToInviteStatus(RsvpStatus status) => status switch
    {
        RsvpStatus.Yes => InviteStatus.Accepted,
        RsvpStatus.No => InviteStatus.Declined,
        RsvpStatus.Maybe => InviteStatus.Maybe,
        _ => InviteStatus.Opened
    };
}
