namespace DocumentDeploy.Core.Models;

/// <summary>
/// A pickable, reusable pattern for a recurring type of session (e.g. "Phonics Lesson",
/// "1-on-1 SEN Review"). Bundles the documents that type of session usually needs with a
/// set of custom questions to fill in each time (e.g. "Today's sound", "Three words").
/// Attach one to a RecurringSlot so every generated week already knows what to ask for.
/// </summary>
public sealed class SessionTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Guid> DocumentTemplateIds { get; set; } = new();
    public List<NotePromptField> NoteFields { get; set; } = new();
    public string? DefaultNotes { get; set; }
}
