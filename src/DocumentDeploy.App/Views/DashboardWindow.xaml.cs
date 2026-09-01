using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Scheduling;

namespace DocumentDeploy.App.Views;

/// <summary>
/// The main hub: what's happening right now, what's coming up, the full day, and anything
/// outstanding from before - always visible so nothing missed is ever silently dropped.
/// Closing the window (the X button) just hides it back to the tray; only the tray's Exit
/// really quits the app.
/// </summary>
public partial class DashboardWindow : Window
{
    private readonly AppState _state;

    public bool AllowClose { get; set; }

    public DashboardWindow(AppState state)
    {
        InitializeComponent();
        _state = state;
    }

    public void UpdateSnapshot(ScheduleSnapshot snapshot)
    {
        DateText.Text = snapshot.Now.ToString("dddd, d MMMM");
        TimeStatusText.Text = snapshot.Now.ToString("HH:mm") + (snapshot.IsBusy ? " · busy right now" : " · free right now");

        RenderOutstanding(snapshot);
        RenderCurrent(snapshot);
        RenderNext(snapshot);
        RenderToday(snapshot);
    }

    private void RenderOutstanding(ScheduleSnapshot snapshot)
    {
        var overdue = snapshot.OutstandingReturns.Where(o => o.IsOverdue).ToList();
        OutstandingBanner.Visibility = overdue.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        OutstandingPanel.Children.Clear();
        foreach (var item in overdue)
            OutstandingPanel.Children.Add(AgendaItemRenderer.BuildOutstandingRow(item, _state, OnDataChanged));
    }

    private void RenderCurrent(ScheduleSnapshot snapshot)
    {
        CurrentPanel.Children.Clear();
        if (snapshot.CurrentItem is { } current)
        {
            CurrentPanel.Children.Add(AgendaItemRenderer.BuildDetailPanel(current, _state, OnDataChanged));
        }
        else
        {
            var message = snapshot.NextItem is { } next
                ? $"Nothing on right now. Next up at {next.Start:HH:mm}: {next.Title}."
                : "Nothing else on today.";
            CurrentPanel.Children.Add(new TextBlock { Text = message, Foreground = System.Windows.Media.Brushes.Gray, TextWrapping = TextWrapping.Wrap });
        }
    }

    private void RenderNext(ScheduleSnapshot snapshot)
    {
        if (snapshot.CurrentItem is not null && snapshot.NextItem is { } next)
        {
            NextCard.Visibility = Visibility.Visible;
            NextPanel.Children.Clear();
            NextPanel.Children.Add(AgendaItemRenderer.BuildDetailPanel(next, _state, OnDataChanged));
        }
        else
        {
            NextCard.Visibility = Visibility.Collapsed;
        }
    }

    private void RenderToday(ScheduleSnapshot snapshot)
    {
        TodayPanel.Children.Clear();
        if (snapshot.TodayAgenda.Count == 0)
        {
            TodayPanel.Children.Add(new TextBlock { Text = "Nothing planned today.", Foreground = System.Windows.Media.Brushes.Gray });
            return;
        }

        foreach (var item in snapshot.TodayAgenda)
            TodayPanel.Children.Add(AgendaItemRenderer.BuildSummaryRow(item, snapshot.Now));
    }

    private void OnDataChanged() => _state.SaveAgenda();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (AllowClose) return;
        e.Cancel = true;
        Hide();
    }

    public void OpenTimetableEditor() => new TimetableEditorWindow(_state) { Owner = this }.ShowDialog();
    public void OpenTemplateLibrary() => new DocumentTemplateLibraryWindow(_state) { Owner = this }.ShowDialog();
    public void OpenWeeklyPlanner() => new WeeklyPlannerWindow(_state) { Owner = this }.ShowDialog();
    public void OpenSettings() => new SettingsWindow(_state) { Owner = this }.ShowDialog();

    private void OnTimetableClick(object sender, RoutedEventArgs e) => OpenTimetableEditor();
    private void OnTemplatesClick(object sender, RoutedEventArgs e) => OpenTemplateLibrary();
    private void OnPlanWeekClick(object sender, RoutedEventArgs e) => OpenWeeklyPlanner();
    private void OnSettingsClick(object sender, RoutedEventArgs e) => OpenSettings();
}
