using System.IO;
using DocumentDeploy.Core.Storage;

namespace DocumentDeploy.App.Services;

/// <summary>Writes unhandled-exception details to %AppData%\DocumentDeploy\crash.log so a crash
/// leaves a real trail instead of just Windows' generic "stopped working" dialog.</summary>
public static class CrashLogger
{
    private static string LogPath => Path.Combine(AppPaths.DefaultDataDirectory, "crash.log");

    public static void Log(string context, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DefaultDataDirectory);
            var entry = $"--- {DateTime.Now:yyyy-MM-dd HH:mm:ss} · {context} ---{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(LogPath, entry);
        }
        catch
        {
            // If we can't even write the log, there's nothing more we can safely do here.
        }
    }
}
