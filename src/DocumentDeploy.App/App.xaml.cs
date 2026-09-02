using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
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

        // Built for UK use - force British conventions (dd/MM/yyyy, day/month names, etc.)
        // regardless of the machine's own regional settings. This also sets a real, resolvable
        // culture for WPF's binding engine, which needs one to activate any data binding at all.
        var uk = CultureInfo.GetCultureInfo("en-GB");
        CultureInfo.DefaultThreadCurrentCulture = uk;
        CultureInfo.DefaultThreadCurrentUICulture = uk;
        CultureInfo.CurrentCulture = uk;
        CultureInfo.CurrentUICulture = uk;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(uk.IetfLanguageTag)));

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            CrashLogger.Log("AppDomain.UnhandledException", args.ExceptionObject as Exception ?? new Exception(args.ExceptionObject?.ToString()));

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

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        CrashLogger.Log("DispatcherUnhandledException", e.Exception);

        MessageBox.Show(
            $"Something went wrong, but DocumentDeploy is staying open.\n\n{e.Exception.Message}\n\n" +
            "Details were written to %AppData%\\DocumentDeploy\\crash.log.",
            "DocumentDeploy", MessageBoxButton.OK, MessageBoxImage.Warning);

        // Keep the app alive where we can - it's a background tray app, and losing it entirely
        // over one bad interaction (and taking the tray icon down with it) is worse than a
        // logged, recoverable hiccup.
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _instanceGuard?.Dispose();
        base.OnExit(e);
    }
}
