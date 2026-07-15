using ApexTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ApexTracker.Data;

public sealed class ApexTrackerDbContext(DbContextOptions<ApexTrackerDbContext> options) : DbContext(options)
{
    public DbSet<MatchInfo> Matches => Set<MatchInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var match = modelBuilder.Entity<MatchInfo>();

        match.ToTable("Matches");
        match.HasKey(item => item.Id);
        match.Property(item => item.StartTime).IsRequired();
        match.Property(item => item.Map).HasMaxLength(64).IsRequired();
        match.Property(item => item.Legend).HasMaxLength(64).IsRequired();
        match.Property(item => item.Kills).HasDefaultValue(0);
        match.Property(item => item.Assists).HasDefaultValue(0);
        match.Property(item => item.Damage).HasDefaultValue(0);
        match.Property(item => item.Rank).HasDefaultValue(0);
        match.Ignore(item => item.Duration);
        match.HasIndex(item => item.StartTime);
        match.ToTable(table => table.HasCheckConstraint(
            "CK_Matches_ValuesAreNotNegative",
            "Kills >= 0 AND Assists >= 0 AND Damage >= 0 AND Rank >= 0"));
    }
}
