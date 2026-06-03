using System.Text.Json;
using CaiViewer.Core.Database;
using CaiViewer.Core.Database.Repositories;
using CaiViewer.Core.Domain;
using CaiViewer.Core.Import.Dto;

namespace CaiViewer.Core.Import;

public class ImportService(CaiDbContext db)
{
    private readonly ModelRepository _models = new(db);
    private readonly ModelVersionRepository _versions = new(db);
    private readonly ZipLocationRepository _zips = new(db);

    public async Task<ImportResult> ImportAsync(ParseResult parsed, string relativePath)
    {
        var m = parsed.ModelDto;
        var v = parsed.VersionDto;

        // Upsert Creator
        if (m.Creator is not null)
        {
            await _models.UpsertCreatorAsync(new Creator
            {
                Username = m.Creator.Username,
                ImageUrl = m.Creator.Image
            });
        }

        // Insert Model (idempotent)
        var model = MapModel(m);
        await _models.InsertOrIgnoreAsync(model);

        // Update FTS for model if it's the first insert
        // (content-based FTS — insert will be no-op duplicate otherwise via OR IGNORE on Model)

        // Tags
        if (m.Tags?.Count > 0)
            await _models.UpsertTagsAsync(m.Id, m.Tags);

        // Look up latest snapshot
        var existing = await _versions.GetLatestSnapshotAsync(v.Id);

        if (existing is null)
        {
            // Full insert
            var mvId = await InsertVersionAsync(v, m.Id, 0);
            await _zips.InsertAsync(new ZipLocation
            {
                ModelVersionId = mvId,
                ZipPath = relativePath,
                ContentHash = parsed.ContentHash,
                IsPrimary = 1
            });
            await _versions.UpdateFtsAsync(mvId);
            return new ImportResult(ImportAction.Inserted, mvId);
        }

        // Same content hash?
        if (existing.Id != 0)
        {
            var existingZip = await _zips.GetAsync(existing.Id, relativePath);
            if (existingZip is not null)
            {
                // Already known — touch
                await _zips.TouchAsync(existing.Id, relativePath);
                return new ImportResult(ImportAction.Skipped, existing.Id);
            }

            // Check if content changed
            // We need to compare against the hash stored for this snapshot's primary zip
            var primaryZip = await GetPrimaryZipAsync(existing.Id);
            if (primaryZip?.ContentHash == parsed.ContentHash)
            {
                // Same content, new alias
                await _zips.InsertAsync(new ZipLocation
                {
                    ModelVersionId = existing.Id,
                    ZipPath = relativePath,
                    ContentHash = parsed.ContentHash,
                    IsPrimary = 0
                });
                return new ImportResult(ImportAction.AliasAdded, existing.Id);
            }
            else
            {
                // Content changed → new snapshot
                var newSnapshot = existing.SnapshotIndex + 1;
                var mvId = await InsertVersionAsync(v, m.Id, newSnapshot);
                await _zips.InsertAsync(new ZipLocation
                {
                    ModelVersionId = mvId,
                    ZipPath = relativePath,
                    ContentHash = parsed.ContentHash,
                    IsPrimary = 1
                });
                await _versions.UpdateFtsAsync(mvId);
                return new ImportResult(ImportAction.Updated, mvId);
            }
        }

        return new ImportResult(ImportAction.Skipped, existing.Id);
    }

    private async Task<int> InsertVersionAsync(RootModelVersion v, int modelId, int snapshotIndex)
    {
        var mv = new ModelVersion
        {
            CivitaiVersionId = v.Id,
            SnapshotIndex = snapshotIndex,
            ModelId = modelId,
            Name = v.Name,
            Status = v.Status,
            Availability = v.Availability,
            BaseModel = v.BaseModel,
            BaseModelType = v.BaseModelType,
            NsfwLevel = v.NsfwLevel,
            Description = v.Description,
            UploadType = v.UploadType,
            Air = v.Air,
            DownloadUrl = v.DownloadUrl,
            TrainingStatus = v.TrainingStatus,
            TrainingDetails = v.TrainingDetails,
            EarlyAccessEndsAt = ParseDate(v.EarlyAccessEndsAt),
            CreatedAt = v.CreatedAt,
            UpdatedAt = v.UpdatedAt,
            PublishedAt = v.PublishedAt,
            StatDownloadCount = v.Stats?.DownloadCount,
            StatThumbsUp = v.Stats?.ThumbsUpCount,
            StatRatingCount = v.Stats?.RatingCount,
            StatRating = v.Stats?.Rating
        };

        var mvId = await _versions.InsertAsync(mv);

        if (v.Files is not null)
        {
            var files = v.Files.Select(f => new ModelVersionFile
            {
                Id = f.Id,
                ModelVersionId = mvId,
                Name = f.Name,
                SizeKb = f.SizeKb,
                Type = f.Type,
                PrimaryFile = f.Primary ? 1 : 0,
                Format = f.Metadata?.Format,
                SizeClass = f.Metadata?.Size,
                Fp = f.Metadata?.Fp,
                DownloadUrl = f.DownloadUrl,
                HashAutov1 = f.Hashes?.AutoV1,
                HashAutov2 = f.Hashes?.AutoV2,
                HashAutov3 = f.Hashes?.AutoV3,
                HashSha256 = f.Hashes?.Sha256,
                HashCrc32 = f.Hashes?.Crc32,
                HashBlake3 = f.Hashes?.Blake3,
                PickleScanResult = f.PickleScanResult,
                VirusScanResult = f.VirusScanResult,
                ScannedAt = f.ScannedAt,
                LocalPath = f.LocalPath
            });
            await _versions.InsertFilesAsync(files);
        }

        if (v.Images is not null)
        {
            var images = v.Images.Select((img, idx) => new ModelVersionImage
            {
                ModelVersionId = mvId,
                SortIndex = idx,
                Url = img.Url,
                Type = img.Type,
                NsfwLevel = img.NsfwLevel,
                Width = img.Width,
                Height = img.Height,
                Hash = img.Hash,
                HasMeta = img.HasMeta.HasValue ? (img.HasMeta.Value ? 1 : 0) : null,
                OnSite = img.OnSite.HasValue ? (img.OnSite.Value ? 1 : 0) : null,
                Availability = img.Availability,
                Prompt = img.Meta?.Prompt
            });
            await _versions.InsertImagesAsync(images);
        }

        if (v.TrainedWords?.Count > 0)
        {
            var words = v.TrainedWords.Select(w => new ModelVersionTrainedWord
            {
                ModelVersionId = mvId,
                Word = w
            });
            await _versions.InsertTrainedWordsAsync(words);
        }

        return mvId;
    }

    private async Task<ZipLocation?> GetPrimaryZipAsync(int modelVersionId)
    {
        await using var conn = db.CreateConnection();
        return await Dapper.SqlMapper.QuerySingleOrDefaultAsync<ZipLocation>(conn,
            "SELECT * FROM ZipLocation WHERE model_version_id = @id AND is_primary = 1",
            new { id = modelVersionId });
    }

    private static Domain.Model MapModel(RootModel m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Description = m.Description,
        Type = m.Type,
        Nsfw = m.Nsfw ? 1 : 0,
        NsfwLevel = m.NsfwLevel,
        Poi = m.Poi ? 1 : 0,
        Minor = m.Minor ? 1 : 0,
        AllowNoCredit = m.AllowNoCredit.HasValue ? (m.AllowNoCredit.Value ? 1 : 0) : null,
        AllowCommercialUse = m.AllowCommercialUse?.ToString(),
        AllowDerivatives = m.AllowDerivatives.HasValue ? (m.AllowDerivatives.Value ? 1 : 0) : null,
        AllowDifferentLicense = m.AllowDifferentLicense.HasValue ? (m.AllowDifferentLicense.Value ? 1 : 0) : null,
        Cosmetic = m.Cosmetic,
        CreatorUsername = m.Creator?.Username,
        StatDownloadCount = m.Stats?.DownloadCount,
        StatFavoriteCount = m.Stats?.FavoriteCount,
        StatThumbsUp = m.Stats?.ThumbsUpCount,
        StatThumbsDown = m.Stats?.ThumbsDownCount,
        StatCommentCount = m.Stats?.CommentCount,
        StatRatingCount = m.Stats?.RatingCount,
        StatRating = m.Stats?.Rating,
        StatTippedAmount = m.Stats?.TippedAmountCount
    };

    private static DateTime? ParseDate(string? s)
        => DateTime.TryParse(s, out var d) ? d : null;
}

public enum ImportAction { Inserted, Updated, AliasAdded, Skipped }

public record ImportResult(ImportAction Action, int ModelVersionId);
