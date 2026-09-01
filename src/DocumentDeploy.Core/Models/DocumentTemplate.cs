namespace DocumentDeploy.Core.Models;

/// <summary>
/// A reusable definition of a document need (e.g. "IEP review form - Child X") that can be
/// attached to a recurring timetable slot or a one-off agenda item without retyping it each time.
/// Only ever stores where a file lives/should be filed - never the file itself.
/// </summary>
public sealed class DocumentTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public bool NeedsReturn { get; set; }
    public string? ReturnDestinationPath { get; set; }
    public ReturnDeadlineKind ReturnDeadline { get; set; } = ReturnDeadlineKind.EndOfSlot;
    public string? Notes { get; set; }
}
