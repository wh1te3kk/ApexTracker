namespace ApexTracker.Models;

public sealed record MatchStatistics(
    int TotalMatches,
    int TodayMatches,
    double AverageDamage,
    double AverageKills);
