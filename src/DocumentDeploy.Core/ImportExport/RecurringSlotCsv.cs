using System.Text;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.ImportExport;

/// <summary>
/// Export/import the weekly timetable as CSV. Documents and the session template are referenced
/// by name (so the spreadsheet stays readable) and resolved against the current libraries on
/// import; a name that doesn't match anything is reported as a warning rather than silently dropped.
/// </summary>
public static class RecurringSlotCsv
{
    private static readonly string[] Header =
        { "Id", "Day", "Start", "End", "Title", "Kind", "GroupName", "Notes", "Active", "DocumentTemplateNames", "SessionTemplateName" };

    public static string Export(
        IEnumerable<RecurringSlot> slots,
        IReadOnlyList<DocumentTemplate> documentTemplates,
        IReadOnlyList<SessionTemplate> sessionTemplates)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CsvIO.WriteRow(Header));
        foreach (var s in slots)
        {
            var docNames = string.Join(";", s.DocumentTemplateIds
                .Select(id => documentTemplates.FirstOrDefault(t => t.Id == id)?.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            var sessionName = s.SessionTemplateId is { } stid
                ? sessionTemplates.FirstOrDefault(t => t.Id == stid)?.Name
                : null;

            sb.AppendLine(CsvIO.WriteRow(new[]
            {
                s.Id.ToString(), s.Day.ToString(), s.Start.ToString("HH:mm"), s.End.ToString("HH:mm"),
                s.Title, s.Kind.ToString(), s.GroupName, s.Notes, s.Active.ToString(), docNames, sessionName,
            }));
        }
        return sb.ToString();
    }

    public static ImportResult Import(
        string csv,
        List<RecurringSlot> existing,
        IReadOnlyList<DocumentTemplate> documentTemplates,
        IReadOnlyList<SessionTemplate> sessionTemplates)
    {
        var result = new ImportResult();
        var rows = CsvIO.ParseRows(csv);

        for (var r = 1; r < rows.Count; r++)
        {
            var cols = rows[r];
            var title = cols.ElementAtOrDefault(4);
            if (string.IsNullOrWhiteSpace(title)) continue;

            if (!Enum.TryParse<DayOfWeek>(cols.ElementAtOrDefault(1), true, out var day))
            {
                result.Warnings.Add($"Row {r + 1}: unrecognised day '{cols.ElementAtOrDefault(1)}', skipped.");
                continue;
            }
            if (!TimeOnly.TryParse(cols.ElementAtOrDefault(2), out var start) ||
                !TimeOnly.TryParse(cols.ElementAtOrDefault(3), out var end))
            {
                result.Warnings.Add($"Row {r + 1}: unrecognised start/end time, skipped.");
                continue;
            }

            var id = Guid.TryParse(cols.ElementAtOrDefault(0), out var g) ? g : (Guid?)null;
            var target = id is { } gid ? existing.FirstOrDefault(s => s.Id == gid) : null;
            if (target is null)
            {
                target = new RecurringSlot { Id = id ?? Guid.NewGuid() };
                existing.Add(target);
                result.Added++;
            }
            else
            {
                result.Updated++;
            }

            target.Day = day;
            target.Start = start;
            target.End = end;
            target.Title = title;
            target.Kind = Enum.TryParse<SlotKind>(cols.ElementAtOrDefault(5), true, out var kind) ? kind : SlotKind.Lesson;
            target.GroupName = NullIfBlank(cols.ElementAtOrDefault(6));
            target.Notes = NullIfBlank(cols.ElementAtOrDefault(7));
            target.Active = !bool.TryParse(cols.ElementAtOrDefault(8), out var activeParsed) || activeParsed;

            target.DocumentTemplateIds.Clear();
            foreach (var name in SplitNames(cols.ElementAtOrDefault(9)))
            {
                var match = documentTemplates.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                if (match is not null) target.DocumentTemplateIds.Add(match.Id);
                else result.Warnings.Add($"Row {r + 1}: no document template named '{name}'.");
            }

            var sessionName = cols.ElementAtOrDefault(10);
            target.SessionTemplateId = null;
            if (!string.IsNullOrWhiteSpace(sessionName))
            {
                var match = sessionTemplates.FirstOrDefault(t => string.Equals(t.Name, sessionName, StringComparison.OrdinalIgnoreCase));
                if (match is not null) target.SessionTemplateId = match.Id;
                else result.Warnings.Add($"Row {r + 1}: no session template named '{sessionName}'.");
            }
        }

        return result;
    }

    private static IEnumerable<string> SplitNames(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Enumerable.Empty<string>()
            : raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
