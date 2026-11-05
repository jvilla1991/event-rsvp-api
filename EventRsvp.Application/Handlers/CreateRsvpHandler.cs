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
        var trimmedEmail = request.Email?.Trim();

        // Dishes only apply to a "bring a dish" event and only for a definite "Yes".
        // For any other case we store an empty list, which also clears dishes if a
        // previous "Yes" is changed to "No"/"Maybe" on re-submission.
        var dishes = eventEntity.IsBringADish && status == RsvpStatus.Yes
            ? SanitizeDishes(request.Dishes)
            : new List<string>();

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
            // A re-submitted RSVP also refreshes the wedding fields (meal, party size, etc.)
            existing.Email = trimmedEmail;
            existing.GuestCount = request.GuestCount;
            existing.MealChoice = request.MealChoice;
            existing.Note = request.Note;
            existing.Dishes = dishes;
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
                Email = trimmedEmail,
                GuestCount = request.GuestCount,
                MealChoice = request.MealChoice,
                Note = request.Note,
                Dishes = dishes,
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
            Email = rsvp.Email,
            GuestCount = rsvp.GuestCount,
            MealChoice = rsvp.MealChoice,
            Note = rsvp.Note,
            Dishes = rsvp.Dishes,
            CreatedAt = rsvp.CreatedAt
        };
    }

    /// <summary>
    /// Maximum number of dishes a single attendee can list. A soft guardrail against abuse.
    /// </summary>
    private const int MaxDishes = 20;

    /// <summary>
    /// Trims each dish name, drops blanks, and caps the list length.
    /// </summary>
    private static List<string> SanitizeDishes(List<string>? dishes)
    {
        if (dishes == null)
            return new List<string>();

        return dishes
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .Take(MaxDishes)
            .ToList();
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
