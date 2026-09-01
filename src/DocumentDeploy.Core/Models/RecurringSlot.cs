namespace DocumentDeploy.Core.Models;

/// <summary>
/// A block in the weekly timetable that repeats every week (a lesson, a duty, a fixed weekly
/// meeting). Used to detect "am I in a lesson right now" and to auto-populate each new week's
/// agenda during Friday planning.
/// </summary>
public sealed class RecurringSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DayOfWeek Day { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Title { get; set; } = string.Empty;
    public SlotKind Kind { get; set; } = SlotKind.Lesson;
    public string? GroupName { get; set; }

    /// <summary>Default note copied onto every generated instance (e.g. "West gate playground duty").</summary>
    public string? Notes { get; set; }

    /// <summary>Retired slots are kept for history but no longer generated into future weeks.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Documents attached directly to this slot (on top of anything the SessionTemplate brings in).</summary>
    public List<Guid> DocumentTemplateIds { get; set; } = new();

    /// <summary>Optional pickable pattern (e.g. "Phonics Lesson") that supplies default documents and note prompts.</summary>
    public Guid? SessionTemplateId { get; set; }
}
