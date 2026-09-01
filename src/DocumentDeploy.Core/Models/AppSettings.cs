namespace DocumentDeploy.Core.Models;

public sealed class AppSettings
{
    /// <summary>When the full-day morning brief automatically opens on a school day.</summary>
    public TimeOnly MorningBriefTime { get; set; } = new TimeOnly(7, 30);

    /// <summary>How long before a slot starts it counts as "coming up" and worth prepping for.</summary>
    public int PrepLeadTimeMinutes { get; set; } = 15;

    public bool LaunchAtWindowsStartup { get; set; } = true;

    /// <summary>Day the app nudges you to plan next week (default Friday afternoon).</summary>
    public DayOfWeek PlanningReminderDay { get; set; } = DayOfWeek.Friday;

    public TimeOnly PlanningReminderTime { get; set; } = new TimeOnly(14, 30);

    public DateOnly? LastMorningBriefShownDate { get; set; }
    public DateOnly? LastPlanningReminderShownDate { get; set; }
}
