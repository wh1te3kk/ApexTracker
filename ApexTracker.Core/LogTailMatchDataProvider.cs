using System.Text;
using ApexTracker.Models;

namespace ApexTracker.Core;

public sealed class LogTailMatchDataProvider : IMatchDataProvider
{
    private readonly string _logFilePath;
    private readonly IMatchLogParser _parser;
    private readonly TimeSpan _pollingInterval;
    private readonly bool _readExistingEntries;
    private readonly object _sync = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _collectionTask;
    private long _readOffset;
    private string _pendingLine = string.Empty;
    private bool _hasInitializedOffset;
    private bool _fileMissingReported;

    public LogTailMatchDataProvider(
        string logFilePath,
        IMatchLogParser parser,
        TimeSpan? pollingInterval = null,
        bool readExistingEntries = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);
        ArgumentNullException.ThrowIfNull(parser);

        _logFilePath = logFilePath;
        _parser = parser;
        _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(1);
        _readExistingEntries = readExistingEntries;

        if (_pollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollingInterval), "Polling interval must be greater than zero.");
        }
    }

    public event EventHandler<MatchInfo>? MatchCompleted;

    public event EventHandler<Exception>? CollectionError;

    public bool IsCollecting
    {
        get
        {
            lock (_sync)
            {
                return _collectionTask is { IsCompleted: false };
            }
        }
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_collectionTask is { IsCompleted: false })
            {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _collectionTask = CollectAsync(_cancellationTokenSource);
        }
    }

    public async Task StopAsync()
    {
        Task? collectionTask;

        lock (_sync)
        {
            _cancellationTokenSource?.Cancel();
            collectionTask = _collectionTask;
        }

        if (collectionTask is not null)
        {
            await collectionTask.ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task CollectAsync(CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            await ReadNewLinesAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            using var timer = new PeriodicTimer(_pollingInterval);
            while (await timer.WaitForNextTickAsync(cancellationTokenSource.Token).ConfigureAwait(false))
            {
                await ReadNewLinesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
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
                    _collectionTask = null;
                    _cancellationTokenSource = null;
                }
            }

            cancellationTokenSource.Dispose();
        }
    }

    private async Task ReadNewLinesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_logFilePath))
            {
                if (!_fileMissingReported)
                {
                    _fileMissingReported = true;
                    ReportError(new FileNotFoundException("The configured match log file was not found.", _logFilePath));
                }

                return;
            }

            _fileMissingReported = false;

            await using var stream = new FileStream(
                _logFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                useAsync: true);

            if (!_hasInitializedOffset)
            {
                _readOffset = _readExistingEntries ? 0 : stream.Length;
                _hasInitializedOffset = true;
            }

            if (stream.Length < _readOffset)
            {
                _readOffset = 0;
                _pendingLine = string.Empty;
            }

            if (stream.Length == _readOffset)
            {
                return;
            }

            var byteCount = checked((int)(stream.Length - _readOffset));
            var buffer = new byte[byteCount];
            stream.Seek(_readOffset, SeekOrigin.Begin);
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            _readOffset = stream.Length;

            var content = _pendingLine + Encoding.UTF8.GetString(buffer);
            var lines = content.Split('\n');
            _pendingLine = lines[^1];

            foreach (var line in lines[..^1])
            {
                if (_parser.TryParseCompletedMatch(line.TrimEnd('\r'), out var match) && match is not null)
                {
                    MatchCompleted?.Invoke(this, match);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private void ReportError(Exception exception)
    {
        CollectionError?.Invoke(this, exception);
    }
}
