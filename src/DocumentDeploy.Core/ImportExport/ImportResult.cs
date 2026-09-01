namespace DocumentDeploy.Core.ImportExport;

public sealed class ImportResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public List<string> Warnings { get; } = new();
}
