using System.Diagnostics;

namespace PerformanceComparer;

internal sealed class StopWatchTimer : IDisposable
{
    private readonly Stopwatch _stopWatch;
    private readonly IDictionary<string, TimeSpan> _dict;
    private readonly string _key;

    public StopWatchTimer(IDictionary<string, TimeSpan> dict, string key)
    {
        if (dict.ContainsKey(key))
        {
            throw new ArgumentException($"Key {key} already exists in the dictionary.", nameof(key));
        }

        _key = key;
        _dict = dict;
        _stopWatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopWatch.Stop();
        _dict.Add(_key, _stopWatch.Elapsed);
    }
}

public static class Utilities
{
    public static void Print(IDictionary<string, TimeSpan> dict, int rounds)
    {
        foreach (var kvp in dict)
        {
            Console.Out.WriteLine($"{kvp.Key}: {rounds} rounds, {kvp.Value.TotalSeconds} seconds.");
        }
    }
}
