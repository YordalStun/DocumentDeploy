using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.Scheduling;

/// <summary>An item that has already happened but still has unanswered "after completion"
/// questions from its session template.</summary>
public sealed record PendingCompletionAnswer(AgendaItem Item, IReadOnlyList<NotePromptField> UnansweredFields);
