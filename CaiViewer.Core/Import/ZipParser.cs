using System.IO.Compression;
using System.Text.Json;
using CaiViewer.Core.Import.Dto;

namespace CaiViewer.Core.Import;

public static class ZipParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ParseResult?> ParseAsync(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);

            var modelEntry = FindEntry(archive, e =>
                e.Name.Contains(".cai.model.") && e.Name.EndsWith(".json") && !e.Name.Contains(".v."));
            var versionEntry = FindEntry(archive, e =>
                e.Name.Contains(".cai.model.") && e.Name.EndsWith(".json") && e.Name.Contains(".v."));

            if (modelEntry is null || versionEntry is null)
                return null;

            var modelJson = await ReadEntryAsync(modelEntry);
            var versionJson = await ReadEntryAsync(versionEntry);

            var modelDto = JsonSerializer.Deserialize<RootModel>(modelJson, JsonOptions);
            var versionDto = JsonSerializer.Deserialize<RootModelVersion>(versionJson, JsonOptions);

            if (modelDto is null || versionDto is null)
                return null;

            var imageCount = archive.Entries.Count(e =>
            {
                var name = e.Name;
                return (name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                    && !name.EndsWith("_preview.webp", StringComparison.OrdinalIgnoreCase);
            });

            var contentHash = ContentHasher.Compute(modelJson, versionJson);

            return new ParseResult
            {
                ModelDto = modelDto,
                VersionDto = versionDto,
                ImageCount = imageCount,
                ContentHash = contentHash,
                ModelJson = modelJson,
                VersionJson = versionJson
            };
        }
        catch
        {
            return null;
        }
    }

    private static ZipArchiveEntry? FindEntry(ZipArchive archive, Func<ZipArchiveEntry, bool> predicate)
        => archive.Entries.FirstOrDefault(predicate);

    private static async Task<string> ReadEntryAsync(ZipArchiveEntry entry)
    {
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
