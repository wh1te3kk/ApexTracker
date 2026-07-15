using System.Globalization;
using ApexTracker.Models;

namespace ApexTracker.Core;

// This parser is for a known key-value log format. A real Apex adapter can replace it.
public sealed class KeyValueMatchLogParser : IMatchLogParser
{
    public bool TryParseCompletedMatch(string logLine, out MatchInfo? match)
    {
        match = null;

        if (string.IsNullOrWhiteSpace(logLine) || !logLine.StartsWith("MATCH_END|", StringComparison.Ordinal))
        {
            return false;
        }

        var values = logLine["MATCH_END|".Length..]
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);

        if (!TryGetDateTime(values, "start", out var startTime) ||
            !TryGetDateTime(values, "end", out var endTime) ||
            !TryGetString(values, "map", out var map) ||
            !TryGetString(values, "legend", out var legend) ||
            !TryGetNonNegativeInt(values, "kills", out var kills) ||
            !TryGetNonNegativeInt(values, "assists", out var assists) ||
            !TryGetNonNegativeInt(values, "damage", out var damage) ||
            !TryGetNonNegativeInt(values, "rank", out var rank))
        {
            return false;
        }

        match = new MatchInfo
        {
            StartTime = startTime,
            EndTime = endTime,
            Map = map,
            Legend = legend,
            Kills = kills,
            Assists = assists,
            Damage = damage,
            Rank = rank
        };

        return true;
    }

    private static bool TryGetDateTime(
        IReadOnlyDictionary<string, string> values,
        string key,
        out DateTime value)
    {
        value = default;
        return values.TryGetValue(key, out var text) &&
               DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value);
    }

    private static bool TryGetString(
        IReadOnlyDictionary<string, string> values,
        string key,
        out string value)
    {
        if (values.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text))
        {
            value = text;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetNonNegativeInt(
        IReadOnlyDictionary<string, string> values,
        string key,
        out int value)
    {
        value = default;
        return values.TryGetValue(key, out var text) &&
               int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) &&
               value >= 0;
    }
}
