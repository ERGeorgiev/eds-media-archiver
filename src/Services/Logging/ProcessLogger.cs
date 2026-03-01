using System.Collections.Concurrent;
using static EdsMediaArchiver.Services.Logging.IProcessLogger;

namespace EdsMediaArchiver.Services.Logging;

public interface IProcessLogger
{
    IEnumerable<ProcessLog> Logs { get; }

    void Log(Operation operation, Result result, string filePath, string message);

    void PrintSummary();

    public enum Operation
    {
        Compress,
        Convert,
        ReadDate,
        Process,
        Archive,
        Date,
        Resolve,
        Validate
    }

    public enum Result
    {
        Success,
        Error,
        Skip
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
        var key = Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileNameWithoutExtension(filePath));
        _logsPerFile.AddOrUpdate(key, [log], (k, v) => { v.Add(log); return v; });
        PrintLog(log);
    }

    public static void PrintLog(ProcessLog log)
    {
        Console.WriteLine($"  [{log.Operation}:{log.Result}] {log.Message} for '{log.FilePath,-60}'");
    }

    public void PrintSummary()
    {
        // Summary
        Console.WriteLine("────────────────────────────────────────────────");
        Console.WriteLine($"Summary");
        var skipped = _logs.Where(r => r.Result == Result.Skip).ToList();
        var errored = _logs.Where(r => r.Result == Result.Error).ToList();

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
        Console.WriteLine($"    Skipped:    {skipped.Count}");
        Console.WriteLine($"    Errors:     {errored.Count}");
    }
}
