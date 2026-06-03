using System.Text;
using Dapper;
using CaiViewer.Core.Database;

namespace CaiViewer.Core.Search;

public class SearchService(CaiDbContext db)
{
    public async Task<(IReadOnlyList<SearchResultRow> Rows, int TotalCount)> SearchAsync(SearchQuery query)
    {
        var (whereSql, parameters) = BuildWhere(query);
        var orderSql = BuildOrder(query.Sort);

        var countSql = $"""
            SELECT COUNT(*) FROM v_model_version_latest mv
            {whereSql}
            """;

        var dataSql = $"""
            SELECT
                mv.id AS ModelVersionId,
                mv.civitai_version_id AS CivitaiVersionId,
                mv.model_id AS ModelId,
                mv.model_name AS ModelName,
                mv.model_type AS ModelType,
                mv.name AS VersionName,
                mv.base_model AS BaseModel,
                mv.creator_username AS CreatorUsername,
                mv.nsfw_level AS NsfwLevel,
                mv.primary_zip_path AS PrimaryZipPath,
                mv.published_at AS PublishedAt,
                mv.stat_download_count AS StatDownloadCount,
                mv.stat_rating AS StatRating
            FROM v_model_version_latest mv
            {whereSql}
            ORDER BY {orderSql}
            LIMIT @_pageSize OFFSET @_offset
            """;

        parameters.Add("_pageSize", query.PageSize);
        parameters.Add("_offset", query.Page * query.PageSize);

        await using var conn = db.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = (await conn.QueryAsync<SearchResultRow>(dataSql, parameters)).AsList();

        return (rows, total);
    }

    private static (string Sql, DynamicParameters Params) BuildWhere(SearchQuery query)
    {
        var sb = new StringBuilder();
        var p = new DynamicParameters();
        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.FullText))
        {
            clauses.Add("""
                (mv.id IN (SELECT model_version_id FROM fts_modelversion WHERE fts_modelversion MATCH @fts)
                 OR mv.model_id IN (SELECT model_id FROM fts_model WHERE fts_model MATCH @fts)
                 OR mv.id IN (SELECT model_version_id FROM ModelVersionTrainedWord tw
                              JOIN fts_trainedword ftw ON ftw.trained_word_id = tw.id
                              WHERE fts_trainedword MATCH @fts)
                 OR mv.id IN (SELECT model_version_id FROM ModelVersionImage img
                              JOIN fts_image fi ON fi.image_id = img.id
                              WHERE fts_image MATCH @fts))
                """);
            p.Add("fts", query.FullText.Trim() + "*");
        }

        if (query.Types?.Count > 0)
        {
            clauses.Add($"mv.model_type IN ({InList(p, "type", query.Types)})");
        }

        if (query.BaseModels?.Count > 0)
        {
            clauses.Add($"mv.base_model IN ({InList(p, "bm", query.BaseModels)})");
        }

        if (query.MaxNsfwLevel.HasValue)
        {
            clauses.Add("(mv.nsfw_level IS NULL OR mv.nsfw_level <= @maxNsfw)");
            p.Add("maxNsfw", query.MaxNsfwLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CreatorUsername))
        {
            clauses.Add("mv.creator_username = @creator");
            p.Add("creator", query.CreatorUsername);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            clauses.Add("mv.status = @status");
            p.Add("status", query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Availability))
        {
            clauses.Add("mv.availability = @availability");
            p.Add("availability", query.Availability);
        }

        if (query.PublishedFrom.HasValue)
        {
            clauses.Add("mv.published_at >= @pubFrom");
            p.Add("pubFrom", query.PublishedFrom.Value);
        }

        if (query.PublishedTo.HasValue)
        {
            clauses.Add("mv.published_at <= @pubTo");
            p.Add("pubTo", query.PublishedTo.Value);
        }

        if (query.HasZip == true)
        {
            clauses.Add("mv.primary_zip_path IS NOT NULL");
        }
        else if (query.HasZip == false)
        {
            clauses.Add("mv.primary_zip_path IS NULL");
        }

        if (query.Tags?.Count > 0)
        {
            if (query.TagsMatchAll)
            {
                for (int i = 0; i < query.Tags.Count; i++)
                {
                    var pname = $"tag{i}";
                    clauses.Add($"""
                        mv.model_id IN (
                            SELECT mt.model_id FROM ModelTag mt
                            JOIN Tag t ON t.id = mt.tag_id
                            WHERE t.name = @{pname})
                        """);
                    p.Add(pname, query.Tags[i]);
                }
            }
            else
            {
                var tagParams = InList(p, "tag", query.Tags);
                clauses.Add($"""
                    mv.model_id IN (
                        SELECT mt.model_id FROM ModelTag mt
                        JOIN Tag t ON t.id = mt.tag_id
                        WHERE t.name IN ({tagParams}))
                    """);
            }
        }

        if (clauses.Count > 0)
            sb.Append("WHERE ").AppendJoin(" AND ", clauses);

        return (sb.ToString(), p);
    }

    private static string InList(DynamicParameters p, string prefix, IList<string> values)
    {
        var names = values.Select((v, i) =>
        {
            var name = $"@{prefix}{i}";
            p.Add($"{prefix}{i}", v);
            return name;
        });
        return string.Join(", ", names);
    }

    private static string BuildOrder(SortOption sort) => sort switch
    {
        SortOption.NameDesc       => "mv.model_name DESC",
        SortOption.NewestFirst    => "mv.published_at DESC",
        SortOption.OldestFirst    => "mv.published_at ASC",
        SortOption.MostDownloaded => "mv.stat_download_count DESC",
        SortOption.HighestRated   => "mv.stat_rating DESC",
        SortOption.NsfwLevelAsc   => "mv.nsfw_level ASC",
        _                         => "mv.model_name ASC"
    };
}
