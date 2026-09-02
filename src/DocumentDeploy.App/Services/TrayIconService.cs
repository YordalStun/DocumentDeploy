using System.Windows.Forms;
using System.Windows.Threading;

namespace DocumentDeploy.App.Services;

/// <summary>Wraps the WinForms NotifyIcon (WPF has no tray icon of its own) with the app's menu.</summary>
public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _icon;

    public event Action? OpenRequested;
    public event Action? PlanWeekRequested;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayIconService()
    {
        var iconHandle = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty);

        _icon = new NotifyIcon
        {
            Icon = iconHandle ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "DocumentDeploy",
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => Raise(OpenRequested));
        menu.Items.Add("Plan next week", null, (_, _) => Raise(PlanWeekRequested));
        menu.Items.Add("Settings", null, (_, _) => Raise(SettingsRequested));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Raise(ExitRequested));
        _icon.ContextMenuStrip = menu;

        _icon.DoubleClick += (_, _) => Raise(OpenRequested);
    }

    /// <summary>
    /// Posts the handler to run on a fresh turn of the WPF dispatcher loop instead of invoking
    /// it inline. WinForms' ContextMenuStrip runs its own native menu-tracking loop while a
    /// click is being handled; calling straight into WPF from there (e.g. Window.ShowDialog,
    /// which pushes its own nested dispatcher frame) mixes the two frameworks' modal loops and
    /// can hang or crash the app. Deferring with BeginInvoke lets the tray's loop unwind first.
    /// </summary>
    private static void Raise(Action? handler)
    {
        if (handler is null) return;
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(handler, DispatcherPriority.Normal);
    }

    public void ShowBalloon(string title, string text) =>
        _icon.ShowBalloonTip(4000, title, text, ToolTipIcon.Info);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
