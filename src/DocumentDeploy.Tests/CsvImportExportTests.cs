using DocumentDeploy.Core.ImportExport;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Tests;

public class CsvImportExportTests
{
    [Fact]
    public void DocumentTemplate_round_trips_through_export_and_import()
    {
        var original = new List<DocumentTemplate>
        {
            new()
            {
                Name = "IEP Review, signed",
                SourcePath = @"\\server\SEN\ChildX",
                NeedsReturn = true,
                ReturnDestinationPath = @"\\server\SEN\ChildX\Signed",
                ReturnDeadline = ReturnDeadlineKind.EndOfDay,
                Notes = "Contains a comma, and a \"quote\"",
            },
        };

        var csv = DocumentTemplateCsv.Export(original);
        var reimported = new List<DocumentTemplate>();
        var result = DocumentTemplateCsv.Import(csv, reimported);

        Assert.Equal(1, result.Added);
        var t = Assert.Single(reimported);
        Assert.Equal(original[0].Id, t.Id);
        Assert.Equal(original[0].Name, t.Name);
        Assert.Equal(original[0].SourcePath, t.SourcePath);
        Assert.Equal(original[0].NeedsReturn, t.NeedsReturn);
        Assert.Equal(original[0].ReturnDestinationPath, t.ReturnDestinationPath);
        Assert.Equal(original[0].ReturnDeadline, t.ReturnDeadline);
        Assert.Equal(original[0].Notes, t.Notes);
    }

    [Fact]
    public void Reimporting_an_edited_export_updates_in_place_instead_of_duplicating()
    {
        var existing = new List<DocumentTemplate> { new() { Name = "Consent form", NeedsReturn = true } };
        var csv = DocumentTemplateCsv.Export(existing).Replace("Consent form", "Consent form (updated)");

        var result = DocumentTemplateCsv.Import(csv, existing);

        Assert.Equal(0, result.Added);
        Assert.Equal(1, result.Updated);
        Assert.Single(existing);
        Assert.Equal("Consent form (updated)", existing[0].Name);
    }

    [Fact]
    public void RecurringSlot_round_trips_documents_and_session_template_by_name()
    {
        var doc = new DocumentTemplate { Name = "Reading record" };
        var sessionTemplate = new SessionTemplate { Name = "Phonics Lesson" };
        var slot = new RecurringSlot
        {
            Day = DayOfWeek.Wednesday,
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(9, 30),
            Title = "Phonics",
            Kind = SlotKind.Lesson,
            SessionTemplateId = sessionTemplate.Id,
        };
        slot.DocumentTemplateIds.Add(doc.Id);

        var csv = RecurringSlotCsv.Export(new[] { slot }, new[] { doc }, new[] { sessionTemplate });

        var reimported = new List<RecurringSlot>();
        var result = RecurringSlotCsv.Import(csv, reimported, new[] { doc }, new[] { sessionTemplate });

        Assert.Empty(result.Warnings);
        var s = Assert.Single(reimported);
        Assert.Equal(slot.Id, s.Id);
        Assert.Equal(DayOfWeek.Wednesday, s.Day);
        Assert.Equal(doc.Id, Assert.Single(s.DocumentTemplateIds));
        Assert.Equal(sessionTemplate.Id, s.SessionTemplateId);
    }

    [Fact]
    public void Unresolved_document_name_on_import_produces_a_warning_and_is_skipped()
    {
        var slot = new RecurringSlot { Day = DayOfWeek.Monday, Start = new TimeOnly(9, 0), End = new TimeOnly(9, 30), Title = "Something" };
        var csv = RecurringSlotCsv.Export(new[] { slot }, new List<DocumentTemplate>(), new List<SessionTemplate>())
            .TrimEnd('\n', '\r');
        // Manually append a row referencing a document template that doesn't exist.
        csv += "\n" + string.Join(",", new[] { "", "Tuesday", "10:00", "10:30", "Extra", "Lesson", "", "", "True", "Nonexistent Doc", "" });

        var reimported = new List<RecurringSlot>();
        var result = RecurringSlotCsv.Import(csv, reimported, new List<DocumentTemplate>(), new List<SessionTemplate>());

        Assert.Contains(result.Warnings, w => w.Contains("Nonexistent Doc"));
    }
}
