namespace DocumentDeploy.Core.Models;

/// <summary>What kind of thing a RecurringSlot/AgendaItem represents, and in turn whether it's
/// safe for the app to pop up over it.</summary>
public enum SlotKind
{
    /// <summary>Teaching time. Never interrupted.</summary>
    Lesson,

    /// <summary>Break/lunchtime/afternoon duty - you're supervising, not free. Never interrupted.</summary>
    Duty,

    /// <summary>A meeting, 1-on-1, or similar. Never interrupted while it's on.</summary>
    Meeting,

    /// <summary>Your own break/lunch/free time. Safe to pop up - a good moment for reminders.</summary>
    PersonalTime,

    /// <summary>Anything else that doesn't need to block popups.</summary>
    Other,
}
