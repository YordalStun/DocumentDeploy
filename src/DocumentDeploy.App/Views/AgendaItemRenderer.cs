using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DocumentDeploy.App.Controls;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Models;
using DocumentDeploy.Core.Scheduling;

namespace DocumentDeploy.App.Views;

/// <summary>Shared building blocks for rendering an AgendaItem, used by the dashboard's
/// "right now"/"coming up" cards, the outstanding banner, and the today list.</summary>
internal static class AgendaItemRenderer
{
    public static string FormatTimeRange(AgendaItem item) => $"{item.Start:HH:mm}–{item.End:HH:mm}";

    public static string FormatKind(SlotKind kind) => kind switch
    {
        SlotKind.Lesson => "Lesson",
        SlotKind.Duty => "Duty",
        SlotKind.Meeting => "Meeting",
        SlotKind.PersonalTime => "Personal time",
        SlotKind.Other => "Other",
        _ => kind.ToString(),
    };

    /// <summary>Full, interactive detail panel - notes, template field answers, and every
    /// document need (drag-and-drop return zones for anything that needs to come back).</summary>
    public static StackPanel BuildDetailPanel(AgendaItem item, AppState state, Action onChanged)
    {
        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });

        var meta = $"{FormatTimeRange(item)} · {FormatKind(item.Kind)}";
        if (!string.IsNullOrWhiteSpace(item.GroupName)) meta += $" · {item.GroupName}";
        panel.Children.Add(new TextBlock
        {
            Text = meta,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 2, 0, 0),
        });

        if (!string.IsNullOrWhiteSpace(item.Notes))
        {
            panel.Children.Add(new TextBlock
            {
                Text = item.Notes,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0),
            });
        }

        foreach (var (label, value) in GetFieldDisplayLines(item, state))
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"{label}: {value}",
                TextWrapping = TextWrapping.Wrap,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 4, 0, 0),
            });
        }

        if (item.DocumentNeeds.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Documents",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 14, 0, 6),
            });

            foreach (var need in item.DocumentNeeds)
                panel.Children.Add(BuildDocumentRow(need, onChanged));
        }

        return panel;
    }

    public static UIElement BuildDocumentRow(DocumentNeed need, Action onChanged)
    {
        if (need.NeedsReturn)
        {
            var zone = new ReturnDropZoneControl { Margin = new Thickness(0, 0, 0, 8) };
            zone.Bind(need, onChanged);
            return zone;
        }

        // DockPanel's LastChildFill (default true) makes the LAST child fill remaining space and
        // ignores its own Dock value - so the button (docked right) must be added BEFORE the
        // text, letting the text be the fill element that both sits left and wraps correctly
        // against the actual remaining width.
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

        if (!string.IsNullOrWhiteSpace(need.SourcePath))
        {
            var button = new Button
            {
                Content = "Open folder",
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Style = Application.Current.TryFindResource("SecondaryButton") as Style,
            };
            button.Click += (_, _) => FileSystemLauncher.OpenInExplorer(need.SourcePath);
            DockPanel.SetDock(button, Dock.Right);
            row.Children.Add(button);
        }

        var text = new TextBlock
        {
            Text = need.Name,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        row.Children.Add(text);

        return row;
    }

    /// <summary>A compact, non-interactive summary row for the full-day agenda list.</summary>
    public static UIElement BuildSummaryRow(AgendaItem item, DateTime now)
    {
        var row = new Border
        {
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 4),
            CornerRadius = new CornerRadius(6),
            Background = item.Start <= TimeOnly.FromDateTime(now) && TimeOnly.FromDateTime(now) < item.End
                ? new SolidColorBrush(Color.FromRgb(0xE9, 0xEE, 0xFB))
                : Brushes.Transparent,
        };

        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        stack.Children.Add(new TextBlock
        {
            Text = FormatTimeRange(item),
            Width = 100,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var titlePanel = new StackPanel();
        titlePanel.Children.Add(new TextBlock { Text = item.Title, TextWrapping = TextWrapping.Wrap });

        var docSummary = SummarizeDocuments(item);
        if (!string.IsNullOrEmpty(docSummary))
        {
            titlePanel.Children.Add(new TextBlock
            {
                Text = docSummary,
                FontSize = 11,
                Foreground = Brushes.Gray,
            });
        }

        stack.Children.Add(titlePanel);
        row.Child = stack;
        return row;
    }

    private static string SummarizeDocuments(AgendaItem item)
    {
        if (item.DocumentNeeds.Count == 0) return string.Empty;

        var outstanding = item.DocumentNeeds.Count(n => n.NeedsReturn && n.Return is null);
        var toBring = item.DocumentNeeds.Count(n => !n.NeedsReturn);

        var parts = new List<string>();
        if (toBring > 0) parts.Add($"{toBring} to bring");
        if (outstanding > 0) parts.Add($"{outstanding} awaiting return");
        return string.Join(" · ", parts);
    }

    public static UIElement BuildOutstandingRow(OutstandingDocumentNeed outstanding, AppState state, Action onChanged)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(new TextBlock
        {
            Text = $"{outstanding.Item.Title} · {outstanding.Item.Date:ddd d MMM}",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4),
        });
        panel.Children.Add(BuildDocumentRow(outstanding.Need, onChanged));
        return panel;
    }

    private static IEnumerable<(string Label, string Value)> GetFieldDisplayLines(AgendaItem item, AppState state)
    {
        if (item.SessionTemplateId is not { } templateId) yield break;

        var template = state.SessionTemplates.FirstOrDefault(t => t.Id == templateId);
        if (template is null) yield break;

        foreach (var field in template.NoteFields)
        {
            if (item.FieldValues.TryGetValue(field.Id, out var value) && !string.IsNullOrWhiteSpace(value))
                yield return (field.Label, value);
        }
    }
}
