using System.Text;

namespace DocumentDeploy.Core.ImportExport;

/// <summary>Minimal RFC4180-ish CSV reader/writer - just enough for Excel round-tripping, no external dependency.</summary>
internal static class CsvIO
{
    public static string WriteRow(IEnumerable<string?> fields) =>
        string.Join(",", fields.Select(Escape));

    private static string Escape(string? field)
    {
        field ??= string.Empty;
        return field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + field.Replace("\"", "\"\"") + "\""
            : field;
    }

    public static List<List<string>> ParseRows(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < csv.Length)
        {
            var c = csv[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i += 2; continue; }
                    inQuotes = false; i++; continue;
                }
                field.Append(c); i++; continue;
            }

            switch (c)
            {
                case '"': inQuotes = true; i++; break;
                case ',': row.Add(field.ToString()); field.Clear(); i++; break;
                case '\r': i++; break;
                case '\n': row.Add(field.ToString()); field.Clear(); rows.Add(row); row = new List<string>(); i++; break;
                default: field.Append(c); i++; break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
