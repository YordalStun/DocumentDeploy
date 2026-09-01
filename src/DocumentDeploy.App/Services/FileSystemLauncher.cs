using System.Diagnostics;
using System.IO;

namespace DocumentDeploy.App.Services;

/// <summary>Opens Explorer at a stored path - a file gets its containing folder with itself
/// selected, a folder opens directly. Never reads, writes, or otherwise touches anything;
/// best-effort only, since these paths point at files the app has never verified still exist.</summary>
public static class FileSystemLauncher
{
    public static void OpenInExplorer(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            if (File.Exists(path))
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            else
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch
        {
            // Best effort only - an unreachable path shouldn't crash the app.
        }
    }
}
