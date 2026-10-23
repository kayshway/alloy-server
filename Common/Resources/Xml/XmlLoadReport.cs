using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;

namespace Common.Resources.Xml;

/// <summary>
/// Collects everything the XML loader had to skip or ignore, so newer game data
/// (exported from the current client) can load without crashing the server.
/// Written to xml-load-report.txt next to the server after every load.
/// </summary>
public static class XmlLoadReport {
    // "kind: value" -> number of times seen (e.g. "ActivateEffect: Dash" -> 12)
    private static readonly ConcurrentDictionary<string, int> UnknownValues = new();

    // entries that could not be loaded at all
    private static readonly ConcurrentQueue<string> SkippedEntries = new();

    public static int SkippedCount => SkippedEntries.Count;
    public static int UnknownCount => UnknownValues.Count;

    public static void Unknown(string kind, string value) {
        UnknownValues.AddOrUpdate($"{kind}: {value}", 1, (_, n) => n + 1);
    }

    public static void Skipped(string what, Exception e) {
        while (e is AggregateException { InnerException: not null } ae) {
            e = ae.InnerException;
        }

        SkippedEntries.Enqueue($"{what}: {e.GetType().Name}: {e.Message}");
    }

    /// <summary>Parse an enum, falling back (and recording the value) when it isn't known yet.</summary>
    public static T ParseEnum<T>(string value, T fallback) where T : struct, Enum {
        if (string.IsNullOrWhiteSpace(value)) {
            return fallback;
        }

        if (Enum.TryParse(value.Replace(" ", ""), out T result)) {
            return result;
        }

        Unknown(typeof(T).Name, value);
        return fallback;
    }

    public static string Write(string path) {
        var sb = new StringBuilder();
        sb.AppendLine("XML load report");
        sb.AppendLine("===============");
        sb.AppendLine();
        sb.AppendLine($"Skipped entries ({SkippedEntries.Count}) - these objects/tiles could not be loaded and are missing in game:");
        foreach (var line in SkippedEntries.OrderBy(l => l)) {
            sb.AppendLine("  " + line);
        }

        sb.AppendLine();
        sb.AppendLine($"Unknown values ({UnknownValues.Count}) - loaded, but this part is ignored until the server supports it:");
        foreach (var (key, count) in UnknownValues.OrderBy(kv => kv.Key)) {
            sb.AppendLine($"  {key}  (x{count})");
        }

        try {
            File.WriteAllText(path, sb.ToString());
        } catch {
            // report is a convenience; never fail the load over it
        }

        return path;
    }
}
