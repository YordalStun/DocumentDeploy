namespace DocumentDeploy.Core.Models;

/// <summary>When a SessionTemplate's custom question should actually be asked.</summary>
public enum PromptTiming
{
    /// <summary>Answered while planning the week (e.g. "today's sound" for a phonics lesson) -
    /// known in advance, decided by the teacher during planning.</summary>
    Planning,

    /// <summary>Answered after the session has happened (e.g. "how did it go", "who needs
    /// follow-up") - a reflection captured once the lesson/meeting is over.</summary>
    Completion,
}
