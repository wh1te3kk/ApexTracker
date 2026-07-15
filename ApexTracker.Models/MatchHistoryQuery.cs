namespace ApexTracker.Models;

public sealed class MatchHistoryQuery
{
    public DateTime? StartedFrom { get; init; }

    public DateTime? StartedTo { get; init; }

    public string? Map { get; init; }

    public string? Legend { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
