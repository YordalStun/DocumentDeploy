using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.Planning;

/// <summary>
/// Turns the recurring weekly timetable into concrete, dated AgendaItems for a target week.
/// Safe to call repeatedly for the same week - it never duplicates an item that was already
/// generated for a given slot+date, so editing a template later and re-running only fills gaps.
/// </summary>
public static class WeekPlanGenerator
{
    /// <param name="weekStart">Any date in the target week; only its Monday-based week matters.</param>
    public static List<AgendaItem> GenerateWeek(
        DateOnly weekStart,
        IReadOnlyList<RecurringSlot> recurringSlots,
        IReadOnlyList<AgendaItem> existingAgenda,
        IReadOnlyList<DocumentTemplate> documentTemplates,
        IReadOnlyList<SessionTemplate> sessionTemplates)
    {
        var monday = StartOfWeek(weekStart);
        var created = new List<AgendaItem>();

        for (var offset = 0; offset < 7; offset++)
        {
            var date = monday.AddDays(offset);
            foreach (var slot in recurringSlots.Where(s => s.Active && s.Day == date.DayOfWeek))
            {
                var alreadyExists = existingAgenda
                    .Any(a => a.SourceRecurringSlotId == slot.Id && a.Date == date);
                if (alreadyExists) continue;

                created.Add(BuildAgendaItem(date, slot, documentTemplates, sessionTemplates));
            }
        }

        return created;
    }

    private static AgendaItem BuildAgendaItem(
        DateOnly date,
        RecurringSlot slot,
        IReadOnlyList<DocumentTemplate> documentTemplates,
        IReadOnlyList<SessionTemplate> sessionTemplates)
    {
        var item = new AgendaItem
        {
            Date = date,
            Start = slot.Start,
            End = slot.End,
            Title = slot.Title,
            Kind = slot.Kind,
            GroupName = slot.GroupName,
            Notes = slot.Notes,
            SourceRecurringSlotId = slot.Id,
            SessionTemplateId = slot.SessionTemplateId,
        };

        var templateIds = new List<Guid>(slot.DocumentTemplateIds);

        if (slot.SessionTemplateId is { } sessionTemplateId)
        {
            var sessionTemplate = sessionTemplates.FirstOrDefault(t => t.Id == sessionTemplateId);
            if (sessionTemplate is not null)
            {
                templateIds.AddRange(sessionTemplate.DocumentTemplateIds);
                item.Notes ??= sessionTemplate.DefaultNotes;
                foreach (var field in sessionTemplate.NoteFields)
                    item.FieldValues[field.Id] = string.Empty;
            }
        }

        foreach (var templateId in templateIds.Distinct())
        {
            var template = documentTemplates.FirstOrDefault(t => t.Id == templateId);
            if (template is null) continue;

            item.DocumentNeeds.Add(new DocumentNeed
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

        return item;
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
