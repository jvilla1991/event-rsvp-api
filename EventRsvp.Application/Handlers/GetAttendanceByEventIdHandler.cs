using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Enums;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetAttendanceByEventIdHandler
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IRsvpRepository _rsvpRepository;

    public GetAttendanceByEventIdHandler(IInviteRepository inviteRepository, IRsvpRepository rsvpRepository)
    {
        _inviteRepository = inviteRepository;
        _rsvpRepository = rsvpRepository;
    }

    public async Task<IEnumerable<AttendanceResponse>> HandleAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var invites = await _inviteRepository.GetByEventIdAsync(eventId, cancellationToken);
        var rsvps = await _rsvpRepository.GetByEventIdAsync(eventId, cancellationToken);

        var result = new List<AttendanceResponse>();
        // Track invited names to avoid duplicates in the standalone-RSVP pass below
        var invitedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var invite in invites)
        {
            if (!string.IsNullOrWhiteSpace(invite.Name))
                invitedNames.Add(invite.Name);

            string? response = null;
            DateTime? proposedTime = null;

            if (invite.Status == InviteStatus.Accepted
                || invite.Status == InviteStatus.Declined
                || invite.Status == InviteStatus.Maybe)
            {
                var rsvp = rsvps.FirstOrDefault(r =>
                    string.Equals(r.Name, invite.Name, StringComparison.OrdinalIgnoreCase));
                if (rsvp != null)
                {
                    response = rsvp.Status.ToString();
                    proposedTime = rsvp.ProposedTime;
                }
            }

            result.Add(new AttendanceResponse
            {
                Id = invite.Id,
                Name = invite.Name,
                Status = invite.Status.ToString(),
                Response = response,
                ProposedTime = proposedTime,
                Source = "invite",
                CreatedAt = invite.CreatedAt
            });
        }

        // Include RSVPs from people who were not sent a tracked invite
        foreach (var rsvp in rsvps)
        {
            if (!invitedNames.Contains(rsvp.Name))
            {
                result.Add(new AttendanceResponse
                {
                    Id = rsvp.Id,
                    Name = rsvp.Name,
                    Status = ToAttendanceStatus(rsvp.Status),
                    Response = rsvp.Status.ToString(),
                    ProposedTime = rsvp.ProposedTime,
                    Source = "rsvp",
                    CreatedAt = rsvp.CreatedAt
                });
            }
        }

        return result.OrderByDescending(r => r.CreatedAt);
    }

    /// <summary>
    /// Maps a walk-in RSVP response onto the same lifecycle labels used for invites.
    /// </summary>
    private static string ToAttendanceStatus(RsvpStatus status) => status switch
    {
        RsvpStatus.Yes => InviteStatus.Accepted.ToString(),
        RsvpStatus.No => InviteStatus.Declined.ToString(),
        RsvpStatus.Maybe => InviteStatus.Maybe.ToString(),
        _ => InviteStatus.Opened.ToString()
    };
}
