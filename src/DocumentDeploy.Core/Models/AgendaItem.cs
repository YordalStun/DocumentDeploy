namespace DocumentDeploy.Core.Models;

/// <summary>
/// A concrete, dated thing on the calendar - either generated from a RecurringSlot for a given
/// week, or added one-off (e.g. a specific child's 1-on-1). Carries its own document needs and
/// any notes/template answers, so editing a template later never rewrites history.
/// </summary>
public sealed class AgendaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Title { get; set; } = string.Empty;
    public SlotKind Kind { get; set; } = SlotKind.Lesson;
    public string? GroupName { get; set; }
    public Guid? SourceRecurringSlotId { get; set; }
    public Guid? SessionTemplateId { get; set; }

    /// <summary>Free-text note for this specific reminder (e.g. "bring the reading folder too").</summary>
    public string? Notes { get; set; }

    /// <summary>Answers to the SessionTemplate's NoteFields, keyed by NotePromptField.Id.</summary>
    public Dictionary<Guid, string> FieldValues { get; set; } = new();

    public List<DocumentNeed> DocumentNeeds { get; set; } = new();
}
