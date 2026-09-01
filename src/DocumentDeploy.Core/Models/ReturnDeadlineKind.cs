namespace DocumentDeploy.Core.Models;

/// <summary>
/// How the "must be back by" moment for a returned document is worked out relative to the
/// agenda item it belongs to.
/// </summary>
public enum ReturnDeadlineKind
{
    /// <summary>Due the moment the slot/meeting ends.</summary>
    EndOfSlot,

    /// <summary>Due by the end of the school day it belongs to.</summary>
    EndOfDay,

    /// <summary>Due before the next time this recurring slot happens (e.g. next week's lesson).</summary>
    NextOccurrence,

    /// <summary>Due at an explicit date/time set on the document need itself.</summary>
    Custom,
}
