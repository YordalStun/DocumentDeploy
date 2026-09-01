using DocumentDeploy.Core.Filing;

namespace DocumentDeploy.Tests;

public class DocumentFilingServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dd-filing-tests-" + Guid.NewGuid());

    public DocumentFilingServiceTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Copies_the_file_into_a_new_destination_folder_and_leaves_the_original_untouched()
    {
        var source = Path.Combine(_root, "source", "signed-form.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllText(source, "pretend pdf bytes");
        var destinationFolder = Path.Combine(_root, "dest", "does", "not", "exist", "yet");

        var record = DocumentFilingService.FileAway(source, destinationFolder);

        Assert.True(File.Exists(source), "the original must never be deleted");
        Assert.True(File.Exists(record.ConfirmedFilePath));
        Assert.Equal("signed-form.pdf", record.ConfirmedFileName);
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(record.ConfirmedFilePath));
    }

    [Fact]
    public void A_name_clash_at_the_destination_is_renamed_rather_than_overwritten()
    {
        var source = Path.Combine(_root, "signed-form.pdf");
        File.WriteAllText(source, "new content");
        var destinationFolder = Path.Combine(_root, "dest");
        Directory.CreateDirectory(destinationFolder);
        var existingPath = Path.Combine(destinationFolder, "signed-form.pdf");
        File.WriteAllText(existingPath, "old content - must survive");

        var record = DocumentFilingService.FileAway(source, destinationFolder);

        Assert.Equal("old content - must survive", File.ReadAllText(existingPath));
        Assert.NotEqual(existingPath, record.ConfirmedFilePath);
        Assert.Equal("new content", File.ReadAllText(record.ConfirmedFilePath));
    }

    [Fact]
    public void Throws_when_the_source_file_does_not_exist()
    {
        var missing = Path.Combine(_root, "nope.pdf");
        Assert.Throws<FileNotFoundException>(() => DocumentFilingService.FileAway(missing, Path.Combine(_root, "dest")));
    }
}
