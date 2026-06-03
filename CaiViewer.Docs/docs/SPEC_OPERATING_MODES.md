# Operating Modes Specification
## FilescannerCLI – Switches, Input Sources and API Modes

---

## Synopsis

```
FilescannerCLI.exe <INPUT> -[MODE][OPTIONS] [PARAM:VALUE ...]
```

---

## Positional Argument: `<INPUT>`

The first argument selects the input source. It can be one of:

| Value | Meaning |
|---|---|
| `<path>.safetensors` | Single local SafeTensor file to inspect or use as source for hash lookup |
| `InfileList.txt` | Text file containing one SafeTensor file path per line |
| `InAutoV2List.txt` | Text file containing one AutoV2 / AutoV3 hash per line |
| `InUrlList.txt` | Text file containing one CivitAI model URL or model-version ID per line |
| `<AutoV2/V3 hash>` | Inline hash (used together with `-A<hash>:`) |
| `<ModelVersionId>` | Inline model version ID (used together with `-U<id>:`) |

---

## Mode Switches (second argument)

The second argument **must** start with one of the following mode letters.

### `-S` – Scan Mode (File → CivitAI lookup)

Looks up CivitAI metadata for one or more local SafeTensor files.  
The AutoV2 / SHA-256 hash of the file is computed locally and sent to the API.

**Trigger conditions:**

| Input type | Resulting sub-mode |
|---|---|
| Single `.safetensors` file | `MODE_FILE=true` |
| `InfileList.txt` | `MODE_LIST=true`, `MODE_FILE=true` |

---

### `-A<hash>:` – AutoV2/V3 Direct Lookup Mode

Looks up a model version by its AutoV2 or AutoV3 hash directly, without reading a local file.  
The hash is embedded in the switch: `-A<hash>:` (colon terminates the hash).

**Trigger conditions:**

| Input type | Resulting sub-mode |
|---|---|
| Inline hash via `-A` | `MODE_AUTOV2=true`, `MODE_FILE=false` |
| `InAutoV2List.txt` | `MODE_LIST=true`, `MODE_AUTOV2=true` |

---

### `-U<id|url>:` – Model Version ID / URL Mode

Looks up a model version by its CivitAI numeric model-version ID or a full CivitAI model URL.  
When a URL is provided, the model-version ID is extracted automatically.

**Trigger conditions:**

| Input type | Resulting sub-mode |
|---|---|
| Inline ID/URL via `-U` | `MODE_MODELVID=true`, `MODE_FILE=false` |
| `InUrlList.txt` | `MODE_LIST=true`, `MODE_MODELVID=true` |

---

### `-I` – Inspect SafeTensor Mode

Reads and displays the internal header/metadata of a local `.safetensors` file.  
Does **not** call the CivitAI API.

**Sub-options (appended directly after `-I`):**

| Letter | Meaning | Default |
|---|---|---|
| `h` | Show complete raw header | Off |
| `m` | Show all `__metadata__` fields | Off |
| `i` | Show interesting/parsed metadata fields | **On** |

---

## Option Letters (appended to `-S` / `-A` / `-U`)

These letters can be combined freely after the mode letter:

| Letter | Flag | Default | Description |
|---|---|---|---|
| `f` | `FILE_OUTPUT` | Off | Write JSON output files next to the source tensor file |
| `d` | `DEBUG` | Off | Enable debug/verbose console output |
| `z` | `ZIP_OUTPUT` | **On** | Create ZIP archive with metadata and images |
| `h` | `HISTORY_OUTPUT` | **On** | Write to history log file |
| `a` | `LOAD_ALL_VERSIONS_META` | **On** | Load metadata for all model versions found in the parent model |
| `p` | `LOAD_PRIMARY_TENSORS_FILE` | Off | Download the primary tensor file for the current model version if not present |
| `t` | `LOAD_ALL_VERSIONS_PRIM_TENSOR` | Off | Download primary tensor of every model version (requires `a`); always re-downloads |
| `c` | `LOAD_ALL_VERSIONS_PRIM_TENSOR_IF_NOT_THERE` | Off | Download primary tensor of every model version (requires `a`); only if missing |
| `E` | `LOAD_ALL_VERSIONS_ALL_FILES` | Off | Download **all files** of every model version (requires `a`); always re-downloads |
| `C` | `LOAD_ALL_VERSIONS_ALL_FILES_IF_NOT_THERE` | Off | Download all files of every model version (requires `a`); only if missing |
| `i` | `WRITE_IMAGE_URL_LISTS` | Off | Write `image_urls.txt` into ZIP for each model version |
| `u` | `UPDATE` | Off | Check for new versions of the model; update existing ZIPs |
| `N` | `FAST_FAIL` | Off | Skip processing if a ZIP with the expected name already exists (no hash verification) |

---

## Named Parameters (any position after the second argument)

| Parameter | Format | Description |
|---|---|---|
| `CCD:` | `CCD:<path>` | Override the central CivitAI repository directory |
| `DLL:` | `DLL:<MB>` | Maximum download size per file in megabytes (default: 1600 MB) |
| `HIST:` | `HIST:<path>` | Override the history log file path |
| `TECH:` | `TECH:<path>` | Override the technical log file path |
| `WORKDIR:` | `WORKDIR:<path>` | Override the working/output directory |
| `APIKEY:` | `APIKEY:<token>` | Provide CivitAI API token inline |

---

## API Token Resolution Order

1. `APIKEY:` command-line parameter  
2. `~/.secrets/civitai.token` file  
3. `<assembly_dir>/civitai.token` file  
4. `<drive_root>/dev/secrets/cai.txt` file  
5. *(none)* – unauthenticated requests (rate-limited, some content restricted)

---

## CivitAI API Endpoints Used

| Purpose | Endpoint |
|---|---|
| Lookup by AutoV2/V3 hash | `GET https://civitai.com/api/v1/model-versions/by-hash/{hash}` |
| Lookup by model-version ID | `GET https://civitai.com/api/v1/model-versions/{id}` |
| Lookup full model by model ID | `GET https://civitai.com/api/v1/models/{modelId}` |
| Image download | Direct URL from `images[].url` |
| File download | Direct URL from `files[].downloadUrl` (requires API token for some files) |

---

## Default Paths

| Path variable | Default value |
|---|---|
| Central CivitAI repo | `~/cai-repo/` |
| Fallback / working dir | `~/cai-fallback/` |
| Log directory | `~/cai-logs/` |
| History log | `~/cai-logs/civitAi-Scans.History.txt` |
| Technical log | `~/cai-logs/civitAi-Scans.TCHLOG.txt` |

---

## Usage Examples

```powershell
# Scan a single file, create ZIP with all version metadata
FilescannerCLI.exe "model.safetensors" -Saz

# Look up by AutoV2 hash, no ZIP, just output files
FilescannerCLI.exe dummy -AE4A2B3C1:f

# Look up by model-version ID, update mode
FilescannerCLI.exe dummy -U123456: -Su

# Batch scan from list with fast-fail
FilescannerCLI.exe InfileList.txt -SzaN CCD:D:\cai-repo DLL:512

# Batch lookup from URL list, write image URL lists
FilescannerCLI.exe InUrlList.txt -Sai

# Inspect safetensor header
FilescannerCLI.exe "model.safetensors" -Ih
```
