using DocumentDeploy.Core.Models;
using DocumentDeploy.Core.Storage;

namespace DocumentDeploy.App.Services;

/// <summary>
/// The app's single in-memory source of truth: loaded once at startup, saved back to disk
/// after every edit. Views mutate these lists directly and call the matching Save method -
/// no change-tracking machinery beyond that.
/// </summary>
public sealed class AppState
{
    private readonly JsonDataStore _store;

    public List<RecurringSlot> RecurringSlots { get; }
    public List<DocumentTemplate> DocumentTemplates { get; }
    public List<SessionTemplate> SessionTemplates { get; }
    public List<AgendaItem> Agenda { get; }
    public AppSettings Settings { get; }

    public AppState() : this(AppPaths.DefaultDataDirectory)
    {
    }

    public AppState(string dataDirectory)
    {
        _store = new JsonDataStore(dataDirectory);
        RecurringSlots = _store.LoadRecurringSlots();
        DocumentTemplates = _store.LoadDocumentTemplates();
        SessionTemplates = _store.LoadSessionTemplates();
        Agenda = _store.LoadAgenda();
        Settings = _store.LoadSettings();
    }

    public void SaveRecurringSlots() => _store.SaveRecurringSlots(RecurringSlots);
    public void SaveDocumentTemplates() => _store.SaveDocumentTemplates(DocumentTemplates);
    public void SaveSessionTemplates() => _store.SaveSessionTemplates(SessionTemplates);
    public void SaveAgenda() => _store.SaveAgenda(Agenda);
    public void SaveSettings() => _store.SaveSettings(Settings);
}
