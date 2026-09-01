using DocumentDeploy.Core.Models;

namespace DocumentDeploy.Core.Filing;

/// <summary>
/// Files a document you hand the app (drag-and-drop or a picker) into the folder a
/// DocumentNeed says it belongs in. Always copies - the original file, wherever it came from,
/// is never opened, moved or deleted. A name clash at the destination is never overwritten;
/// the copy is renamed instead.
/// </summary>
public static class DocumentFilingService
{
    public static ReturnRecord FileAway(string sourceFilePath, string destinationFolder)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("The selected file could not be found.", sourceFilePath);

        Directory.CreateDirectory(destinationFolder);

        var fileName = Path.GetFileName(sourceFilePath);
        var destPath = Path.Combine(destinationFolder, fileName);

        if (File.Exists(destPath))
        {
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var suffix = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            destPath = Path.Combine(destinationFolder, $"{stem}_{suffix}{ext}");
        }

        File.Copy(sourceFilePath, destPath, overwrite: false);

        return new ReturnRecord
        {
            ConfirmedFileName = Path.GetFileName(destPath),
            ConfirmedFilePath = destPath,
            ConfirmedAtUtc = DateTime.UtcNow,
            FileLastWriteUtc = File.GetLastWriteTimeUtc(destPath),
        };
    }
}
