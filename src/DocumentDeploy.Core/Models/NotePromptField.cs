namespace DocumentDeploy.Core.Models;

/// <summary>
/// A custom question a SessionTemplate asks each time it's used, e.g. "Today's sound",
/// "Three words" for a recurring phonics lesson. Answers are captured per AgendaItem in
/// AgendaItem.FieldValues, keyed by this field's Id.
/// </summary>
public sealed class NotePromptField
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public bool Multiline { get; set; }
}
