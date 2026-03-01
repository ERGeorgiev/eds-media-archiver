using System.Collections.Concurrent;
using static EdsMediaArchiver.Services.Logging.IProcessLogger;

namespace EdsMediaArchiver.Services.Logging;

public interface IProcessLogger
{
    IEnumerable<ProcessLog> Logs { get; }

    void Log(Operation operation, Result result, string filePath, string message);
    void PrintLogs(string filePath);
    void PrintSummary();

    public enum Operation
    {
        Compress,
        Convert,
        Archive,
        SetDate,
        RestoreExtension,
    }

    public enum Result
    {
        SUCCESS,
        ERROR,
        SKIPPED
    }
}

public record ProcessLog (Operation Operation, Result Result, string FilePath, string Message);

public class ProcessLogger : IProcessLogger
{
    public readonly ConcurrentBag<ProcessLog> _logs = [];
    public readonly ConcurrentDictionary<string, List<ProcessLog>> _logsPerFile = [];

    public IEnumerable<ProcessLog> Logs => _logs;

    public void Log(Operation operation, Result result, string filePath, string message)
    {
        var log = new ProcessLog(operation, result, filePath, message);
        _logs.Add(log);
        var key = FilePathToKey(filePath);
        _logsPerFile.AddOrUpdate(key, [log], (k, v) => { v.Add(log); return v; });
    }

    public void PrintLogs(string filePath)
    {
        var key = FilePathToKey(filePath);
        if (_logsPerFile.TryGetValue(key, out var logs))
        {
            foreach (var log in logs)
            {
                PrintLog(log);
            }
        }
    }

    public static void PrintLog(ProcessLog log)
    {
        if (log.Result == Result.SUCCESS)
        {
            Console.WriteLine($"  [{log.Operation}] {log.Message}");
        }
        else
        {
            Console.WriteLine($"  {log.Result}:[{log.Operation}] {log.Message}");
        }
    }

    private static string FilePathToKey(string filePath)
    {
        return filePath;
    }

    public void PrintSummary()
    {
        // Summary
        Console.WriteLine("────────────────────────────────────────────────");
        Console.WriteLine($"Summary");
        var success = _logs.Where(r => r.Result == Result.SUCCESS).ToList();
        var skipped = _logs.Where(r => r.Result == Result.SKIPPED).ToList();
        var errored = _logs.Where(r => r.Result == Result.ERROR).ToList();

        if (skipped.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  Skipped Operations:");
            foreach (var l in skipped)
                PrintLog(l);
        }

        if (errored.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  Errored Operations:");
            foreach (var l in errored)
                PrintLog(l);
        }

        Console.WriteLine();
        Console.WriteLine("  Results:");
        Console.WriteLine($"    Success:    {success.Count}");
        Console.WriteLine($"    Skipped:    {skipped.Count}");
        Console.WriteLine($"    Errors:     {errored.Count}");
    }
}
