namespace CaiViewer.Core.Scanner;

public enum ScannerMode { FileScan, AutoV2Lookup, UrlIdLookup, InspectSafeTensor }
public enum InputType { SingleFile, InfileList, InAutoV2List, InUrlList }

public class ScannerOptions
{
    // Mode
    public ScannerMode Mode { get; set; } = ScannerMode.FileScan;
    public InputType InputType { get; set; } = InputType.SingleFile;
    public string Input { get; set; } = string.Empty;

    // Option flags
    public bool CreateZip { get; set; } = true;
    public bool AllVersionsMeta { get; set; } = true;
    public bool FileOutput { get; set; }
    public bool WriteImageUrlLists { get; set; }
    public bool HistoryOutput { get; set; }
    public bool UpdateMode { get; set; }
    public bool DebugOutput { get; set; }
    public bool FastFail { get; set; }

    // Download options
    public bool PrimaryTensorIfMissing { get; set; }
    public bool AllVersionsPrimaryTensorOverwrite { get; set; }
    public bool AllVersionsPrimaryTensorIfMissing { get; set; }
    public bool AllVersionsAllFilesOverwrite { get; set; }
    public bool AllVersionsAllFilesIfMissing { get; set; }

    // Inspect options (only for InspectSafeTensor mode)
    public bool InspectRawHeader { get; set; }
    public bool InspectAllMetadata { get; set; }
    public bool InspectInteresting { get; set; } = true;

    // Named parameters
    public string? CentralCivitAiPath { get; set; }
    public int? MaxDownloadSizeMb { get; set; } = 1600;
    public string? ApiKey { get; set; }
    public string? WorkDirectory { get; set; }

    public string BuildArguments()
    {
        var parts = new List<string>();

        // INPUT
        parts.Add($"\"{Input}\"");

        // MODE + FLAGS
        switch (Mode)
        {
            case ScannerMode.FileScan:
                parts.Add("-S" + BuildOptionFlags());
                break;
            case ScannerMode.AutoV2Lookup:
                parts.Add($"-A{Input}:" + BuildOptionFlags());
                // Re-inject a dummy first arg if needed; for inline hash mode the input IS the hash
                break;
            case ScannerMode.UrlIdLookup:
                parts.Add($"-U{Input}:" + BuildOptionFlags());
                break;
            case ScannerMode.InspectSafeTensor:
                var iflags = string.Empty;
                if (InspectRawHeader) iflags += "h";
                if (InspectAllMetadata) iflags += "m";
                if (InspectInteresting) iflags += "i";
                parts.Add("-I" + iflags);
                break;
        }

        // NAMED PARAMS
        if (!string.IsNullOrWhiteSpace(CentralCivitAiPath))
            parts.Add($"CCD:\"{CentralCivitAiPath}\"");
        if (MaxDownloadSizeMb.HasValue)
            parts.Add($"DLL:{MaxDownloadSizeMb}");
        if (!string.IsNullOrWhiteSpace(ApiKey))
            parts.Add($"APIKEY:{ApiKey}");
        if (!string.IsNullOrWhiteSpace(WorkDirectory))
            parts.Add($"WORKDIR:\"{WorkDirectory}\"");

        return string.Join(" ", parts);
    }

    private string BuildOptionFlags()
    {
        var flags = string.Empty;
        if (CreateZip) flags += "z";
        if (AllVersionsMeta) flags += "a";
        if (FileOutput) flags += "f";
        if (WriteImageUrlLists) flags += "i";
        if (HistoryOutput) flags += "h";
        if (UpdateMode) flags += "u";
        if (DebugOutput) flags += "d";
        if (FastFail) flags += "N";
        if (PrimaryTensorIfMissing) flags += "p";
        if (AllVersionsPrimaryTensorOverwrite) flags += "t";
        if (AllVersionsPrimaryTensorIfMissing) flags += "c";
        if (AllVersionsAllFilesOverwrite) flags += "E";
        if (AllVersionsAllFilesIfMissing) flags += "C";
        return flags;
    }
}
