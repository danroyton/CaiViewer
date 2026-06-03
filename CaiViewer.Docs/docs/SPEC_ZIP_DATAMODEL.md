# ZIP Output Data Model Specification
## FilescannerCLI – CivitAI Model Archive Format

---

## Overview

Each processed model version produces one ZIP archive file.  
The archive name follows the pattern:

```
<ModelName>_<ModelVersionName>_<ModelVersionId>.zip
```

The archive bundles three categories of content:

1. Model-level metadata (JSON)
2. Model-version-level metadata (JSON)
3. Preview images (binary, JPEG/PNG/WEBP)

---

## Archive Directory Structure

```
<archive>.zip
│
├── <name>.safetensors.cai.model.<model_id>.json               # Full RootModel metadata (CivitAI /models/{id} response)
├── <name>.safetensors.cai.model.<model_id>.v.<modelversion_id>.json        # Full RootModelVersion metadata (CivitAI /model-versions/{id} response)
├── <name>.safetensors.cai.<model_id>v.<modelversion_id>.AUTOV3.<shorthash>.txt               # (optional) AutoV3 hash file
├── image_urls.txt                # (optional, switch -i) Newline-separated list of all image URLs
├── *_previw.webp                # (optional) Stitched preview image from CivitAI metadata
├── <name>.safetensors.cai.<model_id>.v.<modelversion_id>_<index>_<some number>.<ext>              # Preview images or vids, zero-padded index
├── <name>.safetensors.cai.<model_id>.v.<modelversion_id>_<index+1>_<some number>.<ext>
└── ...
```

Example:
Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
-a----        11.08.2025     23:56          92902 eva_kiss_pony.safetensors.cai.904130.v.1011768_0_37483014.jpeg -> Image index 0
-a----        11.08.2025     23:56          76601 eva_kiss_pony.safetensors.cai.904130.v.1011768_1_37483012.jpeg
-a----        11.08.2025     23:56         101567 eva_kiss_pony.safetensors.cai.904130.v.1011768_2_37483021.jpeg
-a----        11.08.2025     23:56         107887 eva_kiss_pony.safetensors.cai.904130.v.1011768_3_37483019.jpeg
-a----        11.08.2025     23:56          73293 eva_kiss_pony.safetensors.cai.904130.v.1011768_4_37483018.jpeg
-a----        11.08.2025     23:56         111092 eva_kiss_pony.safetensors.cai.904130.v.1011768_5_37483020.jpeg
-a----        11.08.2025     23:56          84428 eva_kiss_pony.safetensors.cai.904130.v.1011768_6_37483015.jpeg
-a----        11.08.2025     23:56          85674 eva_kiss_pony.safetensors.cai.904130.v.1011768_7_37483013.jpeg
-a----        11.08.2025     23:56         100091 eva_kiss_pony.safetensors.cai.904130.v.1011768_8_37483017.jpeg
-a----        11.08.2025     23:56         101102 eva_kiss_pony.safetensors.cai.904130.v.1011768_9_37483016.jpeg
-a----        11.08.2025     23:56          53106 eva_kiss_pony.safetensors.cai.904130.v.1011768_preview.webp -> Preview image (from CivitAI metadata, stitched together previews of all picutres)
-a----        11.08.2025     23:56           6664 eva_kiss_pony.safetensors.cai.model.904130.json -> Model metadata
-a----        11.08.2025     23:56          17416 eva_kiss_pony.safetensors.cai.model.904130.v.1011768.json -> ModelVersion metadata
-a----        11.08.2025     23:56             84 eva_kiss_pony.safetensors.cai.model.904130v.1011768.AUTOV3.51ebce3c050b.txt -> AUTOV3


---

## File Descriptions

### `model_meta.json`

Serialized `RootModel` object. Contains top-level model data.

| Field | Type | Description |
|---|---|---|
| `id` | int | CivitAI model ID |
| `name` | string | Model name |
| `description` | string (HTML) | Long-form description |
| `type` | string | Model type (e.g. `Checkpoint`, `LORA`, `TextualInversion`, …) |
| `nsfw` | bool | NSFW flag |
| `nsfwLevel` | int | Numeric NSFW level (0–31+) |
| `poi` | bool | Person of interest flag |
| `minor` | bool | Minor-content flag |
| `allowNoCredit` | bool | License: credit required |
| `allowCommercialUse` | string[] | License: commercial use permissions |
| `allowDerivatives` | bool | License: derivatives allowed |
| `allowDifferentLicense` | bool | License: different license for derivatives |
| `cosmetic` | string | Cosmetic/display info |
| `creator.username` | string | Creator username |
| `creator.image` | string (URL) | Creator avatar URL |
| `tags` | string[] | Tag list |
| `stats.downloadCount` | int | Total downloads |
| `stats.favoriteCount` | int | Favorites |
| `stats.thumbsUpCount` | int | Thumbs up |
| `stats.thumbsDownCount` | int | Thumbs down |
| `stats.commentCount` | int | Comments |
| `stats.ratingCount` | int | Rating count |
| `stats.rating` | double | Average rating |
| `stats.tippedAmountCount` | int | Tips received |
| `modelVersions` | ModelVersion[] | List of all known versions (summary only, no images/files) |

---

### `modelversion_meta.json`

Serialized `RootModelVersion` object. Contains version-specific data.

| Field | Type | Description |
|---|---|---|
| `id` | int | CivitAI model version ID |
| `modelId` | int | Parent model ID (foreign key) |
| `name` | string | Version name |
| `createdAt` | DateTime | Creation timestamp |
| `updatedAt` | DateTime | Last update timestamp |
| `publishedAt` | DateTime | Publication timestamp |
| `status` | string | Status (e.g. `Published`) |
| `baseModel` | string | Base model used (e.g. `SD 1.5`, `SDXL 1.0`, `Flux.1 D`) |
| `baseModelType` | string | Base model type qualifier |
| `trainedWords` | string[] | Trigger words / activation tokens |
| `trainingStatus` | string | Training status |
| `trainingDetails` | string | Training detail info |
| `description` | string (HTML) | Version-specific description |
| `uploadType` | string | Upload type |
| `air` | string | AIR identifier string |
| `earlyAccessEndsAt` | string | Early access expiry date |
| `earlyAccessConfig` | string | Early access configuration |
| `downloadUrl` | string (URL) | Primary download URL |
| `model.name` | string | Parent model name (denormalized) |
| `model.type` | string | Parent model type (denormalized) |
| `model.nsfw` | bool | Parent model NSFW flag (denormalized) |
| `model.poi` | bool | Parent model POI flag (denormalized) |
| `stats.downloadCount` | int | Downloads for this version |
| `stats.thumbsUpCount` | int | Thumbs up for this version |
| `stats.ratingCount` | int | Rating count |
| `stats.rating` | double | Average rating |
| `files` | File[] | Associated model files |
| `files[].id` | int | File ID |
| `files[].name` | string | File name |
| `files[].sizeKB` | double | File size in KB |
| `files[].type` | string | File type (e.g. `Model`, `Config`) |
| `files[].primary` | bool | Is this the primary file? |
| `files[].downloadUrl` | string (URL) | File download URL |
| `files[].pickleScanResult` | string | Pickle scan result |
| `files[].virusScanResult` | string | Virus scan result |
| `files[].scannedAt` | DateTime | Scan timestamp |
| `files[].metadata.format` | string | File format (e.g. `SafeTensor`) |
| `files[].metadata.size` | string | Model size class (e.g. `full`, `pruned`) |
| `files[].metadata.fp` | string | Floating point precision (e.g. `fp16`, `fp32`) |
| `files[].hashes.AutoV1` | string | AutoV1 hash |
| `files[].hashes.AutoV2` | string | AutoV2 hash |
| `files[].hashes.AutoV3` | string | AutoV3 hash |
| `files[].hashes.SHA256` | string | SHA-256 hash |
| `files[].hashes.CRC32` | string | CRC-32 hash |
| `files[].hashes.BLAKE3` | string | BLAKE3 hash |
| `images` | Image[] | Preview images metadata |
| `images[].url` | string (URL) | Original image URL |
| `images[].nsfwLevel` | int | Image NSFW level |
| `images[].width` | int | Image width in px |
| `images[].height` | int | Image height in px |
| `images[].hash` | string | Perceptual hash (blurhash) |
| `images[].type` | string | Media type (`image` / `video`) |
| `images[].hasMeta` | bool | Has generation metadata |
| `images[].onSite` | bool | Still available on CivitAI |
| `images[].availability` | string | Availability status |
| `images[].meta.prompt` | string | Generation prompt (if available) |
| `images[].metadata.width` | int | Stored image width |
| `images[].metadata.height` | int | Stored image height |
| `images[].metadata.hash` | string | Stored image hash |

---

### `image_urls.txt` *(optional, enabled by switch `-i`)*

Plain text, one URL per line, in the same order as `images[]` array in `modelversion_meta.json`.

---

### `images\` directory

Binary image files downloaded from CivitAI.  
Naming: `<index padded to 3 digits>_<blurhash>.<ext>`  
Supported extensions: `.jpg`, `.jpeg`, `.png`, `.webp`

---

## Versioning

| Field | Value |
|---|---|
| Format version | 1.0 |
| Source API | CivitAI REST API v1 |
| Producer | FilescannerCLI |
