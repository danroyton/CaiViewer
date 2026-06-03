# Database Schema Specification
## FilescannerCLI – CivitAI Model Repository Database

---

## Design Goals

- Fully searchable by all relevant model and version attributes
- Supports full-text search on description, tags and trained words
- Supports hash-based lookups (AutoV2, AutoV3, SHA256)
- Enables efficient filtering by model type, base model, NSFW level, creator, tags
- 1:N relationship between a ModelVersion snapshot and its physical ZIP file locations (see ADR-003)
- Idempotent import: re-scanning the same directory adds aliases, never duplicates
- Tracks metadata drift: changed JSON content in a ZIP creates a new ModelVersion snapshot
- Normalized to avoid duplication; denormalized columns provided for query convenience
- SQLite-compatible (primary target); also suitable for SQL Server / PostgreSQL

---

## Entity–Relationship Overview

```
Creator ──< Model >──< ModelVersion >──< ModelVersionFile
							  │
							  ├──< ModelVersionImage
							  ├──< ModelVersionTrainedWord
							  └──< ZipLocation

Model >──< ModelTag
Tag ──< ModelTag

Note: ModelVersion.id is a surrogate PK.
	  ModelVersion.civitai_version_id holds the original CivitAI integer ID.
	  Multiple ModelVersion rows with the same civitai_version_id represent
	  different metadata snapshots (snapshot_index 0, 1, 2, …).
```

---

## Tables

---

### `Creator`

Stores CivitAI creator/author profiles.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `username` | TEXT | PK | CivitAI username |
| `image_url` | TEXT | | Avatar URL |
| `imported_at` | DATETIME | NOT NULL DEFAULT NOW | Row insert timestamp |

---

### `Model`

Top-level CivitAI model record.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INTEGER | PK | CivitAI model ID |
| `name` | TEXT | NOT NULL | Model name |
| `description` | TEXT | | Full description (may contain HTML) |
| `type` | TEXT | NOT NULL | Model type: `Checkpoint`, `LORA`, `TextualInversion`, `Hypernetwork`, `AestheticGradient`, `Controlnet`, `Poses`, … |
| `nsfw` | INTEGER | NOT NULL DEFAULT 0 | 0 = SFW, 1 = NSFW |
| `nsfw_level` | INTEGER | | Numeric NSFW level (0–31+) |
| `poi` | INTEGER | NOT NULL DEFAULT 0 | Person of interest flag |
| `minor` | INTEGER | NOT NULL DEFAULT 0 | Minor-content flag |
| `allow_no_credit` | INTEGER | | License: credit not required |
| `allow_commercial_use` | TEXT | | JSON array of commercial use permissions |
| `allow_derivatives` | INTEGER | | License: derivatives allowed |
| `allow_different_license` | INTEGER | | License: different license allowed |
| `cosmetic` | TEXT | | Cosmetic display value |
| `creator_username` | TEXT | FK → Creator.username | Creator |
| `stat_download_count` | INTEGER | | Total downloads |
| `stat_favorite_count` | INTEGER | | Favorites |
| `stat_thumbs_up` | INTEGER | | Thumbs up |
| `stat_thumbs_down` | INTEGER | | Thumbs down |
| `stat_comment_count` | INTEGER | | Comments |
| `stat_rating_count` | INTEGER | | Number of ratings |
| `stat_rating` | REAL | | Average rating |
| `stat_tipped_amount` | INTEGER | | Tips received |
| `imported_at` | DATETIME | NOT NULL DEFAULT NOW | Row insert timestamp |
| `updated_at` | DATETIME | | Last metadata update |

**Indexes:**
- `idx_model_type` on `type`
- `idx_model_nsfw` on `nsfw_level`
- `idx_model_creator` on `creator_username`
- Full-text index on `name`, `description`

---

### `Tag`

Normalised tag dictionary.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INTEGER | PK AUTOINCREMENT | Internal tag ID |
| `name` | TEXT | UNIQUE NOT NULL | Tag text |

---

### `ModelTag`

Many-to-many: Model ↔ Tag.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `model_id` | INTEGER | FK → Model.id | Model |
| `tag_id` | INTEGER | FK → Tag.id | Tag |

**Primary Key:** (`model_id`, `tag_id`)

---

### `ModelVersion`

One version of a model. A model has 1..N versions.
A single CivitAI version ID may have multiple rows here when metadata changed between imports (`snapshot_index` differentiates them).

> **Key change (ADR-003):** `id` is a **surrogate** auto-increment PK. The original CivitAI integer is stored in `civitai_version_id`. Use `GetLatestSnapshot(civitai_version_id)` to retrieve the current state.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INTEGER | PK AUTOINCREMENT | Internal surrogate key |
| `civitai_version_id` | INTEGER | NOT NULL | Original CivitAI model-version ID |
| `snapshot_index` | INTEGER | NOT NULL DEFAULT 0 | 0 = first import; incremented when JSON content changes |
| `model_id` | INTEGER | FK → Model.id NOT NULL | Parent model |
| `index_nr` | INTEGER | | CivitAI version ordering index |
| `name` | TEXT | NOT NULL | Version name |
| `status` | TEXT | | `Published`, `Draft`, … |
| `availability` | TEXT | | `Public`, `EarlyAccess`, … |
| `base_model` | TEXT | | e.g. `SD 1.5`, `SDXL 1.0`, `Flux.1 D` |
| `base_model_type` | TEXT | | Qualifier on the base model |
| `nsfw_level` | INTEGER | | Version-level NSFW level |
| `description` | TEXT | | Version-specific description |
| `upload_type` | TEXT | | Upload type |
| `air` | TEXT | | AIR identifier |
| `download_url` | TEXT | | Primary download URL |
| `training_status` | TEXT | | Training status |
| `training_details` | TEXT | | Training detail text |
| `early_access_ends_at` | DATETIME | | Early access expiry |
| `created_at` | DATETIME | | CivitAI creation date |
| `updated_at` | DATETIME | | CivitAI last update date |
| `published_at` | DATETIME | | CivitAI publish date |
| `stat_download_count` | INTEGER | | Downloads for this version |
| `stat_thumbs_up` | INTEGER | | Thumbs up |
| `stat_rating_count` | INTEGER | | Rating count |
| `stat_rating` | REAL | | Average rating |
| `imported_at` | DATETIME | NOT NULL DEFAULT NOW | Row insert timestamp |

**Indexes:**
- `idx_mv_model_id` on `model_id`
- `idx_mv_civitai_id` on `civitai_version_id`
- `idx_mv_snapshot` on `(civitai_version_id, snapshot_index)`
- `idx_mv_base_model` on `base_model`
- `idx_mv_status` on `status`
- `idx_mv_nsfw` on `nsfw_level`
- `idx_mv_published` on `published_at`
- Full-text index on `name`, `description`

**Unique constraint:** `(civitai_version_id, snapshot_index)`

---

### `ModelVersionFile`

Files attached to a model version (tensor files, configs, …).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INTEGER | PK | CivitAI file ID |
| `model_version_id` | INTEGER | FK → ModelVersion.id NOT NULL | Parent version |
| `name` | TEXT | NOT NULL | File name |
| `size_kb` | REAL | | Size in KB |
| `type` | TEXT | | `Model`, `Config`, `VAE`, … |
| `primary_file` | INTEGER | NOT NULL DEFAULT 0 | 1 = primary file |
| `format` | TEXT | | `SafeTensor`, `PickleTensor`, … |
| `size_class` | TEXT | | `full`, `pruned` |
| `fp` | TEXT | | `fp16`, `fp32`, `bf16` |
| `download_url` | TEXT | | Download URL |
| `hash_autov1` | TEXT | | AutoV1 hash |
| `hash_autov2` | TEXT | UNIQUE | AutoV2 hash (used for lookup) |
| `hash_autov3` | TEXT | UNIQUE | AutoV3 hash (used for lookup) |
| `hash_sha256` | TEXT | | SHA-256 |
| `hash_crc32` | TEXT | | CRC-32 |
| `hash_blake3` | TEXT | | BLAKE3 |
| `pickle_scan_result` | TEXT | | Pickle scan result |
| `virus_scan_result` | TEXT | | Virus scan result |
| `scanned_at` | DATETIME | | Scan timestamp |
| `local_path` | TEXT | | Path to locally downloaded file |

**Indexes:**
- `idx_mvf_version_id` on `model_version_id`
- `idx_mvf_autov2` on `hash_autov2`
- `idx_mvf_autov3` on `hash_autov3`
- `idx_mvf_sha256` on `hash_sha256`
- `idx_mvf_primary` on `primary_file`

---

### `ModelVersionImage`

Preview images for a model version.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INTEGER | PK AUTOINCREMENT | Internal ID |
| `model_version_id` | INTEGER | FK → ModelVersion.id NOT NULL | Parent version |
| `sort_index` | INTEGER | NOT NULL DEFAULT 0 | Display order |
| `url` | TEXT | NOT NULL | Original CivitAI image URL |
| `type` | TEXT | | `image` or `video` |
| `nsfw_level` | INTEGER | | Image NSFW level |
| `width` | INTEGER | | Image width in px |
| `height` | INTEGER | | Image height in px |
| `hash` | TEXT | | Blurhash / perceptual hash |
| `has_meta` | INTEGER | | 1 = has generation metadata |
| `on_site` | INTEGER | | 1 = still available on CivitAI |
| `availability` | TEXT | | Availability status |
| `prompt` | TEXT | | Generation prompt (if available) |
| `local_path` | TEXT | | Path to locally downloaded image |
| `local_filename` | TEXT | | Filename within ZIP |

**Indexes:**
- `idx_mvi_version_id` on `model_version_id`
- `idx_mvi_nsfw` on `nsfw_level`
- Full-text index on `prompt`

---

### `ModelVersionTrainedWord`

Normalized trigger words / activation tokens per version.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INTEGER | PK AUTOINCREMENT | Internal ID |
| `model_version_id` | INTEGER | FK → ModelVersion.id NOT NULL | Parent version (surrogate) |
| `word` | TEXT | NOT NULL | Trigger word |

**Indexes:**
- `idx_mvtw_version_id` on `model_version_id`
- `idx_mvtw_word` on `word`

---

### `ZipLocation`

Records every physical ZIP file that contains data for a specific `ModelVersion` snapshot.  
Implements the **1:N alias strategy** (see ADR-003).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | INTEGER | PK AUTOINCREMENT | Internal ID |
| `model_version_id` | INTEGER | FK → ModelVersion.id NOT NULL | Parent version snapshot (surrogate) |
| `zip_path` | TEXT | NOT NULL | Absolute path to the ZIP file |
| `zip_sha256` | TEXT | | SHA-256 of the entire ZIP file (optional integrity check) |
| `content_hash` | TEXT | NOT NULL | SHA-256(model_json \|\| version_json) – used for change detection |
| `is_primary` | INTEGER | NOT NULL DEFAULT 0 | 1 = first/canonical location for this snapshot |
| `discovered_at` | DATETIME | NOT NULL DEFAULT NOW | When this path was first seen |
| `last_seen_at` | DATETIME | NOT NULL DEFAULT NOW | Updated on each import run (staleness detection) |

**Unique constraint:** `(model_version_id, zip_path)`

**Indexes:**
- `idx_zl_version` on `model_version_id`
- `idx_zl_path` on `zip_path`
- `idx_zl_hash` on `content_hash`

**Deduplication rules applied during import:**

| Situation | Action |
|---|---|
| `civitai_version_id` not in DB | Full insert + `ZipLocation(is_primary=1)` |
| Same version, same `content_hash`, path not yet known | Insert `ZipLocation` alias only |
| Same version, same `content_hash`, path already known | No-op (update `last_seen_at` only) |
| Same version, **different** `content_hash` | Insert new `ModelVersion` (snapshot_index + 1) + `ZipLocation(is_primary=1)` |

---

## Suggested Views

### `v_model_version_latest`

Joins `ModelVersion` (latest snapshot per `civitai_version_id`), `Model`, `Creator` and the primary `ZipLocation`.  
Provides a flat search-friendly row per CivitAI version ID including denormalized model name, type, creator, and primary ZIP path.

```sql
CREATE VIEW v_model_version_latest AS
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
```

### `v_model_summary`

Joins `Model`, `Creator`, aggregated tag list, and the latest primary ZIP path of the most recent version for a quick model overview.

---

## Full-Text Search

Use SQLite FTS5 virtual tables (or equivalent) on the following columns:

| FTS Table | Source Columns |
|---|---|
| `fts_model` | `Model.name`, `Model.description` |
| `fts_modelversion` | `ModelVersion.name`, `ModelVersion.description` |
| `fts_image` | `ModelVersionImage.prompt` |
| `fts_trainedword` | `ModelVersionTrainedWord.word` |

---

## DDL Summary (SQLite)

```sql
CREATE TABLE Creator (
	username TEXT PRIMARY KEY,
	image_url TEXT,
	imported_at DATETIME NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE Model (
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

CREATE TABLE Tag (
	id INTEGER PRIMARY KEY AUTOINCREMENT,
	name TEXT UNIQUE NOT NULL
);

CREATE TABLE ModelTag (
	model_id INTEGER NOT NULL REFERENCES Model(id),
	tag_id   INTEGER NOT NULL REFERENCES Tag(id),
	PRIMARY KEY (model_id, tag_id)
);

CREATE TABLE ModelVersion (
	id INTEGER PRIMARY KEY AUTOINCREMENT,  -- surrogate key
	civitai_version_id INTEGER NOT NULL,   -- original CivitAI model-version ID
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

CREATE INDEX idx_mv_civitai_id ON ModelVersion(civitai_version_id);
CREATE INDEX idx_mv_model_id   ON ModelVersion(model_id);
CREATE INDEX idx_mv_published  ON ModelVersion(published_at);

CREATE TABLE ModelVersionFile (
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
	hash_autov2 TEXT UNIQUE,
	hash_autov3 TEXT UNIQUE,
	hash_sha256 TEXT,
	hash_crc32 TEXT,
	hash_blake3 TEXT,
	pickle_scan_result TEXT,
	virus_scan_result TEXT,
	scanned_at DATETIME,
	local_path TEXT
);

CREATE TABLE ModelVersionImage (
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

CREATE TABLE ModelVersionTrainedWord (
	id INTEGER PRIMARY KEY AUTOINCREMENT,
	model_version_id INTEGER NOT NULL REFERENCES ModelVersion(id),
	word TEXT NOT NULL
);

CREATE TABLE ZipLocation (
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

CREATE INDEX idx_zl_version ON ZipLocation(model_version_id);
CREATE INDEX idx_zl_path    ON ZipLocation(zip_path);
CREATE INDEX idx_zl_hash    ON ZipLocation(content_hash);
```
