using System.Diagnostics;
using System.Threading.Channels;

namespace CaiViewer.Core.Scanner;

public record ScanResult(int ExitCode, IReadOnlyList<string> LogLines);

public class ScannerService
{
    public async Task<ScanResult> RunAsync(
        string executablePath,
        ScannerOptions options,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var args = options.BuildArguments();
        var logLines = new List<string>();

        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                channel.Writer.TryWrite(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                channel.Writer.TryWrite("[ERR] " + e.Data);
        };
        process.Exited += (_, _) => channel.Writer.TryComplete();

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Register cancellation → kill process
        await using var reg = cancellationToken.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            channel.Writer.TryComplete();
        });

        // Drain channel
        await foreach (var line in channel.Reader.ReadAllAsync(CancellationToken.None))
        {
            logLines.Add(line);
            progress?.Report(line);
        }

        await process.WaitForExitAsync(CancellationToken.None);
        return new ScanResult(process.ExitCode, logLines);
    }

    public static string BuildCommandPreview(string executablePath, ScannerOptions options)
        => $"\"{executablePath}\" {options.BuildArguments()}";
}
