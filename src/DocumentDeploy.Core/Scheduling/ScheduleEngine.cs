using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.Scheduling;

/// <summary>
/// Pure scheduling logic - no file I/O, no reads of the system clock. Every rule is driven
/// entirely by the `now` and data passed in, which makes it fully unit-testable without a
/// Windows machine or a real clock.
/// </summary>
public static class ScheduleEngine
{
    public static ScheduleSnapshot Evaluate(
        DateTime now,
        IReadOnlyList<AgendaItem> allAgendaItems,
        AppSettings settings,
        IReadOnlyList<SessionTemplate>? sessionTemplates = null)
    {
        sessionTemplates ??= Array.Empty<SessionTemplate>();

        var today = DateOnly.FromDateTime(now);
        var nowTime = TimeOnly.FromDateTime(now);

        var todayAgenda = allAgendaItems
            .Where(i => i.Date == today)
            .OrderBy(i => i.Start)
            .ToList();

        var current = todayAgenda.FirstOrDefault(i => i.Start <= nowTime && nowTime < i.End);
        var next = todayAgenda.FirstOrDefault(i => i.Start > nowTime);
        var isBusy = current is not null && BlocksPopups(current.Kind);

        var itemsToPrep = new List<DocumentNeed>();
        if (!isBusy && next is not null)
        {
            var minutesUntilNext = (next.Start.ToTimeSpan() - nowTime.ToTimeSpan()).TotalMinutes;
            if (minutesUntilNext <= settings.PrepLeadTimeMinutes)
            {
                itemsToPrep.AddRange(next.DocumentNeeds.Where(n => n.Return is null));
            }
        }

        var outstanding = GetOutstandingReturns(allAgendaItems, now);
        var pendingCompletion = GetPendingCompletionAnswers(allAgendaItems, sessionTemplates, now);

        var shouldShowMorningBrief =
            todayAgenda.Count > 0 &&
            nowTime >= settings.MorningBriefTime &&
            settings.LastMorningBriefShownDate != today;

        var shouldShowPlanningReminder =
            now.DayOfWeek == settings.PlanningReminderDay &&
            nowTime >= settings.PlanningReminderTime &&
            settings.LastPlanningReminderShownDate != today &&
            !isBusy;

        return new ScheduleSnapshot
        {
            Now = now,
            IsBusy = isBusy,
            TodayAgenda = todayAgenda,
            CurrentItem = current,
            NextItem = next,
            ItemsToPrepNow = itemsToPrep,
            OutstandingReturns = outstanding,
            PendingCompletionAnswers = pendingCompletion,
            ShouldShowMorningBriefNow = shouldShowMorningBrief,
            ShouldShowPlanningReminderNow = shouldShowPlanningReminder,
        };
    }

    /// <summary>Lessons, duties, and meetings are never interrupted; personal time and "other" are fair game.</summary>
    public static bool BlocksPopups(SlotKind kind) =>
        kind is SlotKind.Lesson or SlotKind.Duty or SlotKind.Meeting;

    /// <summary>Every unreturned document need across all dates (past and future), earliest deadline first.</summary>
    public static IReadOnlyList<OutstandingDocumentNeed> GetOutstandingReturns(
        IReadOnlyList<AgendaItem> allAgendaItems, DateTime now)
    {
        var result = new List<OutstandingDocumentNeed>();
        foreach (var item in allAgendaItems)
        {
            foreach (var need in item.DocumentNeeds)
            {
                if (!need.NeedsReturn || need.Return is not null) continue;
                var deadline = ResolveDeadline(item, need, allAgendaItems);
                result.Add(new OutstandingDocumentNeed(item, need, deadline, deadline < now));
            }
        }
        return result.OrderBy(o => o.ResolvedDeadline).ToList();
    }

    /// <summary>
    /// Items whose end time has passed but still have unanswered "after completion" questions
    /// from their session template. Persists exactly like outstanding returns - it keeps
    /// showing up until answered, however many ticks or days that takes.
    /// </summary>
    public static IReadOnlyList<PendingCompletionAnswer> GetPendingCompletionAnswers(
        IReadOnlyList<AgendaItem> allAgendaItems, IReadOnlyList<SessionTemplate> sessionTemplates, DateTime now)
    {
        var result = new List<PendingCompletionAnswer>();
        foreach (var item in allAgendaItems)
        {
            if (item.SessionTemplateId is not { } templateId) continue;
            if (item.Date.ToDateTime(item.End) > now) continue; // hasn't happened yet

            var template = sessionTemplates.FirstOrDefault(t => t.Id == templateId);
            if (template is null) continue;

            var unanswered = template.NoteFields
                .Where(f => f.AskAt == PromptTiming.Completion)
                .Where(f => !item.FieldValues.TryGetValue(f.Id, out var v) || string.IsNullOrWhiteSpace(v))
                .ToList();

            if (unanswered.Count > 0)
                result.Add(new PendingCompletionAnswer(item, unanswered));
        }
        return result.OrderBy(p => p.Item.Date).ThenBy(p => p.Item.Start).ToList();
    }

    public static DateTime ResolveDeadline(AgendaItem item, DocumentNeed need, IReadOnlyList<AgendaItem> allAgendaItems) =>
        need.ReturnDeadline switch
        {
            ReturnDeadlineKind.EndOfSlot => item.Date.ToDateTime(item.End),
            ReturnDeadlineKind.EndOfDay => item.Date.ToDateTime(new TimeOnly(23, 59, 59)),
            ReturnDeadlineKind.NextOccurrence => ResolveNextOccurrenceDeadline(item, allAgendaItems),
            ReturnDeadlineKind.Custom => need.CustomReturnDeadline ?? item.Date.ToDateTime(item.End),
            _ => item.Date.ToDateTime(item.End),
        };

    private static DateTime ResolveNextOccurrenceDeadline(AgendaItem item, IReadOnlyList<AgendaItem> allAgendaItems)
    {
        if (item.SourceRecurringSlotId is not { } slotId)
            return item.Date.ToDateTime(item.End);

        var next = allAgendaItems
            .Where(i => i.SourceRecurringSlotId == slotId)
            .Where(i => i.Date > item.Date || (i.Date == item.Date && i.Start > item.Start))
            .OrderBy(i => i.Date).ThenBy(i => i.Start)
            .FirstOrDefault();

        // Next occurrence not planned yet (e.g. next week hasn't been generated) - fall back to
        // end of this slot so the item isn't silently treated as having no deadline at all.
        return next is not null ? next.Date.ToDateTime(next.Start) : item.Date.ToDateTime(item.End);
    }
}
