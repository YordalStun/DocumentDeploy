using System.Windows;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.App.Views;

public partial class DocumentTemplateEditDialog : Window
{
    private readonly DocumentTemplate _template;

    public bool Saved { get; private set; }

    public DocumentTemplateEditDialog(DocumentTemplate template)
    {
        InitializeComponent();
        _template = template;

        DeadlineCombo.ItemsSource = Enum.GetValues<ReturnDeadlineKind>();
        Load();
    }

    private void Load()
    {
        NameBox.Text = _template.Name;
        SourcePathBox.Text = _template.SourcePath;
        NeedsReturnCheckBox.IsChecked = _template.NeedsReturn;
        ReturnDestinationBox.Text = _template.ReturnDestinationPath;
        DeadlineCombo.SelectedItem = _template.ReturnDeadline;
        NotesBox.Text = _template.Notes;
        ReturnDetailsPanel.Visibility = _template.NeedsReturn ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnNeedsReturnChanged(object sender, RoutedEventArgs e) =>
        ReturnDetailsPanel.Visibility = NeedsReturnCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = "Please enter a name.";
            return;
        }

        _template.Name = NameBox.Text.Trim();
        _template.SourcePath = string.IsNullOrWhiteSpace(SourcePathBox.Text) ? null : SourcePathBox.Text.Trim();
        _template.NeedsReturn = NeedsReturnCheckBox.IsChecked == true;
        _template.ReturnDestinationPath = string.IsNullOrWhiteSpace(ReturnDestinationBox.Text) ? null : ReturnDestinationBox.Text.Trim();
        _template.ReturnDeadline = DeadlineCombo.SelectedItem is ReturnDeadlineKind d ? d : ReturnDeadlineKind.EndOfSlot;
        _template.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

        Saved = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
