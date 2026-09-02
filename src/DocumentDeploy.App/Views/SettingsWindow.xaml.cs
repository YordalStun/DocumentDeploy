using System.IO;
using System.Windows;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.ImportExport;
using Microsoft.Win32;

namespace DocumentDeploy.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppState _state;

    public SettingsWindow(AppState state)
    {
        InitializeComponent();
        _state = state;

        PlanningDayCombo.ItemsSource = Enum.GetValues<DayOfWeek>();
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = _state.Settings;
        MorningBriefTimeBox.Text = s.MorningBriefTime.ToString("HH:mm");
        LeadTimeBox.Text = s.PrepLeadTimeMinutes.ToString();
        PlanningDayCombo.SelectedItem = s.PlanningReminderDay;
        PlanningTimeBox.Text = s.PlanningReminderTime.ToString("HH:mm");
        StartupCheckBox.IsChecked = s.LaunchAtWindowsStartup;
    }

    private bool SaveToSettings()
    {
        var s = _state.Settings;

        if (!TimeOnly.TryParse(MorningBriefTimeBox.Text, out var briefTime))
        {
            StatusText.Text = "Morning brief time must look like 07:30.";
            return false;
        }
        if (!int.TryParse(LeadTimeBox.Text, out var leadMinutes) || leadMinutes < 0)
        {
            StatusText.Text = "Prep lead time must be a whole number of minutes.";
            return false;
        }
        if (!TimeOnly.TryParse(PlanningTimeBox.Text, out var planningTime))
        {
            StatusText.Text = "Planning nudge time must look like 14:30.";
            return false;
        }

        s.MorningBriefTime = briefTime;
        s.PrepLeadTimeMinutes = leadMinutes;
        s.PlanningReminderDay = PlanningDayCombo.SelectedItem is DayOfWeek day ? day : DayOfWeek.Friday;
        s.PlanningReminderTime = planningTime;
        s.LaunchAtWindowsStartup = StartupCheckBox.IsChecked == true;

        _state.SaveSettings();
        StartupRegistrationService.SetEnabled(s.LaunchAtWindowsStartup);
        return true;
    }

    private void OnExportTimetableClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = "timetable.csv", Filter = "CSV files (*.csv)|*.csv" };
        if (dialog.ShowDialog() != true) return;

        var csv = RecurringSlotCsv.Export(_state.RecurringSlots, _state.DocumentTemplates, _state.SessionTemplates);
        File.WriteAllText(dialog.FileName, csv);
        StatusText.Text = $"Exported {_state.RecurringSlots.Count} timetable rows to {dialog.FileName}.";
    }

    private void OnImportTimetableClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
        if (dialog.ShowDialog() != true) return;

        var csv = File.ReadAllText(dialog.FileName);
        var result = RecurringSlotCsv.Import(csv, _state.RecurringSlots, _state.DocumentTemplates, _state.SessionTemplates);
        _state.SaveRecurringSlots();
        StatusText.Text = $"Timetable import: {result.Added} added, {result.Updated} updated." +
            (result.Warnings.Count > 0 ? $" {result.Warnings.Count} warning(s) - first: {result.Warnings[0]}" : "");
    }

    private void OnExportTemplatesClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = "document-templates.csv", Filter = "CSV files (*.csv)|*.csv" };
        if (dialog.ShowDialog() != true) return;

        var csv = DocumentTemplateCsv.Export(_state.DocumentTemplates);
        File.WriteAllText(dialog.FileName, csv);
        StatusText.Text = $"Exported {_state.DocumentTemplates.Count} document templates to {dialog.FileName}.";
    }

    private void OnImportTemplatesClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
        if (dialog.ShowDialog() != true) return;

        var csv = File.ReadAllText(dialog.FileName);
        var result = DocumentTemplateCsv.Import(csv, _state.DocumentTemplates);
        _state.SaveDocumentTemplates();
        StatusText.Text = $"Document templates import: {result.Added} added, {result.Updated} updated.";
    }

    private void OnExportSetupClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = "documentdeploy-setup.json", Filter = "DocumentDeploy setup (*.json)|*.json" };
        if (dialog.ShowDialog() != true) return;

        var json = SetupBundleIO.Export(_state.RecurringSlots, _state.DocumentTemplates, _state.SessionTemplates, _state.Settings);
        File.WriteAllText(dialog.FileName, json);
        StatusText.Text = $"Exported {_state.RecurringSlots.Count} timetable slot(s), {_state.DocumentTemplates.Count} document template(s) " +
            $"and {_state.SessionTemplates.Count} session template(s) to {dialog.FileName}.";
    }

    private void OnImportSetupClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "DocumentDeploy setup (*.json)|*.json" };
        if (dialog.ShowDialog() != true) return;

        SetupBundle bundle;
        try
        {
            bundle = SetupBundleIO.Import(File.ReadAllText(dialog.FileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't read that file:\n{ex.Message}", "Import setup", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"This will replace your current setup:\n\n" +
            $"Timetable: {_state.RecurringSlots.Count} slot(s) → {bundle.RecurringSlots.Count}\n" +
            $"Document templates: {_state.DocumentTemplates.Count} → {bundle.DocumentTemplates.Count}\n" +
            $"Session templates: {_state.SessionTemplates.Count} → {bundle.SessionTemplates.Count}\n\n" +
            "Already-planned weeks, filed documents, and answered questions are not affected.\n\n" +
            "Continue?",
            "Import setup", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        SetupBundleIO.ApplyTo(bundle, _state.RecurringSlots, _state.DocumentTemplates, _state.SessionTemplates, _state.Settings);

        _state.SaveRecurringSlots();
        _state.SaveDocumentTemplates();
        _state.SaveSessionTemplates();
        _state.SaveSettings();
        StartupRegistrationService.SetEnabled(_state.Settings.LaunchAtWindowsStartup);
        LoadFromSettings();

        StatusText.Text = $"Imported {bundle.RecurringSlots.Count} timetable slot(s), {bundle.DocumentTemplates.Count} document template(s) " +
            $"and {bundle.SessionTemplates.Count} session template(s).";
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (SaveToSettings())
            Close();
    }
}
