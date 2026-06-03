using Dapper;
using CaiViewer.Core.Domain;

namespace CaiViewer.Core.Database.Repositories;

public class ModelVersionRepository(CaiDbContext db)
{
    public async Task<ModelVersion?> GetLatestSnapshotAsync(int civitaiVersionId)
    {
        await using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ModelVersion>("""
            SELECT * FROM ModelVersion
            WHERE civitai_version_id = @civitaiVersionId
            ORDER BY snapshot_index DESC
            LIMIT 1
            """, new { civitaiVersionId });
    }

    public async Task<int> InsertAsync(ModelVersion version)
    {
        await using var conn = db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO ModelVersion(
                civitai_version_id, snapshot_index, model_id, index_nr, name, status, availability,
                base_model, base_model_type, nsfw_level, description, upload_type, air, download_url,
                training_status, training_details, early_access_ends_at,
                created_at, updated_at, published_at,
                stat_download_count, stat_thumbs_up, stat_rating_count, stat_rating, imported_at)
            VALUES(
                @CivitaiVersionId, @SnapshotIndex, @ModelId, @IndexNr, @Name, @Status, @Availability,
                @BaseModel, @BaseModelType, @NsfwLevel, @Description, @UploadType, @Air, @DownloadUrl,
                @TrainingStatus, @TrainingDetails, @EarlyAccessEndsAt,
                @CreatedAt, @UpdatedAt, @PublishedAt,
                @StatDownloadCount, @StatThumbsUp, @StatRatingCount, @StatRating, @ImportedAt);
            SELECT last_insert_rowid();
            """, version);
    }

    public async Task InsertFilesAsync(IEnumerable<ModelVersionFile> files)
    {
        await using var conn = db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT OR IGNORE INTO ModelVersionFile(
                id, model_version_id, name, size_kb, type, primary_file, format, size_class, fp,
                download_url, hash_autov1, hash_autov2, hash_autov3, hash_sha256, hash_crc32, hash_blake3,
                pickle_scan_result, virus_scan_result, scanned_at, local_path)
            VALUES(
                @Id, @ModelVersionId, @Name, @SizeKb, @Type, @PrimaryFile, @Format, @SizeClass, @Fp,
                @DownloadUrl, @HashAutov1, @HashAutov2, @HashAutov3, @HashSha256, @HashCrc32, @HashBlake3,
                @PickleScanResult, @VirusScanResult, @ScannedAt, @LocalPath)
            """, files);
    }

    public async Task InsertImagesAsync(IEnumerable<ModelVersionImage> images)
    {
        await using var conn = db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO ModelVersionImage(
                model_version_id, sort_index, url, type, nsfw_level, width, height, hash,
                has_meta, on_site, availability, prompt, local_path, local_filename)
            VALUES(
                @ModelVersionId, @SortIndex, @Url, @Type, @NsfwLevel, @Width, @Height, @Hash,
                @HasMeta, @OnSite, @Availability, @Prompt, @LocalPath, @LocalFilename)
            """, images);
    }

    public async Task InsertTrainedWordsAsync(IEnumerable<ModelVersionTrainedWord> words)
    {
        await using var conn = db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO ModelVersionTrainedWord(model_version_id, word)
            VALUES(@ModelVersionId, @Word)
            """, words);
    }

    public async Task UpdateFtsAsync(int modelVersionId)
    {
        await using var conn = db.CreateConnection();
        var mv = await conn.QuerySingleOrDefaultAsync<ModelVersion>(
            "SELECT * FROM ModelVersion WHERE id = @id", new { id = modelVersionId });
        if (mv is null) return;

        await conn.ExecuteAsync("""
            INSERT INTO fts_modelversion(model_version_id, name, description)
            VALUES(@ModelVersionId, @Name, @Description)
            """, new { ModelVersionId = modelVersionId, mv.Name, mv.Description });

        var words = await conn.QueryAsync<ModelVersionTrainedWord>(
            "SELECT * FROM ModelVersionTrainedWord WHERE model_version_id = @id", new { id = modelVersionId });
        foreach (var w in words)
        {
            await conn.ExecuteAsync("""
                INSERT INTO fts_trainedword(trained_word_id, word)
                VALUES(@Id, @Word)
                """, w);
        }

        var imgs = await conn.QueryAsync<ModelVersionImage>(
            "SELECT * FROM ModelVersionImage WHERE model_version_id = @id AND prompt IS NOT NULL", new { id = modelVersionId });
        foreach (var img in imgs)
        {
            await conn.ExecuteAsync("""
                INSERT INTO fts_image(image_id, prompt)
                VALUES(@Id, @Prompt)
                """, img);
        }
    }
}
