namespace ApexTracker.Models;

public sealed class MatchInfo
{
    // SQLite assigns the primary key when a completed match is saved.
    public int Id { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string Map { get; set; } = string.Empty;

    public string Legend { get; set; } = string.Empty;

    public int Kills { get; set; }

    public int Assists { get; set; }

    public int Damage { get; set; }

    public int Rank { get; set; }

    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
}
