using System.Windows;
using System.Windows.Controls;
using DocumentDeploy.App.Services;
using DocumentDeploy.Core.Models;

namespace DocumentDeploy.App.Views;

/// <summary>
/// Walks through every planning-time question for a batch of agenda items in one place, so
/// generating a week doesn't mean opening each lesson individually to fill in "today's sound"
/// and the like. Only ever touches planning-time fields - completion-time answers are untouched.
/// </summary>
public partial class PlanningQuestionsDialog : Window
{
    private readonly AppState _state;
    private readonly Dictionary<(AgendaItem Item, Guid FieldId), TextBox> _boxes = new();

    public PlanningQuestionsDialog(AppState state, IReadOnlyList<AgendaItem> items)
    {
        InitializeComponent();
        _state = state;

        foreach (var item in items)
        {
            var template = state.SessionTemplates.FirstOrDefault(t => t.Id == item.SessionTemplateId);
            var planningFields = template?.NoteFields.Where(f => f.AskAt == PromptTiming.Planning).ToList();
            if (planningFields is not { Count: > 0 }) continue;

            ItemsPanel.Children.Add(new TextBlock
            {
                Text = $"{item.Date:ddd d MMM} · {item.Title}",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 14, 0, 4),
            });

            foreach (var field in planningFields)
            {
                ItemsPanel.Children.Add(new TextBlock { Text = field.Label, Margin = new Thickness(0, 6, 0, 2) });
                var box = new TextBox
                {
                    AcceptsReturn = field.Multiline,
                    Height = field.Multiline ? 50 : double.NaN,
                    TextWrapping = TextWrapping.Wrap,
                };
                if (item.FieldValues.TryGetValue(field.Id, out var value))
                    box.Text = value;

                _boxes[(item, field.Id)] = box;
                ItemsPanel.Children.Add(box);
            }
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        foreach (var ((item, fieldId), box) in _boxes)
            item.FieldValues[fieldId] = box.Text;

        _state.SaveAgenda();
        Close();
    }

    private void OnSkipClick(object sender, RoutedEventArgs e) => Close();
}
