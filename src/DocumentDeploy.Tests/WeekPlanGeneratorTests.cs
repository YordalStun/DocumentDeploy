using DocumentDeploy.Core.Models;
using DocumentDeploy.Core.Planning;

namespace DocumentDeploy.Tests;

public class WeekPlanGeneratorTests
{
    [Fact]
    public void Generates_one_agenda_item_per_matching_weekday_in_the_target_week()
    {
        var slot = new RecurringSlot
        {
            Day = DayOfWeek.Wednesday,
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(10, 0),
            Title = "Maths",
            Kind = SlotKind.Lesson,
        };

        var weekStart = new DateOnly(2026, 9, 1); // Tuesday - any day in the target week works
        var created = WeekPlanGenerator.GenerateWeek(weekStart, new[] { slot }, new List<AgendaItem>(), new List<DocumentTemplate>(), new List<SessionTemplate>());

        var item = Assert.Single(created);
        Assert.Equal(new DateOnly(2026, 9, 2), item.Date); // the Wednesday of that week
        Assert.Equal(slot.Id, item.SourceRecurringSlotId);
    }

    [Fact]
    public void Is_idempotent_and_never_duplicates_an_already_generated_item()
    {
        var slot = new RecurringSlot { Day = DayOfWeek.Monday, Start = new TimeOnly(9, 0), End = new TimeOnly(9, 30), Title = "Registration" };
        var weekStart = new DateOnly(2026, 9, 1);

        var firstPass = WeekPlanGenerator.GenerateWeek(weekStart, new[] { slot }, new List<AgendaItem>(), new List<DocumentTemplate>(), new List<SessionTemplate>());
        var secondPass = WeekPlanGenerator.GenerateWeek(weekStart, new[] { slot }, firstPass, new List<DocumentTemplate>(), new List<SessionTemplate>());

        Assert.Single(firstPass);
        Assert.Empty(secondPass);
    }

    [Fact]
    public void Inactive_slots_are_not_generated()
    {
        var slot = new RecurringSlot { Day = DayOfWeek.Monday, Start = new TimeOnly(9, 0), End = new TimeOnly(9, 30), Title = "Retired", Active = false };
        var created = WeekPlanGenerator.GenerateWeek(new DateOnly(2026, 9, 1), new[] { slot }, new List<AgendaItem>(), new List<DocumentTemplate>(), new List<SessionTemplate>());

        Assert.Empty(created);
    }

    [Fact]
    public void Document_needs_are_copied_from_templates_attached_directly_to_the_slot()
    {
        var template = new DocumentTemplate { Name = "Register", NeedsReturn = false, SourcePath = @"\\server\registers" };
        var slot = new RecurringSlot { Day = DayOfWeek.Monday, Start = new TimeOnly(9, 0), End = new TimeOnly(9, 30), Title = "Form time" };
        slot.DocumentTemplateIds.Add(template.Id);

        var created = WeekPlanGenerator.GenerateWeek(new DateOnly(2026, 9, 1), new[] { slot }, new List<AgendaItem>(), new[] { template }, new List<SessionTemplate>());

        var need = Assert.Single(Assert.Single(created).DocumentNeeds);
        Assert.Equal("Register", need.Name);
        Assert.Equal(@"\\server\registers", need.SourcePath);
        Assert.Equal(template.Id, need.TemplateId);
    }

    [Fact]
    public void Session_template_contributes_documents_note_fields_and_default_notes()
    {
        var doc = new DocumentTemplate { Name = "Phonics tracker", NeedsReturn = true, ReturnDestinationPath = @"C:\Tracking" };
        var soundField = new NotePromptField { Label = "Today's sound" };
        var wordsField = new NotePromptField { Label = "Three words" };
        var sessionTemplate = new SessionTemplate
        {
            Name = "Phonics Lesson",
            DefaultNotes = "Bring the sound mat",
        };
        sessionTemplate.DocumentTemplateIds.Add(doc.Id);
        sessionTemplate.NoteFields.Add(soundField);
        sessionTemplate.NoteFields.Add(wordsField);

        var slot = new RecurringSlot
        {
            Day = DayOfWeek.Thursday,
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(9, 30),
            Title = "Phonics",
            SessionTemplateId = sessionTemplate.Id,
        };

        var created = WeekPlanGenerator.GenerateWeek(new DateOnly(2026, 9, 1), new[] { slot }, new List<AgendaItem>(), new[] { doc }, new[] { sessionTemplate });
        var item = Assert.Single(created);

        Assert.Single(item.DocumentNeeds);
        Assert.Equal("Bring the sound mat", item.Notes);
        Assert.Equal(2, item.FieldValues.Count);
        Assert.True(item.FieldValues.ContainsKey(soundField.Id));
        Assert.True(item.FieldValues.ContainsKey(wordsField.Id));
    }

    [Fact]
    public void Slot_notes_take_precedence_over_the_session_templates_default_notes()
    {
        var sessionTemplate = new SessionTemplate { Name = "Duty", DefaultNotes = "Generic duty note" };
        var slot = new RecurringSlot
        {
            Day = DayOfWeek.Monday,
            Start = new TimeOnly(12, 0),
            End = new TimeOnly(12, 30),
            Title = "Lunch duty",
            Kind = SlotKind.Duty,
            Notes = "West gate, playground",
            SessionTemplateId = sessionTemplate.Id,
        };

        var created = WeekPlanGenerator.GenerateWeek(new DateOnly(2026, 9, 1), new[] { slot }, new List<AgendaItem>(), new List<DocumentTemplate>(), new[] { sessionTemplate });

        Assert.Equal("West gate, playground", Assert.Single(created).Notes);
    }
}
