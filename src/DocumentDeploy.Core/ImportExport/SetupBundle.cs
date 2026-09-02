using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.ImportExport;

/// <summary>
/// Everything needed to set the app up the same way on another machine - the recurring
/// timetable, document and session templates, and settings. Deliberately excludes the dated
/// agenda: outstanding returns, filed documents, and answered completion questions belong to
/// wherever they actually happened and should never be silently overwritten by a setup transfer.
/// </summary>
public sealed class SetupBundle
{
    public int FormatVersion { get; set; } = 1;
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    public List<RecurringSlot> RecurringSlots { get; set; } = new();
    public List<DocumentTemplate> DocumentTemplates { get; set; } = new();
    public List<SessionTemplate> SessionTemplates { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}
