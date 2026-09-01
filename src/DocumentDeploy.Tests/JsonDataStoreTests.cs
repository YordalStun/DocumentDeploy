using DocumentDeploy.Core.Models;
using DocumentDeploy.Core.Storage;

namespace DocumentDeploy.Tests;

public class JsonDataStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dd-store-tests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Round_trips_every_entity_type_including_dates_times_and_enums()
    {
        var store = new JsonDataStore(_dir);

        var slot = new RecurringSlot { Day = DayOfWeek.Friday, Start = new TimeOnly(14, 30), End = new TimeOnly(15, 0), Kind = SlotKind.PersonalTime, Title = "Planning" };
        store.SaveRecurringSlots(new List<RecurringSlot> { slot });

        var template = new DocumentTemplate { Name = "Consent form", ReturnDeadline = ReturnDeadlineKind.NextOccurrence };
        store.SaveDocumentTemplates(new List<DocumentTemplate> { template });

        var sessionTemplate = new SessionTemplate { Name = "Phonics" };
        store.SaveSessionTemplates(new List<SessionTemplate> { sessionTemplate });

        var agendaItem = new AgendaItem { Date = new DateOnly(2026, 9, 4), Start = new TimeOnly(9, 0), End = new TimeOnly(9, 30), Kind = SlotKind.Duty };
        store.SaveAgenda(new List<AgendaItem> { agendaItem });

        var settings = new AppSettings { MorningBriefTime = new TimeOnly(7, 45), LaunchAtWindowsStartup = false };
        store.SaveSettings(settings);

        var reloaded = new JsonDataStore(_dir);

        Assert.Equal(DayOfWeek.Friday, reloaded.LoadRecurringSlots().Single().Day);
        Assert.Equal(SlotKind.PersonalTime, reloaded.LoadRecurringSlots().Single().Kind);
        Assert.Equal(ReturnDeadlineKind.NextOccurrence, reloaded.LoadDocumentTemplates().Single().ReturnDeadline);
        Assert.Equal("Phonics", reloaded.LoadSessionTemplates().Single().Name);
        Assert.Equal(new DateOnly(2026, 9, 4), reloaded.LoadAgenda().Single().Date);
        Assert.Equal(new TimeOnly(7, 45), reloaded.LoadSettings().MorningBriefTime);
        Assert.False(reloaded.LoadSettings().LaunchAtWindowsStartup);
    }

    [Fact]
    public void Loading_from_an_empty_directory_returns_empty_defaults_not_null()
    {
        var store = new JsonDataStore(_dir);

        Assert.Empty(store.LoadRecurringSlots());
        Assert.Empty(store.LoadDocumentTemplates());
        Assert.Empty(store.LoadAgenda());
        Assert.NotNull(store.LoadSettings());
    }
}
