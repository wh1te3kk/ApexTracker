using System.ComponentModel;
using System.Runtime.CompilerServices;
using ApexTracker.Core;
using ApexTracker.Models;

namespace ApexTracker.UI.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly GameMonitorService _gameMonitorService = new();
    private GameStatus _gameStatus = GameStatus.NotRunning;

    public MainWindowViewModel()
    {
        _gameMonitorService.StatusChanged += OnGameStatusChanged;
        _gameMonitorService.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string GameStatusText => _gameStatus == GameStatus.Running ? "Apex 运行中" : "等待游戏启动";

    public string GameStatusDetail => _gameStatus == GameStatus.Running
        ? "已检测到 r5apex.exe"
        : "未检测到 r5apex.exe";

    public string LastUpdatedText => $"最后检测：{DateTime.Now:HH:mm:ss}";

    public int TotalMatches => 0;

    public int TodayMatches => 0;

    public string AverageDamageText => "-";

    public string KdText => "-";

    public void Dispose()
    {
        _gameMonitorService.StatusChanged -= OnGameStatusChanged;
        _gameMonitorService.Dispose();
    }

    private void OnGameStatusChanged(object? sender, GameStatus status)
    {
        _gameStatus = status;
        OnPropertyChanged(nameof(GameStatusText));
        OnPropertyChanged(nameof(GameStatusDetail));
        OnPropertyChanged(nameof(LastUpdatedText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
