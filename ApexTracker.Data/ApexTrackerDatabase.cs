using Microsoft.EntityFrameworkCore;

namespace ApexTracker.Data;

public static class ApexTrackerDatabase
{
    public static string DefaultPath => Path.Combine(
        @"D:\ApexTrackerDB",
        "ApexTracker.db");

    public static ApexTrackerDbContext CreateContext(string? databasePath = null)
    {
        var path = databasePath ?? DefaultPath;
        var directory = Path.GetDirectoryName(path);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("The database path must include a directory.", nameof(databasePath));
        }

        Directory.CreateDirectory(directory);

        var options = new DbContextOptionsBuilder<ApexTrackerDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        return new ApexTrackerDbContext(options);
    }

    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
