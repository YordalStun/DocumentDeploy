using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Models;
using DocumentDeploy.Core.Planning;
using DocumentDeploy.Core.Scheduling;

namespace DocumentDeploy.App.Views;

/// <summary>The Friday planning session: pick a week, generate it from the recurring timetable,
/// add one-off items on top, answer planning-time questions in one place, and see anything
/// still outstanding or answered from last week.</summary>
public partial class WeeklyPlannerWindow : Window
{
    private readonly AppState _state;
    private DateOnly _weekStart;

    public WeeklyPlannerWindow(AppState state)
    {
        InitializeComponent();
        _state = state;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var isLateInWeek = today.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday;
        _weekStart = StartOfWeek(isLateInWeek ? today.AddDays(7) : today);

        RefreshAll();
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private void RefreshAll()
    {
        WeekLabel.Text = $"{_weekStart:ddd d MMM} – {_weekStart.AddDays(6):ddd d MMM}";
        RefreshGrid();
        RefreshOutstanding();
        RefreshRecap();
    }

    private void RefreshGrid()
    {
        var weekEnd = _weekStart.AddDays(6);
        Grid.ItemsSource = ItemsInWeek(_weekStart, weekEnd).ToList();
    }

    private IEnumerable<AgendaItem> ItemsInWeek(DateOnly start, DateOnly end) =>
        _state.Agenda.Where(i => i.Date >= start && i.Date <= end).OrderBy(i => i.Date).ThenBy(i => i.Start);

    private void RefreshOutstanding()
    {
        var overdue = ScheduleEngine.GetOutstandingReturns(_state.Agenda, DateTime.Now).Where(o => o.IsOverdue).ToList();
        if (overdue.Count == 0)
        {
            OutstandingBanner.Visibility = Visibility.Collapsed;
            return;
        }

        OutstandingBanner.Visibility = Visibility.Visible;
        var names = string.Join(", ", overdue.Take(3).Select(o => o.Need.Name));
        var extra = overdue.Count > 3 ? $" and {overdue.Count - 3} more" : "";
        OutstandingText.Text = $"{overdue.Count} document(s) still outstanding: {names}{extra}. Open the dashboard to file them.";
    }

    /// <summary>Shows answered "after completion" questions from the week immediately before the
    /// one being viewed, so this week's planning can reference how last time went.</summary>
    private void RefreshRecap()
    {
        var lastWeekStart = _weekStart.AddDays(-7);
        var lastWeekEnd = _weekStart.AddDays(-1);

        RecapPanel.Children.Clear();
        var anyAnswers = false;

        foreach (var item in ItemsInWeek(lastWeekStart, lastWeekEnd))
        {
            if (item.SessionTemplateId is not { } templateId) continue;
            var template = _state.SessionTemplates.FirstOrDefault(t => t.Id == templateId);
            if (template is null) continue;

            var answered = template.NoteFields
                .Where(f => f.AskAt == PromptTiming.Completion)
                .Where(f => item.FieldValues.TryGetValue(f.Id, out var v) && !string.IsNullOrWhiteSpace(v))
                .Select(f => $"{f.Label}: {item.FieldValues[f.Id]}")
                .ToList();
            if (answered.Count == 0) continue;

            anyAnswers = true;
            RecapPanel.Children.Add(new TextBlock
            {
                Text = $"{item.Date:ddd d MMM} · {item.Title}",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 6, 0, 0),
            });
            RecapPanel.Children.Add(new TextBlock
            {
                Text = string.Join("  ·  ", answered),
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Gray,
            });
        }

        RecapCard.Visibility = anyAnswers ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPreviousWeekClick(object sender, RoutedEventArgs e)
    {
        _weekStart = _weekStart.AddDays(-7);
        RefreshAll();
    }

    private void OnNextWeekClick(object sender, RoutedEventArgs e)
    {
        _weekStart = _weekStart.AddDays(7);
        RefreshAll();
    }

    private void OnGenerateClick(object sender, RoutedEventArgs e)
    {
        var created = WeekPlanGenerator.GenerateWeek(_weekStart, _state.RecurringSlots, _state.Agenda, _state.DocumentTemplates, _state.SessionTemplates);
        _state.Agenda.AddRange(created);
        _state.SaveAgenda();
        RefreshGrid();

        var needingAnswers = GetItemsNeedingPlanningAnswers(created);
        if (needingAnswers.Count > 0)
        {
            new PlanningQuestionsDialog(_state, needingAnswers) { Owner = this }.ShowDialog();
            RefreshGrid();
        }
        else
        {
            MessageBox.Show($"Added {created.Count} item(s) from the timetable.", "Plan the week",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnAnswerPlanningQuestionsClick(object sender, RoutedEventArgs e)
    {
        var weekEnd = _weekStart.AddDays(6);
        var needingAnswers = GetItemsNeedingPlanningAnswers(ItemsInWeek(_weekStart, weekEnd));

        if (needingAnswers.Count == 0)
        {
            MessageBox.Show("Nothing left to answer for this week.", "Plan the week",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        new PlanningQuestionsDialog(_state, needingAnswers) { Owner = this }.ShowDialog();
        RefreshGrid();
    }

    /// <summary>Items with a session template that has planning-time questions still blank.</summary>
    private List<AgendaItem> GetItemsNeedingPlanningAnswers(IEnumerable<AgendaItem> items) =>
        items.Where(item =>
        {
            var template = _state.SessionTemplates.FirstOrDefault(t => t.Id == item.SessionTemplateId);
            var planningFields = template?.NoteFields.Where(f => f.AskAt == PromptTiming.Planning).ToList();
            if (planningFields is not { Count: > 0 }) return false;
            return planningFields.Any(f => !item.FieldValues.TryGetValue(f.Id, out var v) || string.IsNullOrWhiteSpace(v));
        }).ToList();

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var item = new AgendaItem { Date = _weekStart, Start = new TimeOnly(9, 0), End = new TimeOnly(9, 30), Title = "New item" };
        var dialog = new AgendaItemEditDialog(_state, item) { Owner = this };
        dialog.ShowDialog();

        if (dialog.Saved)
        {
            _state.Agenda.Add(item);
            _state.SaveAgenda();
            RefreshGrid();
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e) => EditSelected();
    private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => EditSelected();

    private void EditSelected()
    {
        if (Grid.SelectedItem is not AgendaItem item) return;

        var dialog = new AgendaItemEditDialog(_state, item) { Owner = this };
        dialog.ShowDialog();

        if (dialog.Saved)
        {
            _state.SaveAgenda();
            RefreshGrid();
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not AgendaItem item) return;

        var result = MessageBox.Show($"Delete \"{item.Title}\" on {item.Date:ddd d MMM}?", "Delete item",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        _state.Agenda.Remove(item);
        _state.SaveAgenda();
        RefreshGrid();
    }
}
