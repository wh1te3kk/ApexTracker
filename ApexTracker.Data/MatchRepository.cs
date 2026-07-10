using ApexTracker.Models;

namespace ApexTracker.Data;

public sealed class MatchRepository
{
    private readonly List<MatchInfo> _matches = [];

    public IReadOnlyList<MatchInfo> GetRecentMatches(int count = 20)
    {
        return _matches
            .OrderByDescending(match => match.StartTime)
            .Take(count)
            .ToList();
    }

    public void Add(MatchInfo match)
    {
        _matches.Add(match);
    }
}
