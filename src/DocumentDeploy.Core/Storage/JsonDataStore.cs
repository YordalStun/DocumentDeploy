using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.Storage;

/// <summary>
/// Local metadata storage - a handful of plain JSON files in one directory. Never stores the
/// documents themselves, only what's needed about them (names, paths, deadlines, confirmations).
/// Writes go through a temp file + move so a crash mid-write can't corrupt the real file.
/// </summary>
public sealed class JsonDataStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _directory;

    public JsonDataStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    public List<RecurringSlot> LoadRecurringSlots() => Load<List<RecurringSlot>>("recurring-slots.json") ?? new();
    public void SaveRecurringSlots(List<RecurringSlot> slots) => Save("recurring-slots.json", slots);

    public List<DocumentTemplate> LoadDocumentTemplates() => Load<List<DocumentTemplate>>("document-templates.json") ?? new();
    public void SaveDocumentTemplates(List<DocumentTemplate> templates) => Save("document-templates.json", templates);

    public List<SessionTemplate> LoadSessionTemplates() => Load<List<SessionTemplate>>("session-templates.json") ?? new();
    public void SaveSessionTemplates(List<SessionTemplate> templates) => Save("session-templates.json", templates);

    public List<AgendaItem> LoadAgenda() => Load<List<AgendaItem>>("agenda.json") ?? new();
    public void SaveAgenda(List<AgendaItem> items) => Save("agenda.json", items);

    public AppSettings LoadSettings() => Load<AppSettings>("settings.json") ?? new();
    public void SaveSettings(AppSettings settings) => Save("settings.json", settings);

    private T? Load<T>(string fileName)
    {
        var path = Path.Combine(_directory, fileName);
        if (!File.Exists(path)) return default;
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    private void Save<T>(string fileName, T data)
    {
        var path = Path.Combine(_directory, fileName);
        var tmpPath = path + ".tmp";
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, path, overwrite: true);
    }
}
