using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.Scheduling;

/// <summary>
/// A pure, deterministic "what does the world look like right now" view, computed fresh on
/// every timer tick from the current data + wall-clock time. The WPF layer decides what to do
/// with it (which popups to actually show); this type only ever describes state, never causes
/// side effects itself.
/// </summary>
public sealed class ScheduleSnapshot
{
    public required DateTime Now { get; init; }

    /// <summary>True when the current item is a Lesson, Duty, or Meeting - anything popups must never interrupt.</summary>
    public required bool IsBusy { get; init; }

    public required IReadOnlyList<AgendaItem> TodayAgenda { get; init; }
    public AgendaItem? CurrentItem { get; init; }
    public AgendaItem? NextItem { get; init; }

    /// <summary>Next item's document needs, once it's within the configured prep lead time and no lesson is running.</summary>
    public required IReadOnlyList<DocumentNeed> ItemsToPrepNow { get; init; }

    /// <summary>Every unreturned document need across all dates, oldest deadline first.</summary>
    public required IReadOnlyList<OutstandingDocumentNeed> OutstandingReturns { get; init; }

    public bool HasOverdueReturns => OutstandingReturns.Any(o => o.IsOverdue);
    public bool PopupsAllowedNow => !IsBusy;
    public required bool ShouldShowMorningBriefNow { get; init; }
    public required bool ShouldShowPlanningReminderNow { get; init; }
}
