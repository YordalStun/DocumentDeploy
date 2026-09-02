using DocumentDeploy.Core.Models;
using DocumentDeploy.Core.Scheduling;

namespace DocumentDeploy.Tests;

public class ScheduleEngineTests
{
    private static AgendaItem Item(DateOnly date, TimeOnly start, TimeOnly end, SlotKind kind, string title = "Item") =>
        new() { Date = date, Start = start, End = end, Kind = kind, Title = title };

    [Theory]
    [InlineData(SlotKind.Lesson, true)]
    [InlineData(SlotKind.Duty, true)]
    [InlineData(SlotKind.Meeting, true)]
    [InlineData(SlotKind.PersonalTime, false)]
    [InlineData(SlotKind.Other, false)]
    public void IsBusy_reflects_whether_the_current_slot_kind_blocks_popups(SlotKind kind, bool expectedBusy)
    {
        var date = new DateOnly(2026, 9, 1); // a Tuesday
        var now = date.ToDateTime(new TimeOnly(10, 15));
        var agenda = new List<AgendaItem> { Item(date, new TimeOnly(10, 0), new TimeOnly(10, 30), kind) };

        var snapshot = ScheduleEngine.Evaluate(now, agenda, new AppSettings());

        Assert.Equal(expectedBusy, snapshot.IsBusy);
        Assert.Equal(!expectedBusy, snapshot.PopupsAllowedNow);
    }

    [Fact]
    public void No_current_item_means_not_busy()
    {
        var date = new DateOnly(2026, 9, 1);
        var now = date.ToDateTime(new TimeOnly(7, 0)); // before school
        var agenda = new List<AgendaItem> { Item(date, new TimeOnly(9, 0), new TimeOnly(10, 0), SlotKind.Lesson) };

        var snapshot = ScheduleEngine.Evaluate(now, agenda, new AppSettings());

        Assert.False(snapshot.IsBusy);
        Assert.Null(snapshot.CurrentItem);
        Assert.NotNull(snapshot.NextItem);
    }

    [Fact]
    public void At_8am_the_whole_day_is_visible_in_TodayAgenda()
    {
        var date = new DateOnly(2026, 9, 1);
        var now = date.ToDateTime(new TimeOnly(8, 0));
        var agenda = new List<AgendaItem>
        {
            Item(date, new TimeOnly(9, 0), new TimeOnly(10, 0), SlotKind.Lesson, "Period 1"),
            Item(date, new TimeOnly(13, 0), new TimeOnly(14, 0), SlotKind.Lesson, "Period 5"),
        };

        var snapshot = ScheduleEngine.Evaluate(now, agenda, new AppSettings());

        Assert.Equal(2, snapshot.TodayAgenda.Count);
        Assert.Null(snapshot.CurrentItem);
    }

    [Fact]
    public void At_1pm_the_current_lesson_is_identified()
    {
        var date = new DateOnly(2026, 9, 1);
        var now = date.ToDateTime(new TimeOnly(13, 15));
        var agenda = new List<AgendaItem>
        {
            Item(date, new TimeOnly(9, 0), new TimeOnly(10, 0), SlotKind.Lesson, "Period 1"),
            Item(date, new TimeOnly(13, 0), new TimeOnly(14, 0), SlotKind.Lesson, "Period 5"),
        };

        var snapshot = ScheduleEngine.Evaluate(now, agenda, new AppSettings());

        Assert.NotNull(snapshot.CurrentItem);
        Assert.Equal("Period 5", snapshot.CurrentItem!.Title);
        Assert.True(snapshot.IsBusy);
    }

    [Fact]
    public void Prep_items_surface_only_within_the_lead_time_and_outside_a_busy_slot()
    {
        var date = new DateOnly(2026, 9, 1);
        var settings = new AppSettings { PrepLeadTimeMinutes = 10 };
        var need = new DocumentNeed { Name = "Register" };
        var next = Item(date, new TimeOnly(10, 0), new TimeOnly(10, 30), SlotKind.Lesson, "Period 2");
        next.DocumentNeeds.Add(need);
        var agenda = new List<AgendaItem> { next };

        var tooEarly = ScheduleEngine.Evaluate(date.ToDateTime(new TimeOnly(9, 30)), agenda, settings);
        Assert.Empty(tooEarly.ItemsToPrepNow);

        var withinLeadTime = ScheduleEngine.Evaluate(date.ToDateTime(new TimeOnly(9, 55)), agenda, settings);
        Assert.Single(withinLeadTime.ItemsToPrepNow);
    }

    [Fact]
    public void Prep_items_are_suppressed_while_busy_even_within_lead_time()
    {
        var date = new DateOnly(2026, 9, 1);
        var settings = new AppSettings { PrepLeadTimeMinutes = 60 };
        var current = Item(date, new TimeOnly(9, 0), new TimeOnly(10, 0), SlotKind.Lesson, "Period 1");
        var next = Item(date, new TimeOnly(10, 0), new TimeOnly(10, 30), SlotKind.Lesson, "Period 2");
        next.DocumentNeeds.Add(new DocumentNeed { Name = "Register" });
        var agenda = new List<AgendaItem> { current, next };

        var snapshot = ScheduleEngine.Evaluate(date.ToDateTime(new TimeOnly(9, 55)), agenda, settings);

        Assert.True(snapshot.IsBusy);
        Assert.Empty(snapshot.ItemsToPrepNow);
    }

    [Fact]
    public void Morning_brief_fires_once_after_the_configured_time_on_a_day_with_agenda_items()
    {
        var date = new DateOnly(2026, 9, 1);
        var settings = new AppSettings { MorningBriefTime = new TimeOnly(7, 30) };
        var agenda = new List<AgendaItem> { Item(date, new TimeOnly(9, 0), new TimeOnly(10, 0), SlotKind.Lesson) };

        var beforeTime = ScheduleEngine.Evaluate(date.ToDateTime(new TimeOnly(7, 0)), agenda, settings);
        Assert.False(beforeTime.ShouldShowMorningBriefNow);

        var afterTime = ScheduleEngine.Evaluate(date.ToDateTime(new TimeOnly(7, 45)), agenda, settings);
        Assert.True(afterTime.ShouldShowMorningBriefNow);

        settings.LastMorningBriefShownDate = date;
        var alreadyShown = ScheduleEngine.Evaluate(date.ToDateTime(new TimeOnly(8, 30)), agenda, settings);
        Assert.False(alreadyShown.ShouldShowMorningBriefNow);
    }

    [Fact]
    public void Morning_brief_never_fires_on_a_day_with_no_agenda_items()
    {
        var date = new DateOnly(2026, 9, 5); // Saturday, nothing planned
        var settings = new AppSettings { MorningBriefTime = new TimeOnly(7, 30) };

        var snapshot = ScheduleEngine.Evaluate(date.ToDateTime(new TimeOnly(9, 0)), new List<AgendaItem>(), settings);

        Assert.False(snapshot.ShouldShowMorningBriefNow);
    }

    [Fact]
    public void Planning_reminder_only_fires_on_the_configured_day_outside_busy_time()
    {
        var friday = new DateOnly(2026, 9, 4);
        var settings = new AppSettings
        {
            PlanningReminderDay = DayOfWeek.Friday,
            PlanningReminderTime = new TimeOnly(14, 30),
        };

        var duringLesson = new List<AgendaItem>
        {
            Item(friday, new TimeOnly(14, 0), new TimeOnly(15, 0), SlotKind.Lesson),
        };
        var busySnapshot = ScheduleEngine.Evaluate(friday.ToDateTime(new TimeOnly(14, 45)), duringLesson, settings);
        Assert.False(busySnapshot.ShouldShowPlanningReminderNow);

        var freeSnapshot = ScheduleEngine.Evaluate(friday.ToDateTime(new TimeOnly(14, 45)), new List<AgendaItem>(), settings);
        Assert.True(freeSnapshot.ShouldShowPlanningReminderNow);

        var monday = new DateOnly(2026, 9, 7);
        var wrongDay = ScheduleEngine.Evaluate(monday.ToDateTime(new TimeOnly(14, 45)), new List<AgendaItem>(), settings);
        Assert.False(wrongDay.ShouldShowPlanningReminderNow);
    }

    [Fact]
    public void EndOfSlot_deadline_becomes_overdue_once_the_slot_has_passed()
    {
        var date = new DateOnly(2026, 9, 1);
        var item = Item(date, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Meeting);
        item.DocumentNeeds.Add(new DocumentNeed { Name = "Consent form", NeedsReturn = true, ReturnDeadline = ReturnDeadlineKind.EndOfSlot });
        var agenda = new List<AgendaItem> { item };

        var stillOpen = ScheduleEngine.GetOutstandingReturns(agenda, date.ToDateTime(new TimeOnly(9, 15)));
        Assert.False(stillOpen.Single().IsOverdue);

        var overdue = ScheduleEngine.GetOutstandingReturns(agenda, date.ToDateTime(new TimeOnly(9, 45)));
        Assert.True(overdue.Single().IsOverdue);
    }

    [Fact]
    public void Returned_documents_never_show_up_as_outstanding()
    {
        var date = new DateOnly(2026, 9, 1);
        var item = Item(date, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Meeting);
        item.DocumentNeeds.Add(new DocumentNeed
        {
            Name = "Consent form",
            NeedsReturn = true,
            Return = new ReturnRecord { ConfirmedFileName = "consent.pdf", ConfirmedFilePath = "C:\\x\\consent.pdf", ConfirmedAtUtc = DateTime.UtcNow },
        });

        var outstanding = ScheduleEngine.GetOutstandingReturns(new List<AgendaItem> { item }, date.ToDateTime(new TimeOnly(10, 0)));

        Assert.Empty(outstanding);
    }

    [Fact]
    public void NextOccurrence_deadline_resolves_to_the_next_agenda_item_from_the_same_recurring_slot()
    {
        var slotId = Guid.NewGuid();
        var monday = new DateOnly(2026, 9, 1);
        var nextMonday = monday.AddDays(7);

        var thisWeek = Item(monday, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Lesson, "Reading group");
        thisWeek.SourceRecurringSlotId = slotId;
        thisWeek.DocumentNeeds.Add(new DocumentNeed { Name = "Reading record", NeedsReturn = true, ReturnDeadline = ReturnDeadlineKind.NextOccurrence });

        var following = Item(nextMonday, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Lesson, "Reading group");
        following.SourceRecurringSlotId = slotId;

        var agenda = new List<AgendaItem> { thisWeek, following };
        var deadline = ScheduleEngine.ResolveDeadline(thisWeek, thisWeek.DocumentNeeds[0], agenda);

        Assert.Equal(nextMonday.ToDateTime(new TimeOnly(9, 0)), deadline);
    }

    [Fact]
    public void NextOccurrence_falls_back_to_end_of_slot_when_the_following_week_is_not_generated_yet()
    {
        var slotId = Guid.NewGuid();
        var monday = new DateOnly(2026, 9, 1);
        var thisWeek = Item(monday, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Lesson);
        thisWeek.SourceRecurringSlotId = slotId;
        thisWeek.DocumentNeeds.Add(new DocumentNeed { Name = "Reading record", NeedsReturn = true, ReturnDeadline = ReturnDeadlineKind.NextOccurrence });

        var deadline = ScheduleEngine.ResolveDeadline(thisWeek, thisWeek.DocumentNeeds[0], new List<AgendaItem> { thisWeek });

        Assert.Equal(monday.ToDateTime(new TimeOnly(9, 30)), deadline);
    }

    [Fact]
    public void Completion_question_is_pending_once_the_item_has_ended_and_stays_pending_until_answered()
    {
        var completionField = new NotePromptField { Label = "How did it go?", AskAt = PromptTiming.Completion };
        var template = new SessionTemplate { Name = "Phonics" };
        template.NoteFields.Add(completionField);

        var date = new DateOnly(2026, 9, 1);
        var item = Item(date, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Lesson);
        item.SessionTemplateId = template.Id;
        item.FieldValues[completionField.Id] = "";

        var templates = new List<SessionTemplate> { template };
        var agenda = new List<AgendaItem> { item };

        var beforeEnd = ScheduleEngine.GetPendingCompletionAnswers(agenda, templates, date.ToDateTime(new TimeOnly(9, 15)));
        Assert.Empty(beforeEnd);

        var afterEnd = ScheduleEngine.GetPendingCompletionAnswers(agenda, templates, date.ToDateTime(new TimeOnly(9, 45)));
        var pending = Assert.Single(afterEnd);
        Assert.Same(item, pending.Item);
        Assert.Equal(completionField.Id, Assert.Single(pending.UnansweredFields).Id);

        item.FieldValues[completionField.Id] = "Went well";
        var afterAnswering = ScheduleEngine.GetPendingCompletionAnswers(agenda, templates, date.ToDateTime(new TimeOnly(9, 45)));
        Assert.Empty(afterAnswering);
    }

    [Fact]
    public void Planning_time_questions_never_show_up_as_pending_completion_answers()
    {
        var planningField = new NotePromptField { Label = "Today's sound", AskAt = PromptTiming.Planning };
        var template = new SessionTemplate { Name = "Phonics" };
        template.NoteFields.Add(planningField);

        var date = new DateOnly(2026, 9, 1);
        var item = Item(date, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Lesson);
        item.SessionTemplateId = template.Id;
        item.FieldValues[planningField.Id] = "";

        var pending = ScheduleEngine.GetPendingCompletionAnswers(
            new List<AgendaItem> { item }, new List<SessionTemplate> { template }, date.ToDateTime(new TimeOnly(10, 0)));

        Assert.Empty(pending);
    }

    [Fact]
    public void Evaluate_includes_pending_completion_answers_in_the_snapshot()
    {
        var completionField = new NotePromptField { Label = "Notes", AskAt = PromptTiming.Completion };
        var template = new SessionTemplate { Name = "1-on-1" };
        template.NoteFields.Add(completionField);

        var date = new DateOnly(2026, 9, 1);
        var item = Item(date, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Meeting);
        item.SessionTemplateId = template.Id;

        var snapshot = ScheduleEngine.Evaluate(
            date.ToDateTime(new TimeOnly(10, 0)), new List<AgendaItem> { item }, new AppSettings(), new List<SessionTemplate> { template });

        Assert.True(snapshot.HasPendingCompletionAnswers);
        Assert.Single(snapshot.PendingCompletionAnswers);
    }

    [Fact]
    public void Evaluate_without_session_templates_still_works_and_has_no_pending_completion_answers()
    {
        var date = new DateOnly(2026, 9, 1);
        var agenda = new List<AgendaItem> { Item(date, new TimeOnly(9, 0), new TimeOnly(9, 30), SlotKind.Lesson) };

        var snapshot = ScheduleEngine.Evaluate(date.ToDateTime(new TimeOnly(10, 0)), agenda, new AppSettings());

        Assert.False(snapshot.HasPendingCompletionAnswers);
    }
}
