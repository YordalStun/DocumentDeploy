using System.Windows;
using System.Windows.Input;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.App.Views;

public partial class DocumentTemplateLibraryWindow : Window
{
    private readonly AppState _state;

    public DocumentTemplateLibraryWindow(AppState state)
    {
        InitializeComponent();
        _state = state;
        RefreshDocuments();
        RefreshSessionTemplates();
    }

    private void RefreshDocuments() => DocumentsGrid.ItemsSource = _state.DocumentTemplates.OrderBy(t => t.Name).ToList();
    private void RefreshSessionTemplates() => SessionTemplatesGrid.ItemsSource = _state.SessionTemplates.OrderBy(t => t.Name).ToList();

    private void OnAddDocumentClick(object sender, RoutedEventArgs e)
    {
        var template = new DocumentTemplate { Name = "New document" };
        var dialog = new DocumentTemplateEditDialog(template) { Owner = this };
        dialog.ShowDialog();

        if (dialog.Saved)
        {
            _state.DocumentTemplates.Add(template);
            _state.SaveDocumentTemplates();
            RefreshDocuments();
        }
    }

    private void OnEditDocumentClick(object sender, RoutedEventArgs e) => EditSelectedDocument();
    private void OnDocumentsGridDoubleClick(object sender, MouseButtonEventArgs e) => EditSelectedDocument();

    private void EditSelectedDocument()
    {
        if (DocumentsGrid.SelectedItem is not DocumentTemplate template) return;

        var dialog = new DocumentTemplateEditDialog(template) { Owner = this };
        dialog.ShowDialog();

        if (dialog.Saved)
        {
            _state.SaveDocumentTemplates();
            RefreshDocuments();
        }
    }

    private void OnDeleteDocumentClick(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not DocumentTemplate template) return;

        var inUse = _state.RecurringSlots.Any(s => s.DocumentTemplateIds.Contains(template.Id))
            || _state.SessionTemplates.Any(t => t.DocumentTemplateIds.Contains(template.Id));
        var warning = inUse
            ? " It's used by at least one timetable slot or session template - already-generated weeks are unaffected, but future weeks will stop including it."
            : "";

        var result = MessageBox.Show($"Delete \"{template.Name}\"?{warning}", "Delete document",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        _state.DocumentTemplates.Remove(template);
        foreach (var slot in _state.RecurringSlots) slot.DocumentTemplateIds.Remove(template.Id);
        foreach (var session in _state.SessionTemplates) session.DocumentTemplateIds.Remove(template.Id);

        _state.SaveDocumentTemplates();
        _state.SaveRecurringSlots();
        _state.SaveSessionTemplates();
        RefreshDocuments();
    }

    private void OnAddSessionTemplateClick(object sender, RoutedEventArgs e)
    {
        var template = new SessionTemplate { Name = "New session template" };
        var dialog = new SessionTemplateEditDialog(_state, template) { Owner = this };
        dialog.ShowDialog();

        if (dialog.Saved)
        {
            _state.SessionTemplates.Add(template);
            _state.SaveSessionTemplates();
            RefreshSessionTemplates();
        }
    }

    private void OnEditSessionTemplateClick(object sender, RoutedEventArgs e) => EditSelectedSessionTemplate();
    private void OnSessionTemplatesGridDoubleClick(object sender, MouseButtonEventArgs e) => EditSelectedSessionTemplate();

    private void EditSelectedSessionTemplate()
    {
        if (SessionTemplatesGrid.SelectedItem is not SessionTemplate template) return;

        var dialog = new SessionTemplateEditDialog(_state, template) { Owner = this };
        dialog.ShowDialog();

        if (dialog.Saved)
        {
            _state.SaveSessionTemplates();
            RefreshSessionTemplates();
        }
    }

    private void OnDeleteSessionTemplateClick(object sender, RoutedEventArgs e)
    {
        if (SessionTemplatesGrid.SelectedItem is not SessionTemplate template) return;

        var result = MessageBox.Show(
            $"Delete \"{template.Name}\"? Timetable slots using it will keep their existing documents but lose the linked question prompts.",
            "Delete session template", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        _state.SessionTemplates.Remove(template);
        foreach (var slot in _state.RecurringSlots.Where(s => s.SessionTemplateId == template.Id))
            slot.SessionTemplateId = null;

        _state.SaveSessionTemplates();
        _state.SaveRecurringSlots();
        RefreshSessionTemplates();
    }
}
