using System.Windows;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.App.Views;

public partial class RecurringSlotEditDialog : Window
{
    private readonly AppState _state;
    private readonly RecurringSlot _slot;

    public bool Saved { get; private set; }

    public RecurringSlotEditDialog(AppState state, RecurringSlot slot)
    {
        InitializeComponent();
        _state = state;
        _slot = slot;

        DayCombo.ItemsSource = Enum.GetValues<DayOfWeek>();
        KindCombo.ItemsSource = Enum.GetValues<SlotKind>();
        DocumentsListBox.ItemsSource = _state.DocumentTemplates;

        var sessionOptions = new List<SessionTemplate?> { null };
        sessionOptions.AddRange(_state.SessionTemplates);
        SessionTemplateCombo.ItemsSource = sessionOptions;

        LoadFromSlot();
    }

    private void LoadFromSlot()
    {
        TitleBox.Text = _slot.Title;
        DayCombo.SelectedItem = _slot.Day;
        StartBox.Text = _slot.Start.ToString("HH:mm");
        EndBox.Text = _slot.End.ToString("HH:mm");
        KindCombo.SelectedItem = _slot.Kind;
        GroupBox.Text = _slot.GroupName;
        NotesBox.Text = _slot.Notes;
        ActiveCheckBox.IsChecked = _slot.Active;
        SessionTemplateCombo.SelectedItem = _state.SessionTemplates.FirstOrDefault(t => t.Id == _slot.SessionTemplateId);

        foreach (var template in _state.DocumentTemplates)
        {
            if (_slot.DocumentTemplateIds.Contains(template.Id))
                DocumentsListBox.SelectedItems.Add(template);
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            ErrorText.Text = "Please enter a title.";
            return;
        }
        if (DayCombo.SelectedItem is not DayOfWeek day)
        {
            ErrorText.Text = "Please choose a day.";
            return;
        }
        if (!TimeOnly.TryParse(StartBox.Text, out var start) || !TimeOnly.TryParse(EndBox.Text, out var end))
        {
            ErrorText.Text = "Start and end must look like 09:00.";
            return;
        }
        if (end <= start)
        {
            ErrorText.Text = "End time must be after the start time.";
            return;
        }

        _slot.Title = TitleBox.Text.Trim();
        _slot.Day = day;
        _slot.Start = start;
        _slot.End = end;
        _slot.Kind = KindCombo.SelectedItem is SlotKind kind ? kind : SlotKind.Lesson;
        _slot.GroupName = string.IsNullOrWhiteSpace(GroupBox.Text) ? null : GroupBox.Text.Trim();
        _slot.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
        _slot.Active = ActiveCheckBox.IsChecked == true;
        _slot.SessionTemplateId = (SessionTemplateCombo.SelectedItem as SessionTemplate)?.Id;
        _slot.DocumentTemplateIds = DocumentsListBox.SelectedItems.Cast<DocumentTemplate>().Select(t => t.Id).ToList();

        Saved = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
