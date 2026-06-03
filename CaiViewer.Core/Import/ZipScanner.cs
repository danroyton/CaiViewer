namespace CaiViewer.Core.Import;

public class ImportSummary
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int AliasAdded { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; } = [];
}

public class ZipScanner(ImportService importService)
{
    public async Task<ImportSummary> ScanAsync(
        string baseDirectory,
        bool recursive = true,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var summary = new ImportSummary();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var zips = Directory.EnumerateFiles(baseDirectory, "*.zip", searchOption);

        foreach (var zipPath in zips)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(baseDirectory, zipPath);
            progress?.Report($"Scanning: {relativePath}");

            try
            {
                var parsed = await ZipParser.ParseAsync(zipPath);
                if (parsed is null)
                {
                    summary.Skipped++;
                    progress?.Report($"  Skipped (no metadata): {relativePath}");
                    continue;
                }

                var result = await importService.ImportAsync(parsed, relativePath);
                switch (result.Action)
                {
                    case ImportAction.Inserted:  summary.Inserted++;   break;
                    case ImportAction.Updated:   summary.Updated++;    break;
                    case ImportAction.AliasAdded: summary.AliasAdded++; break;
                    case ImportAction.Skipped:   summary.Skipped++;    break;
                }

                progress?.Report($"  {result.Action}: {relativePath}");
            }
            catch (Exception ex)
            {
                summary.Failed++;
                var msg = $"  ERROR [{relativePath}]: {ex.Message}";
                summary.Errors.Add(msg);
                progress?.Report(msg);
            }
        }

        return summary;
    }
}
