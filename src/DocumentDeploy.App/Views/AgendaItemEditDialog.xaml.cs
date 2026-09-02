using System.Windows;
using System.Windows.Controls;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.App.Views;

public partial class AgendaItemEditDialog : Window
{
    private readonly AppState _state;
    private readonly AgendaItem _item;
    private readonly Dictionary<Guid, TextBox> _fieldBoxes = new();

    public bool Saved { get; private set; }

    public AgendaItemEditDialog(AppState state, AgendaItem item)
    {
        InitializeComponent();
        _state = state;
        _item = item;

        KindCombo.ItemsSource = Enum.GetValues<SlotKind>();
        DocumentsListBox.ItemsSource = _state.DocumentTemplates;

        var sessionOptions = new List<SessionTemplate?> { null };
        sessionOptions.AddRange(_state.SessionTemplates);
        SessionTemplateCombo.ItemsSource = sessionOptions;

        Load();
    }

    private void Load()
    {
        TitleBox.Text = _item.Title;
        DatePicker.SelectedDate = _item.Date.ToDateTime(TimeOnly.MinValue);
        StartBox.Text = _item.Start.ToString("HH:mm");
        EndBox.Text = _item.End.ToString("HH:mm");
        KindCombo.SelectedItem = _item.Kind;
        GroupBox.Text = _item.GroupName;
        NotesBox.Text = _item.Notes;
        SessionTemplateCombo.SelectedItem = _state.SessionTemplates.FirstOrDefault(t => t.Id == _item.SessionTemplateId);

        foreach (var template in _state.DocumentTemplates)
        {
            if (_item.DocumentNeeds.Any(n => n.TemplateId == template.Id))
                DocumentsListBox.SelectedItems.Add(template);
        }

        // Items already generated from a recurring slot already repeat every week - the
        // checkbox is only meaningful for a brand-new, one-off item.
        RepeatsCheckBox.Visibility = _item.SourceRecurringSlotId is null ? Visibility.Visible : Visibility.Collapsed;

        RebuildFieldValuePanel();
    }

    private void OnSessionTemplateChanged(object sender, SelectionChangedEventArgs e) => RebuildFieldValuePanel();

    /// <summary>
    /// Only planning-time questions are editable here - this dialog is reached from the weekly
    /// planner, which is a planning-time tool. Completion-time questions (e.g. "how did it go")
    /// are answered later, from the dashboard, once the session has actually happened.
    /// </summary>
    private void RebuildFieldValuePanel()
    {
        FieldValuesPanel.Children.Clear();
        _fieldBoxes.Clear();

        var planningFields = (SessionTemplateCombo.SelectedItem as SessionTemplate)?.NoteFields
            .Where(f => f.AskAt == PromptTiming.Planning)
            .ToList() ?? new List<NotePromptField>();

        FieldValuesHeader.Visibility = planningFields.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var field in planningFields)
        {
            FieldValuesPanel.Children.Add(new TextBlock { Text = field.Label, Margin = new Thickness(0, 6, 0, 0) });
            var box = new TextBox
            {
                Margin = new Thickness(0, 2, 0, 0),
                AcceptsReturn = field.Multiline,
                Height = field.Multiline ? 50 : double.NaN,
                TextWrapping = TextWrapping.Wrap,
            };
            if (_item.FieldValues.TryGetValue(field.Id, out var value))
                box.Text = value;

            _fieldBoxes[field.Id] = box;
            FieldValuesPanel.Children.Add(box);
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            ErrorText.Text = "Please enter a title.";
            return;
        }
        if (DatePicker.SelectedDate is not { } date)
        {
            ErrorText.Text = "Please choose a date.";
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

        _item.Title = TitleBox.Text.Trim();
        _item.Date = DateOnly.FromDateTime(date);
        _item.Start = start;
        _item.End = end;
        _item.Kind = KindCombo.SelectedItem is SlotKind kind ? kind : SlotKind.Lesson;
        _item.GroupName = string.IsNullOrWhiteSpace(GroupBox.Text) ? null : GroupBox.Text.Trim();
        _item.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
        _item.SessionTemplateId = (SessionTemplateCombo.SelectedItem as SessionTemplate)?.Id;

        // Merge, don't replace - completion-time answers for this item (not shown/editable in
        // this dialog) must survive a planning-time edit.
        foreach (var (fieldId, box) in _fieldBoxes)
            _item.FieldValues[fieldId] = box.Text;

        SyncDocumentNeeds();
        ApplyRepeatIfRequested();

        Saved = true;
        Close();
    }

    /// <summary>
    /// Promotes this one-off item into a recurring weekly slot so future weeks pick it up via
    /// "Generate from timetable" - reuses the existing recurring-slot machinery rather than
    /// inventing a second, parallel repeat mechanism.
    /// </summary>
    private void ApplyRepeatIfRequested()
    {
        if (RepeatsCheckBox.Visibility != Visibility.Visible || RepeatsCheckBox.IsChecked != true) return;
        if (_item.SourceRecurringSlotId is not null) return;

        var slot = new RecurringSlot
        {
            Day = _item.Date.DayOfWeek,
            Start = _item.Start,
            End = _item.End,
            Title = _item.Title,
            Kind = _item.Kind,
            GroupName = _item.GroupName,
            Notes = _item.Notes,
            SessionTemplateId = _item.SessionTemplateId,
            DocumentTemplateIds = _item.DocumentNeeds
                .Where(n => n.TemplateId is not null)
                .Select(n => n.TemplateId!.Value)
                .ToList(),
        };

        _state.RecurringSlots.Add(slot);
        _state.SaveRecurringSlots();
        _item.SourceRecurringSlotId = slot.Id;
    }

    private void SyncDocumentNeeds()
    {
        var selectedTemplates = DocumentsListBox.SelectedItems.Cast<DocumentTemplate>().ToList();
        var selectedIds = selectedTemplates.Select(t => t.Id).ToHashSet();

        _item.DocumentNeeds.RemoveAll(n => n.TemplateId is { } tid && !selectedIds.Contains(tid));

        var alreadyPresent = _item.DocumentNeeds
            .Where(n => n.TemplateId is not null)
            .Select(n => n.TemplateId!.Value)
            .ToHashSet();

        foreach (var template in selectedTemplates)
        {
            if (alreadyPresent.Contains(template.Id)) continue;

            _item.DocumentNeeds.Add(new DocumentNeed
            {
                TemplateId = template.Id,
                Name = template.Name,
                SourcePath = template.SourcePath,
                NeedsReturn = template.NeedsReturn,
                ReturnDestinationPath = template.ReturnDestinationPath,
                ReturnDeadline = template.ReturnDeadline,
                Notes = template.Notes,
            });
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
