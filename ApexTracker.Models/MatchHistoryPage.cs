namespace ApexTracker.Models;

public sealed record MatchHistoryPage(
    IReadOnlyList<MatchInfo> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
