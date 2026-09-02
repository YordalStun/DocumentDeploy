using DocumentDeploy.Core.ImportExport;
using DocumentDeploy.Core.Models;
using DocumentDeploy.Core.Storage;

namespace DocumentDeploy.Tests;

public class SetupBundleIOTests : IDisposable
{
    private readonly string _homeDir = Path.Combine(Path.GetTempPath(), "dd-bundle-tests-home-" + Guid.NewGuid());
    private readonly string _workDir = Path.Combine(Path.GetTempPath(), "dd-bundle-tests-work-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_homeDir)) Directory.Delete(_homeDir, recursive: true);
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    [Fact]
    public void Round_trips_recurring_slots_templates_and_settings()
    {
        var slot = new RecurringSlot
        {
            Day = DayOfWeek.Wednesday,
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(9, 30),
            Title = "Phonics",
            Kind = SlotKind.Lesson,
        };

        var doc = new DocumentTemplate { Name = "Reading record", NeedsReturn = true, ReturnDeadline = ReturnDeadlineKind.NextOccurrence };
        slot.DocumentTemplateIds.Add(doc.Id);

        var field = new NotePromptField { Label = "Today's sound", AskAt = PromptTiming.Planning };
        var sessionTemplate = new SessionTemplate { Name = "Phonics Lesson" };
        sessionTemplate.NoteFields.Add(field);
        slot.SessionTemplateId = sessionTemplate.Id;

        var settings = new AppSettings { MorningBriefTime = new TimeOnly(7, 45), LaunchAtWindowsStartup = false };

        var json = SetupBundleIO.Export(new[] { slot }, new[] { doc }, new[] { sessionTemplate }, settings);
        var bundle = SetupBundleIO.Import(json);

        var importedSlot = Assert.Single(bundle.RecurringSlots);
        Assert.Equal(slot.Id, importedSlot.Id);
        Assert.Equal(DayOfWeek.Wednesday, importedSlot.Day);
        Assert.Equal(doc.Id, Assert.Single(importedSlot.DocumentTemplateIds));
        Assert.Equal(sessionTemplate.Id, importedSlot.SessionTemplateId);

        var importedDoc = Assert.Single(bundle.DocumentTemplates);
        Assert.Equal(ReturnDeadlineKind.NextOccurrence, importedDoc.ReturnDeadline);

        var importedTemplate = Assert.Single(bundle.SessionTemplates);
        var importedField = Assert.Single(importedTemplate.NoteFields);
        Assert.Equal("Today's sound", importedField.Label);
        Assert.Equal(PromptTiming.Planning, importedField.AskAt);

        Assert.Equal(new TimeOnly(7, 45), bundle.Settings.MorningBriefTime);
        Assert.False(bundle.Settings.LaunchAtWindowsStartup);
    }

    [Fact]
    public void Import_rejects_content_that_is_not_a_valid_bundle()
    {
        Assert.ThrowsAny<Exception>(() => SetupBundleIO.Import("not json at all"));
    }

    /// <summary>
    /// Every field on every model that goes into the bundle, set to a non-default value, must
    /// survive an export/import round trip unchanged. This is the "everything, exactly" test.
    /// </summary>
    [Fact]
    public void Every_field_on_every_model_survives_the_round_trip_exactly()
    {
        var docA = new DocumentTemplate
        {
            Name = "IEP review, signed",
            SourcePath = @"\\server\SEN\ChildX",
            NeedsReturn = true,
            ReturnDestinationPath = @"\\server\SEN\ChildX\Signed",
            ReturnDeadline = ReturnDeadlineKind.EndOfDay,
            Notes = "Handle with care, comma, and \"quotes\"",
        };
        var docB = new DocumentTemplate
        {
            Name = "Register",
            SourcePath = null,
            NeedsReturn = false,
            ReturnDestinationPath = null,
            ReturnDeadline = ReturnDeadlineKind.Custom,
            Notes = null,
        };

        var planningField = new NotePromptField { Label = "Today's sound", Multiline = false, AskAt = PromptTiming.Planning };
        var completionField = new NotePromptField { Label = "How did it go", Multiline = true, AskAt = PromptTiming.Completion };
        var sessionTemplate = new SessionTemplate
        {
            Name = "Phonics Lesson",
            DefaultNotes = "Bring the sound mat",
        };
        sessionTemplate.NoteFields.Add(planningField);
        sessionTemplate.NoteFields.Add(completionField);
        sessionTemplate.DocumentTemplateIds.Add(docA.Id);

        var slotA = new RecurringSlot
        {
            Day = DayOfWeek.Thursday,
            Start = new TimeOnly(9, 15),
            End = new TimeOnly(9, 45),
            Title = "Phonics",
            Kind = SlotKind.Lesson,
            GroupName = "Year 2",
            Notes = "Carpet time first",
            Active = true,
            SessionTemplateId = sessionTemplate.Id,
        };
        slotA.DocumentTemplateIds.Add(docA.Id);
        slotA.DocumentTemplateIds.Add(docB.Id);

        var slotB = new RecurringSlot
        {
            Day = DayOfWeek.Monday,
            Start = new TimeOnly(12, 0),
            End = new TimeOnly(12, 30),
            Title = "Lunch duty",
            Kind = SlotKind.Duty,
            GroupName = null,
            Notes = null,
            Active = false,
            SessionTemplateId = null,
        };

        var settings = new AppSettings
        {
            MorningBriefTime = new TimeOnly(7, 20),
            PrepLeadTimeMinutes = 12,
            LaunchAtWindowsStartup = false,
            PlanningReminderDay = DayOfWeek.Thursday,
            PlanningReminderTime = new TimeOnly(15, 10),
            LastMorningBriefShownDate = new DateOnly(2026, 9, 1),
            LastPlanningReminderShownDate = new DateOnly(2026, 8, 28),
        };

        var json = SetupBundleIO.Export(
            new[] { slotA, slotB }, new[] { docA, docB }, new[] { sessionTemplate }, settings);
        var bundle = SetupBundleIO.Import(json);

        // --- DocumentTemplates ---
        Assert.Equal(2, bundle.DocumentTemplates.Count);
        var importedDocA = bundle.DocumentTemplates.Single(d => d.Id == docA.Id);
        Assert.Equal(docA.Name, importedDocA.Name);
        Assert.Equal(docA.SourcePath, importedDocA.SourcePath);
        Assert.Equal(docA.NeedsReturn, importedDocA.NeedsReturn);
        Assert.Equal(docA.ReturnDestinationPath, importedDocA.ReturnDestinationPath);
        Assert.Equal(docA.ReturnDeadline, importedDocA.ReturnDeadline);
        Assert.Equal(docA.Notes, importedDocA.Notes);

        var importedDocB = bundle.DocumentTemplates.Single(d => d.Id == docB.Id);
        Assert.Equal(docB.Name, importedDocB.Name);
        Assert.Null(importedDocB.SourcePath);
        Assert.False(importedDocB.NeedsReturn);
        Assert.Null(importedDocB.ReturnDestinationPath);
        Assert.Equal(ReturnDeadlineKind.Custom, importedDocB.ReturnDeadline);
        Assert.Null(importedDocB.Notes);

        // --- SessionTemplates + NotePromptFields ---
        var importedTemplate = Assert.Single(bundle.SessionTemplates);
        Assert.Equal(sessionTemplate.Id, importedTemplate.Id);
        Assert.Equal(sessionTemplate.Name, importedTemplate.Name);
        Assert.Equal(sessionTemplate.DefaultNotes, importedTemplate.DefaultNotes);
        Assert.Equal(docA.Id, Assert.Single(importedTemplate.DocumentTemplateIds));

        Assert.Equal(2, importedTemplate.NoteFields.Count);
        var importedPlanningField = importedTemplate.NoteFields.Single(f => f.Id == planningField.Id);
        Assert.Equal("Today's sound", importedPlanningField.Label);
        Assert.False(importedPlanningField.Multiline);
        Assert.Equal(PromptTiming.Planning, importedPlanningField.AskAt);

        var importedCompletionField = importedTemplate.NoteFields.Single(f => f.Id == completionField.Id);
        Assert.Equal("How did it go", importedCompletionField.Label);
        Assert.True(importedCompletionField.Multiline);
        Assert.Equal(PromptTiming.Completion, importedCompletionField.AskAt);

        // --- RecurringSlots ---
        Assert.Equal(2, bundle.RecurringSlots.Count);
        var importedSlotA = bundle.RecurringSlots.Single(s => s.Id == slotA.Id);
        Assert.Equal(slotA.Day, importedSlotA.Day);
        Assert.Equal(slotA.Start, importedSlotA.Start);
        Assert.Equal(slotA.End, importedSlotA.End);
        Assert.Equal(slotA.Title, importedSlotA.Title);
        Assert.Equal(slotA.Kind, importedSlotA.Kind);
        Assert.Equal(slotA.GroupName, importedSlotA.GroupName);
        Assert.Equal(slotA.Notes, importedSlotA.Notes);
        Assert.True(importedSlotA.Active);
        Assert.Equal(slotA.SessionTemplateId, importedSlotA.SessionTemplateId);
        Assert.Equal(new[] { docA.Id, docB.Id }, importedSlotA.DocumentTemplateIds);

        var importedSlotB = bundle.RecurringSlots.Single(s => s.Id == slotB.Id);
        Assert.Equal(SlotKind.Duty, importedSlotB.Kind);
        Assert.Null(importedSlotB.GroupName);
        Assert.Null(importedSlotB.Notes);
        Assert.False(importedSlotB.Active);
        Assert.Null(importedSlotB.SessionTemplateId);
        Assert.Empty(importedSlotB.DocumentTemplateIds);

        // --- AppSettings ---
        Assert.Equal(settings.MorningBriefTime, bundle.Settings.MorningBriefTime);
        Assert.Equal(settings.PrepLeadTimeMinutes, bundle.Settings.PrepLeadTimeMinutes);
        Assert.Equal(settings.LaunchAtWindowsStartup, bundle.Settings.LaunchAtWindowsStartup);
        Assert.Equal(settings.PlanningReminderDay, bundle.Settings.PlanningReminderDay);
        Assert.Equal(settings.PlanningReminderTime, bundle.Settings.PlanningReminderTime);
        Assert.Equal(settings.LastMorningBriefShownDate, bundle.Settings.LastMorningBriefShownDate);
        Assert.Equal(settings.LastPlanningReminderShownDate, bundle.Settings.LastPlanningReminderShownDate);
    }

    [Fact]
    public void Empty_state_round_trips_to_empty_collections_not_null()
    {
        var json = SetupBundleIO.Export(
            Array.Empty<RecurringSlot>(), Array.Empty<DocumentTemplate>(), Array.Empty<SessionTemplate>(), new AppSettings());
        var bundle = SetupBundleIO.Import(json);

        Assert.Empty(bundle.RecurringSlots);
        Assert.Empty(bundle.DocumentTemplates);
        Assert.Empty(bundle.SessionTemplates);
        Assert.NotNull(bundle.Settings);
    }

    [Fact]
    public void ApplyTo_replaces_lists_and_settings_but_preserves_the_targets_own_tracking_dates()
    {
        var bundle = new SetupBundle
        {
            Settings = new AppSettings { MorningBriefTime = new TimeOnly(8, 0), LastMorningBriefShownDate = new DateOnly(2026, 1, 1) },
        };
        bundle.RecurringSlots.Add(new RecurringSlot { Title = "New slot" });

        var targetSlots = new List<RecurringSlot> { new() { Title = "Old slot" } };
        var targetDocs = new List<DocumentTemplate> { new() { Name = "Old doc" } };
        var targetSessionTemplates = new List<SessionTemplate> { new() { Name = "Old template" } };
        var targetSettings = new AppSettings { MorningBriefTime = new TimeOnly(6, 0), LastMorningBriefShownDate = new DateOnly(2026, 9, 2) };

        SetupBundleIO.ApplyTo(bundle, targetSlots, targetDocs, targetSessionTemplates, targetSettings);

        Assert.Equal("New slot", Assert.Single(targetSlots).Title);
        Assert.Empty(targetDocs);
        Assert.Empty(targetSessionTemplates);
        Assert.Equal(new TimeOnly(8, 0), targetSettings.MorningBriefTime);
        // The target machine's own "already shown today" tracking must survive untouched -
        // blindly importing it from another machine could wrongly suppress today's brief here.
        Assert.Equal(new DateOnly(2026, 9, 2), targetSettings.LastMorningBriefShownDate);
    }

    /// <summary>
    /// The real pipeline end to end: build a setup and persist it like the real app does
    /// (JsonDataStore), export a bundle from what's actually on disk, apply it onto a
    /// "work PC" that already has its own state, persist that, then reload everything from a
    /// brand new JsonDataStore instance (simulating an app restart) and check every field
    /// survived - and that the work PC's pre-existing runtime tracking wasn't clobbered.
    /// </summary>
    [Fact]
    public void End_to_end_through_JsonDataStore_home_to_work_preserves_everything()
    {
        var homeStore = new JsonDataStore(_homeDir);

        var doc = new DocumentTemplate { Name = "Consent form", NeedsReturn = true, ReturnDestinationPath = @"C:\Consents" };
        homeStore.SaveDocumentTemplates(new List<DocumentTemplate> { doc });

        var field = new NotePromptField { Label = "Today's sound", AskAt = PromptTiming.Planning };
        var sessionTemplate = new SessionTemplate { Name = "Phonics" };
        sessionTemplate.NoteFields.Add(field);
        sessionTemplate.DocumentTemplateIds.Add(doc.Id);
        homeStore.SaveSessionTemplates(new List<SessionTemplate> { sessionTemplate });

        var slot = new RecurringSlot
        {
            Day = DayOfWeek.Tuesday,
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(9, 30),
            Title = "Phonics",
            SessionTemplateId = sessionTemplate.Id,
        };
        slot.DocumentTemplateIds.Add(doc.Id);
        homeStore.SaveRecurringSlots(new List<RecurringSlot> { slot });

        var homeSettings = new AppSettings { MorningBriefTime = new TimeOnly(7, 15), LastMorningBriefShownDate = new DateOnly(2026, 9, 1) };
        homeStore.SaveSettings(homeSettings);

        // Export from what's actually persisted on disk (a fresh store instance, not the
        // in-memory objects above) - this is exactly what the real Export button does.
        var freshHomeStore = new JsonDataStore(_homeDir);
        var json = SetupBundleIO.Export(
            freshHomeStore.LoadRecurringSlots(), freshHomeStore.LoadDocumentTemplates(),
            freshHomeStore.LoadSessionTemplates(), freshHomeStore.LoadSettings());

        // "Work PC" already has its own state before importing.
        var workStore = new JsonDataStore(_workDir);
        var workRecurringSlots = new List<RecurringSlot> { new() { Day = DayOfWeek.Friday, Title = "Old slot to be replaced" } };
        var workDocumentTemplates = new List<DocumentTemplate>();
        var workSessionTemplates = new List<SessionTemplate>();
        var workSettings = new AppSettings { LastMorningBriefShownDate = new DateOnly(2026, 9, 2) };

        var bundle = SetupBundleIO.Import(json);
        SetupBundleIO.ApplyTo(bundle, workRecurringSlots, workDocumentTemplates, workSessionTemplates, workSettings);

        workStore.SaveRecurringSlots(workRecurringSlots);
        workStore.SaveDocumentTemplates(workDocumentTemplates);
        workStore.SaveSessionTemplates(workSessionTemplates);
        workStore.SaveSettings(workSettings);

        // Simulate restarting the app on the work PC: reload everything from a brand new store.
        var reloaded = new JsonDataStore(_workDir);
        var reloadedSlot = Assert.Single(reloaded.LoadRecurringSlots());
        Assert.Equal(slot.Id, reloadedSlot.Id);
        Assert.Equal("Phonics", reloadedSlot.Title);
        Assert.Equal(doc.Id, Assert.Single(reloadedSlot.DocumentTemplateIds));
        Assert.Equal(sessionTemplate.Id, reloadedSlot.SessionTemplateId);

        var reloadedDoc = Assert.Single(reloaded.LoadDocumentTemplates());
        Assert.Equal("Consent form", reloadedDoc.Name);
        Assert.Equal(@"C:\Consents", reloadedDoc.ReturnDestinationPath);

        var reloadedTemplate = Assert.Single(reloaded.LoadSessionTemplates());
        var reloadedField = Assert.Single(reloadedTemplate.NoteFields);
        Assert.Equal("Today's sound", reloadedField.Label);
        Assert.Equal(PromptTiming.Planning, reloadedField.AskAt);

        var reloadedSettings = reloaded.LoadSettings();
        Assert.Equal(new TimeOnly(7, 15), reloadedSettings.MorningBriefTime);
        Assert.Equal(new DateOnly(2026, 9, 2), reloadedSettings.LastMorningBriefShownDate);
    }
}
