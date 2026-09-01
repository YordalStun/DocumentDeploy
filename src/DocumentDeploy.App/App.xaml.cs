using System.Windows;
using DocumentDeploy.App.Services;
using DocumentDeploy.App.Views;

namespace DocumentDeploy.App;

public partial class App : Application
{
    private SingleInstanceGuard? _instanceGuard;
    private TrayIconService? _tray;
    private SchedulerHost? _scheduler;

    public AppState State { get; private set; } = null!;
    public DashboardWindow Dashboard { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceGuard = new SingleInstanceGuard();
        if (!_instanceGuard.IsFirstInstance)
        {
            MessageBox.Show("DocumentDeploy is already running - check the system tray.", "DocumentDeploy",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        State = new AppState();
        if (State.Settings.LaunchAtWindowsStartup)
            StartupRegistrationService.SetEnabled(true);

        Dashboard = new DashboardWindow(State);

        _scheduler = new SchedulerHost(State);
        _scheduler.SnapshotUpdated += snapshot => Dashboard.UpdateSnapshot(snapshot);
        _scheduler.DashboardPopupRequested += snapshot => ShowDashboard(snapshot);
        _scheduler.PlanningNudgeRequested += () =>
            _tray?.ShowBalloon("Plan next week?", "It's time to sit down and plan next week - click here or open DocumentDeploy from the tray.");

        _tray = new TrayIconService();
        _tray.OpenRequested += () => ShowDashboard(null);
        _tray.PlanWeekRequested += () => { ShowDashboard(null); Dashboard.OpenWeeklyPlanner(); };
        _tray.SettingsRequested += () => { ShowDashboard(null); Dashboard.OpenSettings(); };
        _tray.ExitRequested += () => Shutdown();

        _scheduler.Start();
    }

    private void ShowDashboard(Core.Scheduling.ScheduleSnapshot? snapshot)
    {
        if (snapshot is not null)
            Dashboard.UpdateSnapshot(snapshot);

        Dashboard.Show();
        if (Dashboard.WindowState == WindowState.Minimized)
            Dashboard.WindowState = WindowState.Normal;
        Dashboard.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _instanceGuard?.Dispose();
        base.OnExit(e);
    }
}
