using Dapper;
using CaiViewer.Core.Domain;

namespace CaiViewer.Core.Database.Repositories;

public class ModelRepository(CaiDbContext db)
{
    public async Task UpsertCreatorAsync(Creator creator)
    {
        await using var conn = db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO Creator(username, image_url)
            VALUES(@Username, @ImageUrl)
            ON CONFLICT(username) DO UPDATE SET image_url = excluded.image_url
            """, creator);
    }

    public async Task<Domain.Model?> GetByIdAsync(int id)
    {
        await using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Domain.Model>(
            "SELECT * FROM Model WHERE id = @id", new { id });
    }

    public async Task InsertOrIgnoreAsync(Domain.Model model)
    {
        await using var conn = db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT OR IGNORE INTO Model(
                id, name, description, type, nsfw, nsfw_level, poi, minor,
                allow_no_credit, allow_commercial_use, allow_derivatives, allow_different_license,
                cosmetic, creator_username,
                stat_download_count, stat_favorite_count, stat_thumbs_up, stat_thumbs_down,
                stat_comment_count, stat_rating_count, stat_rating, stat_tipped_amount,
                imported_at, updated_at)
            VALUES(
                @Id, @Name, @Description, @Type, @Nsfw, @NsfwLevel, @Poi, @Minor,
                @AllowNoCredit, @AllowCommercialUse, @AllowDerivatives, @AllowDifferentLicense,
                @Cosmetic, @CreatorUsername,
                @StatDownloadCount, @StatFavoriteCount, @StatThumbsUp, @StatThumbsDown,
                @StatCommentCount, @StatRatingCount, @StatRating, @StatTippedAmount,
                @ImportedAt, @UpdatedAt)
            """, model);
    }

    public async Task UpsertTagsAsync(int modelId, IEnumerable<string> tags)
    {
        await using var conn = db.CreateConnection();
        await conn.OpenAsync();
        foreach (var tag in tags)
        {
            await conn.ExecuteAsync(
                "INSERT OR IGNORE INTO Tag(name) VALUES(@name)", new { name = tag });
            var tagId = await conn.ExecuteScalarAsync<int>(
                "SELECT id FROM Tag WHERE name = @name", new { name = tag });
            await conn.ExecuteAsync(
                "INSERT OR IGNORE INTO ModelTag(model_id, tag_id) VALUES(@modelId, @tagId)",
                new { modelId, tagId });
        }
    }
}
