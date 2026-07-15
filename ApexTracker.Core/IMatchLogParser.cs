using ApexTracker.Models;

namespace ApexTracker.Core;

public interface IMatchLogParser
{
    bool TryParseCompletedMatch(string logLine, out MatchInfo? match);
}
