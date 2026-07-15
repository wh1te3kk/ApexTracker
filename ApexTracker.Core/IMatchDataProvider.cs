using ApexTracker.Models;

namespace ApexTracker.Core;

public interface IMatchDataProvider : IDisposable, IAsyncDisposable
{
    event EventHandler<MatchInfo>? MatchCompleted;

    event EventHandler<Exception>? CollectionError;

    bool IsCollecting { get; }

    void Start();

    Task StopAsync();
}
