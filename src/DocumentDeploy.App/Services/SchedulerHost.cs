using System.Windows.Threading;
using DocumentDeploy.Core.Models;
using DocumentDeploy.Core.Scheduling;

namespace DocumentDeploy.App.Services;

/// <summary>
/// Polls ScheduleEngine on a timer and decides when the dashboard should proactively pop
/// itself up: the morning brief, a slot's prep-lead-time being reached, a busy slot ending
/// with unreturned documents, a document newly going overdue, or a session newly needing its
/// "after completion" questions answered. Each trigger fires once per
/// occurrence (so it never spams the same tick's worth of state every 20 seconds); the
/// dashboard itself always shows the full outstanding list regardless, so nothing is ever
/// silently dropped - it just doesn't force a fresh popup for something already surfaced.
/// Never triggers while a lesson/duty/meeting is running.
/// </summary>
public sealed class SchedulerHost
{
    private readonly AppState _state;
    private readonly DispatcherTimer _timer;
    private readonly HashSet<Guid> _prepNotifiedItemIds = new();
    private readonly HashSet<Guid> _transitionPromptedNeedIds = new();
    private readonly HashSet<Guid> _overdueNotifiedNeedIds = new();
    private readonly HashSet<Guid> _completionPromptedItemIds = new();
    private AgendaItem? _lastCurrentItem;

    public event Action<ScheduleSnapshot>? SnapshotUpdated;
    public event Action<ScheduleSnapshot>? DashboardPopupRequested;
    public event Action? PlanningNudgeRequested;

    public SchedulerHost(AppState state, TimeSpan? interval = null)
    {
        _state = state;
        _timer = new DispatcherTimer { Interval = interval ?? TimeSpan.FromSeconds(20) };
        _timer.Tick += (_, _) => Tick();
    }

    public void Start()
    {
        Tick();
        _timer.Start();
    }

    public void Tick()
    {
        var now = DateTime.Now;
        var snapshot = ScheduleEngine.Evaluate(now, _state.Agenda, _state.Settings, _state.SessionTemplates);
        SnapshotUpdated?.Invoke(snapshot);

        var shouldPop = false;

        if (snapshot.ShouldShowMorningBriefNow)
        {
            _state.Settings.LastMorningBriefShownDate = DateOnly.FromDateTime(now);
            _state.SaveSettings();
            shouldPop = true;
        }

        var justTransitionedAway = _lastCurrentItem is not null && _lastCurrentItem.Id != snapshot.CurrentItem?.Id
            ? _lastCurrentItem
            : null;
        _lastCurrentItem = snapshot.CurrentItem;

        if (snapshot.PopupsAllowedNow)
        {
            if (snapshot.NextItem is { } next && snapshot.ItemsToPrepNow.Count > 0 && _prepNotifiedItemIds.Add(next.Id))
                shouldPop = true;

            if (justTransitionedAway is not null)
            {
                foreach (var need in justTransitionedAway.DocumentNeeds.Where(n => n.NeedsReturn && n.Return is null))
                {
                    if (_transitionPromptedNeedIds.Add(need.Id))
                        shouldPop = true;
                }
            }

            foreach (var outstanding in snapshot.OutstandingReturns.Where(o => o.IsOverdue))
            {
                if (_overdueNotifiedNeedIds.Add(outstanding.Need.Id))
                    shouldPop = true;
            }

            foreach (var pending in snapshot.PendingCompletionAnswers)
            {
                if (_completionPromptedItemIds.Add(pending.Item.Id))
                    shouldPop = true;
            }
        }

        if (shouldPop)
            DashboardPopupRequested?.Invoke(snapshot);

        if (snapshot.ShouldShowPlanningReminderNow)
        {
            _state.Settings.LastPlanningReminderShownDate = DateOnly.FromDateTime(now);
            _state.SaveSettings();
            PlanningNudgeRequested?.Invoke();
        }
    }
}
