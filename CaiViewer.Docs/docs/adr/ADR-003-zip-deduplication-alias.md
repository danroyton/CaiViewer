# ADR-003: ZIP Deduplication – Alias Strategy (1:N ZipLocation)
## Status: Accepted
## Date: 2025-08

---

## Context

The same physical AI model file (same CivitAI `modelVersionId`) can exist as multiple ZIP archives in the local file system:
- Duplicate downloads placed in different directories
- A model re-scanned after metadata changed on CivitAI (stats, description, new trained words) produces a new ZIP with the same `modelVersionId` but different JSON content
- Renamed or moved ZIPs

The original `SPEC_DATABASE.md` placed a single `zip_path` column on `ModelVersion`. This is insufficient because:
1. It models a 1:1 relationship between a version and a file location
2. It cannot track that a version's metadata changed over time (no history)
3. It cannot represent the same content stored in two places

## Decision

Remove `zip_path` from `ModelVersion` and introduce a dedicated **`ZipLocation`** table, giving a **1:N relationship** between a `ModelVersion` snapshot and its physical ZIP files.
The zip_path is always relative to one or more base paths. the base paths are global to the viewer and can be set and changed on startup (e.g. if the network drive letters changes). A zip file is searched for in all given base paths.

### Deduplication Rules

On import of a ZIP file at path `P`:

| Condition | Action |
|---|---|
| `modelVersionId` not in DB | Full insert (Model + ModelVersion + Files + Images + Words) + `ZipLocation(isPrimary=true)` |
| `modelVersionId` in DB, **content hash unchanged**, path `P` not yet known | Insert `ZipLocation(isPrimary=false)` alias only — no other writes |
| `modelVersionId` in DB, **content hash unchanged**, path `P` already known | No-op (skip entirely) |
| `modelVersionId` in DB, **content hash changed** | Insert new `ModelVersion` row (snapshot with incremented `snapshot_index`) + `ZipLocation(isPrimary=true)` for this snapshot |

**Content hash** = SHA-256 of `concat(model_meta_json, modelversion_meta_json)` — computed from the raw bytes of the two JSON files inside the ZIP.

### ZipLocation Table

```sql
CREATE TABLE ZipLocation (
	id                  INTEGER PRIMARY KEY AUTOINCREMENT,
	model_version_id    INTEGER NOT NULL REFERENCES ModelVersion(id),
	zip_path            TEXT    NOT NULL,
	zip_sha256          TEXT,           -- SHA-256 of the whole ZIP file (optional, for integrity)
	content_hash        TEXT    NOT NULL, -- SHA-256(model_json || version_json)
	is_primary          INTEGER NOT NULL DEFAULT 0,
	discovered_at       DATETIME NOT NULL DEFAULT (datetime('now')),
	last_seen_at        DATETIME NOT NULL DEFAULT (datetime('now')),
	UNIQUE(model_version_id, zip_path)
);
CREATE INDEX idx_zl_version   ON ZipLocation(model_version_id);
CREATE INDEX idx_zl_path      ON ZipLocation(zip_path);
CREATE INDEX idx_zl_hash      ON ZipLocation(content_hash);
```

### ModelVersion snapshot_index

A new `snapshot_index` column (INTEGER DEFAULT 0) is added to `ModelVersion`.  
The first import has `snapshot_index = 0`. Each subsequent changed-content import increments it.  
The combination `(id_civitai, snapshot_index)` is unique.

> `id` (PK) in `ModelVersion` becomes an internal surrogate key.  
> `civitai_version_id` (the original CivitAI integer ID) is stored as a separate column.

## Consequences

- **Positive:** Enables tracking of metadata drift over time (useful for seeing when CivitAI stats or descriptions changed).
- **Positive:** Multiple physical ZIP locations for the same content are safely tracked without data duplication.
- **Positive:** The Import pipeline can safely be re-run on the same directory tree (idempotent).
- **Negative:** `ModelVersion.id` is now a surrogate, not the CivitAI ID. All lookups by CivitAI ID must use `civitai_version_id`. This must be clearly documented and enforced in the repository layer.
- **Mitigation:** `CivitAI_version_id` is indexed and exposed via a `GetLatestSnapshot(civitaiVersionId)` repository method that returns the row with the highest `snapshot_index`.
