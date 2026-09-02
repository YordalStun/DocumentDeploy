using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.ImportExport;

/// <summary>Serializes/deserializes a SetupBundle to a single portable JSON file - carry it on a
/// USB stick, email it to yourself, whatever - and load it on another machine.</summary>
public static class SetupBundleIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Export(
        IReadOnlyList<RecurringSlot> recurringSlots,
        IReadOnlyList<DocumentTemplate> documentTemplates,
        IReadOnlyList<SessionTemplate> sessionTemplates,
        AppSettings settings)
    {
        var bundle = new SetupBundle
        {
            RecurringSlots = recurringSlots.ToList(),
            DocumentTemplates = documentTemplates.ToList(),
            SessionTemplates = sessionTemplates.ToList(),
            Settings = settings,
        };
        return JsonSerializer.Serialize(bundle, Options);
    }

    public static SetupBundle Import(string json) =>
        JsonSerializer.Deserialize<SetupBundle>(json, Options)
        ?? throw new InvalidOperationException("That file doesn't look like a DocumentDeploy setup export.");

    /// <summary>
    /// Applies an imported bundle onto existing mutable collections/settings in place -
    /// replacing the timetable, document templates, and session templates entirely, and copying
    /// settings field-by-field (never reassigning the AppSettings instance, since other
    /// components may already hold a reference to it). Never touches the dated agenda - that
    /// isn't part of the bundle at all.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT copy LastMorningBriefShownDate/LastPlanningReminderShownDate onto
    /// the target: those are this machine's own runtime tracking, not part of "setup", and
    /// blindly copying them from another machine could wrongly suppress today's morning brief
    /// or planning nudge here.
    /// </remarks>
    public static void ApplyTo(
        SetupBundle bundle,
        List<RecurringSlot> recurringSlots,
        List<DocumentTemplate> documentTemplates,
        List<SessionTemplate> sessionTemplates,
        AppSettings settings)
    {
        recurringSlots.Clear();
        recurringSlots.AddRange(bundle.RecurringSlots);

        documentTemplates.Clear();
        documentTemplates.AddRange(bundle.DocumentTemplates);

        sessionTemplates.Clear();
        sessionTemplates.AddRange(bundle.SessionTemplates);

        settings.MorningBriefTime = bundle.Settings.MorningBriefTime;
        settings.PrepLeadTimeMinutes = bundle.Settings.PrepLeadTimeMinutes;
        settings.LaunchAtWindowsStartup = bundle.Settings.LaunchAtWindowsStartup;
        settings.PlanningReminderDay = bundle.Settings.PlanningReminderDay;
        settings.PlanningReminderTime = bundle.Settings.PlanningReminderTime;
    }
}
