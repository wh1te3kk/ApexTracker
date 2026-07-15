using ApexTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ApexTracker.Data;

public sealed class MatchRepository(string? databasePath = null)
{
    private readonly string _databasePath = databasePath ?? ApexTrackerDatabase.DefaultPath;

    public async Task<MatchInfo> AddAsync(MatchInfo match, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(match);
        ValidateMatch(match);

        match.Map = match.Map.Trim();
        match.Legend = match.Legend.Trim();

        await using var context = CreateContext();
        context.Matches.Add(match);
        await context.SaveChangesAsync(cancellationToken);
        return match;
    }

    public async Task<MatchHistoryPage> GetHistoryAsync(
        MatchHistoryQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        query ??= new MatchHistoryQuery();
        ValidateQuery(query);

        await using var context = CreateContext();
        IQueryable<MatchInfo> matches = context.Matches.AsNoTracking();

        if (query.StartedFrom.HasValue)
        {
            matches = matches.Where(match => match.StartTime >= query.StartedFrom.Value);
        }

        if (query.StartedTo.HasValue)
        {
            matches = matches.Where(match => match.StartTime <= query.StartedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Map))
        {
            matches = matches.Where(match => match.Map == query.Map.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Legend))
        {
            matches = matches.Where(match => match.Legend == query.Legend.Trim());
        }

        var totalCount = await matches.CountAsync(cancellationToken);
        var items = await matches
            .OrderByDescending(match => match.StartTime)
            .ThenByDescending(match => match.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new MatchHistoryPage(items, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<MatchStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var matches = context.Matches.AsNoTracking();
        var totalMatches = await matches.CountAsync(cancellationToken);
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var todayMatches = await matches.CountAsync(
            match => match.StartTime >= today && match.StartTime < tomorrow,
            cancellationToken);

        var averageDamage = totalMatches == 0
            ? 0
            : await matches.AverageAsync(match => (double)match.Damage, cancellationToken);
        var averageKills = totalMatches == 0
            ? 0
            : await matches.AverageAsync(match => (double)match.Kills, cancellationToken);

        return new MatchStatistics(totalMatches, todayMatches, averageDamage, averageKills);
    }

    public async Task<IReadOnlyList<MatchInfo>> GetRecentMatchesAsync(
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        var page = await GetHistoryAsync(
            new MatchHistoryQuery { PageSize = count },
            cancellationToken);
        return page.Items;
    }

    public async Task<IReadOnlyList<MatchInfo>> GetAllMatchesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Matches
            .AsNoTracking()
            .OrderByDescending(match => match.StartTime)
            .ThenByDescending(match => match.Id)
            .ToListAsync(cancellationToken);
    }

    private ApexTrackerDbContext CreateContext() => ApexTrackerDatabase.CreateContext(_databasePath);

    private static void ValidateMatch(MatchInfo match)
    {
        if (match.StartTime == default)
        {
            throw new ArgumentException("Match start time is required.", nameof(match));
        }

        if (match.EndTime.HasValue && match.EndTime.Value < match.StartTime)
        {
            throw new ArgumentException("Match end time cannot be earlier than start time.", nameof(match));
        }

        if (string.IsNullOrWhiteSpace(match.Map) || match.Map.Length > 64)
        {
            throw new ArgumentException("Map must contain 1 to 64 characters.", nameof(match));
        }

        if (string.IsNullOrWhiteSpace(match.Legend) || match.Legend.Length > 64)
        {
            throw new ArgumentException("Legend must contain 1 to 64 characters.", nameof(match));
        }

        if (match.Kills < 0 || match.Assists < 0 || match.Damage < 0 || match.Rank < 0)
        {
            throw new ArgumentException("Match statistics cannot be negative.", nameof(match));
        }
    }

    private static void ValidateQuery(MatchHistoryQuery query)
    {
        if (query.PageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Page number must be greater than zero.");
        }

        if (query.PageSize is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Page size must be between 1 and 100.");
        }

        if (query.StartedFrom.HasValue && query.StartedTo.HasValue && query.StartedFrom > query.StartedTo)
        {
            throw new ArgumentException("Start date must be earlier than end date.", nameof(query));
        }
    }
}
