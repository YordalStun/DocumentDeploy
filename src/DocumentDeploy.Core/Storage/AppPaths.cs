namespace DocumentDeploy.Core.Storage;

public static class AppPaths
{
    /// <summary>%AppData%\DocumentDeploy - where all local metadata (never documents) lives.</summary>
    public static string DefaultDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocumentDeploy");
}
