using System.Text;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.ImportExport;

/// <summary>
/// Export/import the document template library as CSV. Import upserts by the Id column when
/// present (so re-importing an edited export updates in place and preserves references from
/// recurring slots); a blank Id creates a new template.
/// </summary>
public static class DocumentTemplateCsv
{
    private static readonly string[] Header =
        { "Id", "Name", "SourcePath", "NeedsReturn", "ReturnDestinationPath", "ReturnDeadline", "Notes" };

    public static string Export(IEnumerable<DocumentTemplate> templates)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CsvIO.WriteRow(Header));
        foreach (var t in templates)
        {
            sb.AppendLine(CsvIO.WriteRow(new[]
            {
                t.Id.ToString(), t.Name, t.SourcePath, t.NeedsReturn.ToString(),
                t.ReturnDestinationPath, t.ReturnDeadline.ToString(), t.Notes,
            }));
        }
        return sb.ToString();
    }

    public static ImportResult Import(string csv, List<DocumentTemplate> existing)
    {
        var result = new ImportResult();
        var rows = CsvIO.ParseRows(csv);

        for (var r = 1; r < rows.Count; r++)
        {
            var cols = rows[r];
            var name = cols.ElementAtOrDefault(1);
            if (string.IsNullOrWhiteSpace(name)) continue;

            var id = Guid.TryParse(cols.ElementAtOrDefault(0), out var g) ? g : (Guid?)null;
            var target = id is { } gid ? existing.FirstOrDefault(t => t.Id == gid) : null;
            if (target is null)
            {
                target = new DocumentTemplate { Id = id ?? Guid.NewGuid() };
                existing.Add(target);
                result.Added++;
            }
            else
            {
                result.Updated++;
            }

            target.Name = name;
            target.SourcePath = NullIfBlank(cols.ElementAtOrDefault(2));
            target.NeedsReturn = bool.TryParse(cols.ElementAtOrDefault(3), out var needsReturn) && needsReturn;
            target.ReturnDestinationPath = NullIfBlank(cols.ElementAtOrDefault(4));
            target.ReturnDeadline = Enum.TryParse<ReturnDeadlineKind>(cols.ElementAtOrDefault(5), true, out var deadline)
                ? deadline
                : ReturnDeadlineKind.EndOfSlot;
            target.Notes = NullIfBlank(cols.ElementAtOrDefault(6));
        }

        return result;
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
