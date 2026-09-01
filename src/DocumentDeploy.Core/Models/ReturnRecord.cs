namespace DocumentDeploy.Core.Models;

/// <summary>
/// Proof that a returned document was filed - the file's name, path and timestamps only.
/// The app verifies the file exists at confirmation time but never opens, reads or copies it.
/// </summary>
public sealed class ReturnRecord
{
    public string ConfirmedFileName { get; set; } = string.Empty;
    public string ConfirmedFilePath { get; set; } = string.Empty;
    public DateTime ConfirmedAtUtc { get; set; }
    public DateTime? FileLastWriteUtc { get; set; }
}
