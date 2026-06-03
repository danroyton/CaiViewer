using Dapper;
using CaiViewer.Core.Domain;

namespace CaiViewer.Core.Database.Repositories;

public class ZipLocationRepository(CaiDbContext db)
{
    public async Task<ZipLocation?> GetAsync(int modelVersionId, string zipPath)
    {
        await using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ZipLocation>("""
            SELECT * FROM ZipLocation
            WHERE model_version_id = @modelVersionId AND zip_path = @zipPath
            """, new { modelVersionId, zipPath });
    }

    public async Task InsertAsync(ZipLocation location)
    {
        await using var conn = db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT OR IGNORE INTO ZipLocation(
                model_version_id, zip_path, zip_sha256, content_hash, is_primary,
                discovered_at, last_seen_at)
            VALUES(
                @ModelVersionId, @ZipPath, @ZipSha256, @ContentHash, @IsPrimary,
                @DiscoveredAt, @LastSeenAt)
            """, location);
    }

    public async Task TouchAsync(int modelVersionId, string zipPath)
    {
        await using var conn = db.CreateConnection();
        await conn.ExecuteAsync("""
            UPDATE ZipLocation SET last_seen_at = datetime('now')
            WHERE model_version_id = @modelVersionId AND zip_path = @zipPath
            """, new { modelVersionId, zipPath });
    }
}
