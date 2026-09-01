using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Filing;
using DocumentDeploy.Core.Models;
using Microsoft.Win32;

namespace DocumentDeploy.App.Controls;

/// <summary>
/// One document need's return workflow: drag a file (or browse for one) and it's copied into
/// the need's destination folder - the original is never touched. Shows current status and
/// lets you undo a mistaken confirmation (the copy stays; only the app's record is cleared).
/// </summary>
public partial class ReturnDropZoneControl : UserControl
{
    private static readonly SolidColorBrush IdleBackground = new(Color.FromRgb(0xFA, 0xFB, 0xFD));
    private static readonly SolidColorBrush DoneBackground = new(Color.FromRgb(0xE9, 0xF7, 0xEF));

    private DocumentNeed? _need;
    private Action? _onChanged;

    public ReturnDropZoneControl()
    {
        InitializeComponent();
    }

    public void Bind(DocumentNeed need, Action onChanged)
    {
        _need = need;
        _onChanged = onChanged;
        Refresh();
    }

    private void Refresh()
    {
        if (_need is null) return;

        NameText.Text = _need.Name;
        OpenSourceButton.Visibility = string.IsNullOrWhiteSpace(_need.SourcePath) ? Visibility.Collapsed : Visibility.Visible;

        if (_need.Return is { } ret)
        {
            StatusText.Text = $"Filed away as \"{ret.ConfirmedFileName}\" on {ret.ConfirmedAtUtc.ToLocalTime():d MMM, HH:mm}";
            BrowseButton.Visibility = Visibility.Collapsed;
            UndoButton.Visibility = Visibility.Visible;
            RootBorder.Background = DoneBackground;
        }
        else
        {
            var destinationKnown = !string.IsNullOrWhiteSpace(_need.ReturnDestinationPath);
            StatusText.Text = destinationKnown
                ? $"Drag the completed file here, or browse - it will be filed in {_need.ReturnDestinationPath}"
                : "No destination folder is set for this document yet - set one in the Document Templates screen.";
            BrowseButton.Visibility = Visibility.Visible;
            BrowseButton.IsEnabled = destinationKnown;
            UndoButton.Visibility = Visibility.Collapsed;
            RootBorder.Background = IdleBackground;
        }
    }

    private void OnDragEnter(object sender, DragEventArgs e) =>
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDragLeave(object sender, DragEventArgs e)
    {
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (_need is null || string.IsNullOrWhiteSpace(_need.ReturnDestinationPath)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            FileAway(files[0]);
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        if (_need is null || string.IsNullOrWhiteSpace(_need.ReturnDestinationPath)) return;

        var dialog = new OpenFileDialog { Title = $"Choose the completed file for \"{_need.Name}\"" };
        if (dialog.ShowDialog() == true)
            FileAway(dialog.FileName);
    }

    private void FileAway(string sourcePath)
    {
        try
        {
            _need!.Return = DocumentFilingService.FileAway(sourcePath, _need.ReturnDestinationPath!);
            Refresh();
            _onChanged?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't file that document away:\n{ex.Message}", "DocumentDeploy",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        if (_need is null) return;
        _need.Return = null;
        Refresh();
        _onChanged?.Invoke();
    }

    private void OnOpenSourceClick(object sender, RoutedEventArgs e) => FileSystemLauncher.OpenInExplorer(_need?.SourcePath);
}
