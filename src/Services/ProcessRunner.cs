using System.Diagnostics;

namespace PersonalMediaArchiver.Services;

/// <summary>
/// Runs external CLI tools (exiftool, magick) safely using ArgumentList
/// to avoid shell injection issues.
/// </summary>
public static class ProcessRunner
{
    public static async Task<(string Output, int ExitCode)> RunAsync(
        string fileName, params string[] args)
    {
        using var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var output = await outputTask;
        await errorTask; // drain stderr to prevent deadlock
        await process.WaitForExitAsync();
        return (output, process.ExitCode);
    }

    public static bool IsAvailable(string command, params string[] testArgs)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            foreach (var arg in testArgs)
                process.StartInfo.ArgumentList.Add(arg);

            process.Start();
            if (!process.WaitForExit(5000))
                return false;
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
