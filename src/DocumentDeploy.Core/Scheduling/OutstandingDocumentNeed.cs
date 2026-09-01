using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.Scheduling;

/// <summary>
/// A document need that still hasn't been filed back, paired with its resolved deadline.
/// Deadline is in local wall-clock time, matching AgendaItem's Date/Start/End - this app only
/// ever runs on one machine in one timezone, so there's no UTC conversion in play here.
/// </summary>
public sealed record OutstandingDocumentNeed(
    AgendaItem Item,
    DocumentNeed Need,
    DateTime ResolvedDeadline,
    bool IsOverdue);
