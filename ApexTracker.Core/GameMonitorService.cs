using System.Diagnostics;
using ApexTracker.Models;

namespace ApexTracker.Core;

public sealed class GameMonitorService : IDisposable, IAsyncDisposable
{
    public const string ApexProcessName = "r5apex";

    private readonly TimeSpan _pollingInterval;
    private readonly Func<bool> _isGameRunning;
    private readonly object _sync = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitorTask;
    private GameStatus _currentStatus = GameStatus.NotRunning;
    private DateTimeOffset _lastCheckedAt;

    public GameMonitorService(TimeSpan? pollingInterval = null, Func<bool>? isGameRunning = null)
    {
        _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(2);

        if (_pollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollingInterval), "Polling interval must be greater than zero.");
        }

        _isGameRunning = isGameRunning ?? IsApexRunning;
    }

    public event EventHandler<GameStatus>? StatusChanged;

    public event EventHandler<GameStatus>? StatusChecked;

    public event EventHandler<Exception>? MonitoringError;

    public TimeSpan PollingInterval => _pollingInterval;

    public GameStatus CurrentStatus
    {
        get
        {
            lock (_sync)
            {
                return _currentStatus;
            }
        }
    }

    public DateTimeOffset LastCheckedAt
    {
        get
        {
            lock (_sync)
            {
                return _lastCheckedAt;
            }
        }
    }

    public bool IsMonitoring
    {
        get
        {
            lock (_sync)
            {
                return _monitorTask is { IsCompleted: false };
            }
        }
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_monitorTask is { IsCompleted: false })
            {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _monitorTask = MonitorAsync(_cancellationTokenSource);
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _cancellationTokenSource?.Cancel();
        }
    }

    public async Task StopAsync()
    {
        Task? monitorTask;

        lock (_sync)
        {
            _cancellationTokenSource?.Cancel();
            monitorTask = _monitorTask;
        }

        if (monitorTask is not null)
        {
            await monitorTask.ConfigureAwait(false);
        }
    }

    public GameStatus ProbeNow()
    {
        GameStatus nextStatus;

        try
        {
            nextStatus = _isGameRunning() ? GameStatus.Running : GameStatus.NotRunning;
        }
        catch (Exception exception)
        {
            RecordProbeFailure(exception);
            return CurrentStatus;
        }

        EventHandler<GameStatus>? statusChanged = null;
        EventHandler<GameStatus>? statusChecked;

        lock (_sync)
        {
            _lastCheckedAt = DateTimeOffset.Now;
            statusChecked = StatusChecked;

            if (nextStatus != _currentStatus)
            {
                _currentStatus = nextStatus;
                statusChanged = StatusChanged;
            }
        }

        statusChanged?.Invoke(this, nextStatus);
        statusChecked?.Invoke(this, nextStatus);
        return nextStatus;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task MonitorAsync(CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            ProbeNow();

            using var timer = new PeriodicTimer(_pollingInterval);
            while (await timer.WaitForNextTickAsync(cancellationTokenSource.Token).ConfigureAwait(false))
            {
                ProbeNow();
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
                {
                    _monitorTask = null;
                    _cancellationTokenSource = null;
                }
            }

            cancellationTokenSource.Dispose();
        }
    }

    private void RecordProbeFailure(Exception exception)
    {
        EventHandler<Exception>? monitoringError;

        lock (_sync)
        {
            _lastCheckedAt = DateTimeOffset.Now;
            monitoringError = MonitoringError;
        }

        monitoringError?.Invoke(this, exception);
    }

    private static bool IsApexRunning()
    {
        var processes = Process.GetProcessesByName(ApexProcessName);

        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}
