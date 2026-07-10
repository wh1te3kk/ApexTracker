using System.Diagnostics;
using ApexTracker.Models;

namespace ApexTracker.Core;

public sealed class GameMonitorService : IDisposable
{
    private readonly PeriodicTimer _timer;
    private CancellationTokenSource? _cancellationTokenSource;
    private GameStatus _currentStatus = GameStatus.NotRunning;

    public GameMonitorService(TimeSpan? pollingInterval = null)
    {
        _timer = new PeriodicTimer(pollingInterval ?? TimeSpan.FromSeconds(2));
    }

    public event EventHandler<GameStatus>? StatusChanged;

    public GameStatus CurrentStatus => _currentStatus;

    public void Start()
    {
        if (_cancellationTokenSource is not null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _ = MonitorAsync(_cancellationTokenSource.Token);
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        UpdateStatus();

        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken))
            {
                UpdateStatus();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void UpdateStatus()
    {
        var nextStatus = IsApexRunning() ? GameStatus.Running : GameStatus.NotRunning;
        if (nextStatus == _currentStatus)
        {
            return;
        }

        _currentStatus = nextStatus;
        StatusChanged?.Invoke(this, _currentStatus);
    }

    private static bool IsApexRunning()
    {
        return Process.GetProcessesByName("r5apex").Length > 0;
    }
}
