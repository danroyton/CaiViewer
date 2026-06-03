namespace CaiViewer.Core.Database;

internal static class Schema
{
    public const string Ddl = """
        CREATE TABLE IF NOT EXISTS Creator (
            username TEXT PRIMARY KEY,
            image_url TEXT,
            imported_at DATETIME NOT NULL DEFAULT (datetime('now'))
        );

        CREATE TABLE IF NOT EXISTS Model (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            description TEXT,
            type TEXT NOT NULL,
            nsfw INTEGER NOT NULL DEFAULT 0,
            nsfw_level INTEGER,
            poi INTEGER NOT NULL DEFAULT 0,
            minor INTEGER NOT NULL DEFAULT 0,
            allow_no_credit INTEGER,
            allow_commercial_use TEXT,
            allow_derivatives INTEGER,
            allow_different_license INTEGER,
            cosmetic TEXT,
            creator_username TEXT REFERENCES Creator(username),
            stat_download_count INTEGER,
            stat_favorite_count INTEGER,
            stat_thumbs_up INTEGER,
            stat_thumbs_down INTEGER,
            stat_comment_count INTEGER,
            stat_rating_count INTEGER,
            stat_rating REAL,
            stat_tipped_amount INTEGER,
            imported_at DATETIME NOT NULL DEFAULT (datetime('now')),
            updated_at DATETIME
        );

        CREATE INDEX IF NOT EXISTS idx_model_type ON Model(type);
        CREATE INDEX IF NOT EXISTS idx_model_nsfw ON Model(nsfw_level);
        CREATE INDEX IF NOT EXISTS idx_model_creator ON Model(creator_username);

        CREATE TABLE IF NOT EXISTS Tag (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT UNIQUE NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ModelTag (
            model_id INTEGER NOT NULL REFERENCES Model(id),
            tag_id   INTEGER NOT NULL REFERENCES Tag(id),
            PRIMARY KEY (model_id, tag_id)
        );

        CREATE TABLE IF NOT EXISTS ModelVersion (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            civitai_version_id INTEGER NOT NULL,
            snapshot_index INTEGER NOT NULL DEFAULT 0,
            model_id INTEGER NOT NULL REFERENCES Model(id),
            index_nr INTEGER,
            name TEXT NOT NULL,
            status TEXT,
            availability TEXT,
            base_model TEXT,
            base_model_type TEXT,
            nsfw_level INTEGER,
            description TEXT,
            upload_type TEXT,
            air TEXT,
            download_url TEXT,
            training_status TEXT,
            training_details TEXT,
            early_access_ends_at DATETIME,
            created_at DATETIME,
            updated_at DATETIME,
            published_at DATETIME,
            stat_download_count INTEGER,
            stat_thumbs_up INTEGER,
            stat_rating_count INTEGER,
            stat_rating REAL,
            imported_at DATETIME NOT NULL DEFAULT (datetime('now')),
            UNIQUE(civitai_version_id, snapshot_index)
        );

        CREATE INDEX IF NOT EXISTS idx_mv_civitai_id ON ModelVersion(civitai_version_id);
        CREATE INDEX IF NOT EXISTS idx_mv_model_id   ON ModelVersion(model_id);
        CREATE INDEX IF NOT EXISTS idx_mv_base_model ON ModelVersion(base_model);
        CREATE INDEX IF NOT EXISTS idx_mv_status     ON ModelVersion(status);
        CREATE INDEX IF NOT EXISTS idx_mv_nsfw       ON ModelVersion(nsfw_level);
        CREATE INDEX IF NOT EXISTS idx_mv_published  ON ModelVersion(published_at);

        CREATE TABLE IF NOT EXISTS ModelVersionFile (
            id INTEGER PRIMARY KEY,
            model_version_id INTEGER NOT NULL REFERENCES ModelVersion(id),
            name TEXT NOT NULL,
            size_kb REAL,
            type TEXT,
            primary_file INTEGER NOT NULL DEFAULT 0,
            format TEXT,
            size_class TEXT,
            fp TEXT,
            download_url TEXT,
            hash_autov1 TEXT,
            hash_autov2 TEXT,
            hash_autov3 TEXT,
            hash_sha256 TEXT,
            hash_crc32 TEXT,
            hash_blake3 TEXT,
            pickle_scan_result TEXT,
            virus_scan_result TEXT,
            scanned_at DATETIME,
            local_path TEXT
        );

        CREATE INDEX IF NOT EXISTS idx_mvf_version_id ON ModelVersionFile(model_version_id);
        CREATE INDEX IF NOT EXISTS idx_mvf_autov2     ON ModelVersionFile(hash_autov2);
        CREATE INDEX IF NOT EXISTS idx_mvf_autov3     ON ModelVersionFile(hash_autov3);
        CREATE INDEX IF NOT EXISTS idx_mvf_sha256     ON ModelVersionFile(hash_sha256);
        CREATE INDEX IF NOT EXISTS idx_mvf_primary    ON ModelVersionFile(primary_file);

        CREATE TABLE IF NOT EXISTS ModelVersionImage (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            model_version_id INTEGER NOT NULL REFERENCES ModelVersion(id),
            sort_index INTEGER NOT NULL DEFAULT 0,
            url TEXT NOT NULL,
            type TEXT,
            nsfw_level INTEGER,
            width INTEGER,
            height INTEGER,
            hash TEXT,
            has_meta INTEGER,
            on_site INTEGER,
            availability TEXT,
            prompt TEXT,
            local_path TEXT,
            local_filename TEXT
        );

        CREATE INDEX IF NOT EXISTS idx_mvi_version_id ON ModelVersionImage(model_version_id);
        CREATE INDEX IF NOT EXISTS idx_mvi_nsfw        ON ModelVersionImage(nsfw_level);

        CREATE TABLE IF NOT EXISTS ModelVersionTrainedWord (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            model_version_id INTEGER NOT NULL REFERENCES ModelVersion(id),
            word TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_mvtw_version_id ON ModelVersionTrainedWord(model_version_id);
        CREATE INDEX IF NOT EXISTS idx_mvtw_word        ON ModelVersionTrainedWord(word);

        CREATE TABLE IF NOT EXISTS ZipLocation (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            model_version_id INTEGER NOT NULL REFERENCES ModelVersion(id),
            zip_path TEXT NOT NULL,
            zip_sha256 TEXT,
            content_hash TEXT NOT NULL,
            is_primary INTEGER NOT NULL DEFAULT 0,
            discovered_at DATETIME NOT NULL DEFAULT (datetime('now')),
            last_seen_at  DATETIME NOT NULL DEFAULT (datetime('now')),
            UNIQUE(model_version_id, zip_path)
        );

        CREATE INDEX IF NOT EXISTS idx_zl_version ON ZipLocation(model_version_id);
        CREATE INDEX IF NOT EXISTS idx_zl_path    ON ZipLocation(zip_path);
        CREATE INDEX IF NOT EXISTS idx_zl_hash    ON ZipLocation(content_hash);

        CREATE TABLE IF NOT EXISTS _schema_version (
            version INTEGER PRIMARY KEY
        );
        INSERT OR IGNORE INTO _schema_version(version) VALUES(1);

        CREATE VIRTUAL TABLE IF NOT EXISTS fts_model USING fts5(
            model_id UNINDEXED,
            name,
            description,
            content='Model',
            content_rowid='id'
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS fts_modelversion USING fts5(
            model_version_id UNINDEXED,
            name,
            description,
            content='ModelVersion',
            content_rowid='id'
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS fts_image USING fts5(
            image_id UNINDEXED,
            prompt,
            content='ModelVersionImage',
            content_rowid='id'
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS fts_trainedword USING fts5(
            trained_word_id UNINDEXED,
            word,
            content='ModelVersionTrainedWord',
            content_rowid='id'
        );

        CREATE VIEW IF NOT EXISTS v_model_version_latest AS
        SELECT mv.*, m.name AS model_name, m.type AS model_type,
               m.nsfw AS model_nsfw, m.creator_username,
               zl.zip_path AS primary_zip_path
        FROM ModelVersion mv
        JOIN Model m ON m.id = mv.model_id
        LEFT JOIN ZipLocation zl ON zl.model_version_id = mv.id AND zl.is_primary = 1
        WHERE mv.snapshot_index = (
            SELECT MAX(snapshot_index) FROM ModelVersion mv2
            WHERE mv2.civitai_version_id = mv.civitai_version_id
        );
        """;
}
