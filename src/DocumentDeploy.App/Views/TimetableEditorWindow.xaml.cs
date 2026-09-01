using System.Windows;
using System.Windows.Input;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.App.Views;

public partial class TimetableEditorWindow : Window
{
    private readonly AppState _state;

    public TimetableEditorWindow(AppState state)
    {
        InitializeComponent();
        _state = state;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        Grid.ItemsSource = _state.RecurringSlots
            .OrderBy(s => s.Day)
            .ThenBy(s => s.Start)
            .ToList();
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var slot = new RecurringSlot { Title = "New slot", Start = new TimeOnly(9, 0), End = new TimeOnly(9, 30) };
        var dialog = new RecurringSlotEditDialog(_state, slot) { Owner = this };
        dialog.ShowDialog();

        if (dialog.Saved)
        {
            _state.RecurringSlots.Add(slot);
            _state.SaveRecurringSlots();
            RefreshGrid();
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e) => EditSelected();

    private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => EditSelected();

    private void EditSelected()
    {
        if (Grid.SelectedItem is not RecurringSlot slot) return;

        var dialog = new RecurringSlotEditDialog(_state, slot) { Owner = this };
        dialog.ShowDialog();

        if (dialog.Saved)
        {
            _state.SaveRecurringSlots();
            RefreshGrid();
        }
    }

    private void OnDuplicateClick(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not RecurringSlot slot) return;

        var copy = new RecurringSlot
        {
            Day = slot.Day,
            Start = slot.Start,
            End = slot.End,
            Title = slot.Title + " (copy)",
            Kind = slot.Kind,
            GroupName = slot.GroupName,
            Notes = slot.Notes,
            Active = slot.Active,
            SessionTemplateId = slot.SessionTemplateId,
            DocumentTemplateIds = new List<Guid>(slot.DocumentTemplateIds),
        };

        _state.RecurringSlots.Add(copy);
        _state.SaveRecurringSlots();
        RefreshGrid();
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not RecurringSlot slot) return;

        var result = MessageBox.Show($"Delete \"{slot.Title}\"? Weeks already generated from it are unaffected.",
            "Delete slot", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        _state.RecurringSlots.Remove(slot);
        _state.SaveRecurringSlots();
        RefreshGrid();
    }
}
