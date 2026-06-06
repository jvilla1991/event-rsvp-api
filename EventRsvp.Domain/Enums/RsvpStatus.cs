namespace EventRsvp.Domain.Enums;

/// <summary>
/// How an attendee responded to an event.
/// Values are chosen so the legacy boolean column converts cleanly
/// (false =&gt; No, true =&gt; Yes) when migrating existing data.
/// </summary>
public enum RsvpStatus
{
    No = 0,    // Not attending
    Yes = 1,   // Attending
    Maybe = 2  // Tentative / undecided
}
