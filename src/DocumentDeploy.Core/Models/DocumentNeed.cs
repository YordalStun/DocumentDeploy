namespace DocumentDeploy.Core.Models;

/// <summary>
/// One document need attached to a specific AgendaItem. Fields are copied from the
/// DocumentTemplate at the point the item was generated/added, so later template edits never
/// silently rewrite what was already planned.
/// </summary>
public sealed class DocumentNeed
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public bool NeedsReturn { get; set; }
    public string? ReturnDestinationPath { get; set; }
    public ReturnDeadlineKind ReturnDeadline { get; set; } = ReturnDeadlineKind.EndOfSlot;

    /// <summary>Local wall-clock deadline, only used when ReturnDeadline is Custom.</summary>
    public DateTime? CustomReturnDeadline { get; set; }
    public string? Notes { get; set; }

    /// <summary>Set once the document has been filed away and confirmed. Null while still outstanding.</summary>
    public ReturnRecord? Return { get; set; }
}
