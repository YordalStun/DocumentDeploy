using System.Windows;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.App.Views;

public partial class SessionTemplateEditDialog : Window
{
    private readonly AppState _state;
    private readonly SessionTemplate _template;
    private readonly List<NotePromptField> _fields;

    public bool Saved { get; private set; }

    public SessionTemplateEditDialog(AppState state, SessionTemplate template)
    {
        InitializeComponent();
        _state = state;
        _template = template;
        _fields = template.NoteFields
            .Select(f => new NotePromptField { Id = f.Id, Label = f.Label, Multiline = f.Multiline, AskAt = f.AskAt })
            .ToList();

        DocumentsListBox.ItemsSource = _state.DocumentTemplates;
        NewFieldTimingCombo.ItemsSource = Enum.GetValues<PromptTiming>();
        NewFieldTimingCombo.SelectedItem = PromptTiming.Planning;
        RefreshFields();

        NameBox.Text = template.Name;
        DefaultNotesBox.Text = template.DefaultNotes;

        foreach (var doc in _state.DocumentTemplates)
        {
            if (template.DocumentTemplateIds.Contains(doc.Id))
                DocumentsListBox.SelectedItems.Add(doc);
        }
    }

    private void RefreshFields()
    {
        FieldsListBox.ItemsSource = null;
        FieldsListBox.ItemsSource = _fields;
    }

    private void OnAddFieldClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewFieldBox.Text)) return;

        _fields.Add(new NotePromptField
        {
            Label = NewFieldBox.Text.Trim(),
            AskAt = NewFieldTimingCombo.SelectedItem is PromptTiming timing ? timing : PromptTiming.Planning,
        });
        NewFieldBox.Clear();
        RefreshFields();
    }

    private void OnRemoveFieldClick(object sender, RoutedEventArgs e)
    {
        if (FieldsListBox.SelectedItem is not NotePromptField field) return;

        _fields.Remove(field);
        RefreshFields();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = "Please enter a name.";
            return;
        }

        _template.Name = NameBox.Text.Trim();
        _template.DefaultNotes = string.IsNullOrWhiteSpace(DefaultNotesBox.Text) ? null : DefaultNotesBox.Text.Trim();
        _template.NoteFields = _fields;
        _template.DocumentTemplateIds = DocumentsListBox.SelectedItems.Cast<DocumentTemplate>().Select(d => d.Id).ToList();

        Saved = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
