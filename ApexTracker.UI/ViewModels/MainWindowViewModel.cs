using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using ApexTracker.Core;
using ApexTracker.Data;
using ApexTracker.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace ApexTracker.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private const int HistoryPageSize = 20;
    private const int AnalyticsMatchCount = 30;
    private readonly GameMonitorService _gameMonitorService = new();
    private readonly MatchRepository _matchRepository = new();
    private readonly MatchExcelExporter _matchExcelExporter = new();
    private readonly StartupRegistrationService _startupRegistrationService = new();
    private bool _initializingAutoStart;

    [ObservableProperty]
    private string _gameStatusText = "Waiting for Apex";

    [ObservableProperty]
    private string _gameStatusDetail = "r5apex.exe was not detected";

    [ObservableProperty]
    private string _lastUpdatedText = $"Last check: {DateTime.Now:HH:mm:ss}";

    [ObservableProperty]
    private string _statusMessage = "Loading match history...";

    [ObservableProperty]
    private int _totalMatches;

    [ObservableProperty]
    private int _todayMatches;

    [ObservableProperty]
    private string _averageDamageText = "-";

    [ObservableProperty]
    private string _averageKillsText = "-";

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private ISeries[] _damageSeries = [];

    [ObservableProperty]
    private ISeries[] _killsSeries = [];

    [ObservableProperty]
    private Axis[] _matchAxis = [new Axis { Labels = [] }];

    [ObservableProperty]
    private Axis[] _damageAxis = [new Axis { Name = "Damage" }];

    [ObservableProperty]
    private Axis[] _killsAxis = [new Axis { Name = "Kills" }];

    [ObservableProperty]
    private bool _isAutoStartEnabled;

    public ObservableCollection<MatchInfo> RecentMatches { get; } = [];

    public ObservableCollection<MatchInfo> DashboardMatches { get; } = [];

    public string HistoryPageText => TotalPages == 0 ? "No matches" : $"Page {CurrentPage} of {TotalPages}";

    public MainWindowViewModel()
    {
        _initializingAutoStart = true;
        IsAutoStartEnabled = _startupRegistrationService.IsEnabled();
        _initializingAutoStart = false;
        _gameMonitorService.StatusChanged += OnGameStatusChanged;
        _gameMonitorService.StatusChecked += OnGameStatusChecked;
        _gameMonitorService.MonitoringError += OnMonitoringError;
        _gameMonitorService.Start();
        RefreshMatchesCommand.Execute(null);
    }

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(HistoryPageText));

    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(HistoryPageText));

    partial void OnIsAutoStartEnabledChanged(bool value)
    {
        if (_initializingAutoStart)
        {
            return;
        }

        try
        {
            _startupRegistrationService.SetEnabled(value);
            StatusMessage = value ? "Launch at sign-in enabled" : "Launch at sign-in disabled";
        }
        catch (Exception exception)
        {
            _initializingAutoStart = true;
            IsAutoStartEnabled = _startupRegistrationService.IsEnabled();
            _initializingAutoStart = false;
            StatusMessage = $"Could not update launch at sign-in: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveTestMatchAsync()
    {
        var endTime = DateTime.Now;
        var match = new MatchInfo
        {
            StartTime = endTime.AddMinutes(-18),
            EndTime = endTime,
            Map = "World's Edge",
            Legend = "Wraith",
            Kills = 5,
            Assists = 2,
            Damage = 1240,
            Rank = 3
        };

        await _matchRepository.AddAsync(match);
        CurrentPage = 1;
        await LoadWorkspaceAsync();
        StatusMessage = "Test match saved to D:\\ApexTrackerDB\\ApexTracker.db";
    }

    [RelayCommand]
    private async Task RefreshMatchesAsync()
    {
        CurrentPage = 1;
        await LoadWorkspaceAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadWorkspaceAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadWorkspaceAsync();
        }
    }

    [RelayCommand]
    private async Task ExportMatchesAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            FileName = $"ApexTracker-Matches-{DateTime.Now:yyyyMMdd}.xlsx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var matches = await _matchRepository.GetAllMatchesAsync();
            await _matchExcelExporter.ExportAsync(matches, dialog.FileName);
            StatusMessage = $"Exported {matches.Count} matches to {dialog.FileName}";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Match export failed: {exception.Message}";
        }
    }

    public void Dispose()
    {
        _gameMonitorService.StatusChanged -= OnGameStatusChanged;
        _gameMonitorService.StatusChecked -= OnGameStatusChecked;
        _gameMonitorService.MonitoringError -= OnMonitoringError;
        _gameMonitorService.Dispose();
    }

    private async Task LoadWorkspaceAsync()
    {
        try
        {
            var history = await _matchRepository.GetHistoryAsync(new MatchHistoryQuery
            {
                PageNumber = CurrentPage,
                PageSize = HistoryPageSize
            });
            var statistics = await _matchRepository.GetStatisticsAsync();
            var analyticsMatches = (await _matchRepository.GetRecentMatchesAsync(AnalyticsMatchCount))
                .OrderBy(match => match.StartTime)
                .ToList();

            RecentMatches.Clear();
            foreach (var match in history.Items)
            {
                RecentMatches.Add(match);
            }

            DashboardMatches.Clear();
            foreach (var match in history.Items.Take(5))
            {
                DashboardMatches.Add(match);
            }

            TotalMatches = statistics.TotalMatches;
            TodayMatches = statistics.TodayMatches;
            AverageDamageText = statistics.TotalMatches == 0 ? "-" : Math.Round(statistics.AverageDamage).ToString("0");
            AverageKillsText = statistics.TotalMatches == 0 ? "-" : statistics.AverageKills.ToString("0.0");
            TotalPages = history.TotalPages;
            CurrentPage = history.TotalPages == 0 ? 1 : history.PageNumber;
            UpdateCharts(analyticsMatches);
            StatusMessage = history.TotalCount == 0 ? "No match records" : $"Loaded {history.TotalCount} match records";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Match history failed to load: {exception.Message}";
        }
    }

    private void UpdateCharts(IReadOnlyList<MatchInfo> matches)
    {
        MatchAxis = [new Axis
        {
            Labels = matches.Select(match => match.StartTime.ToString("MM-dd")).ToArray(),
            LabelsRotation = 0,
            TextSize = 11
        }];
        DamageAxis = [new Axis { Name = "Damage", TextSize = 11 }];
        KillsAxis = [new Axis { Name = "Kills", TextSize = 11 }];
        DamageSeries =
        [
            new LineSeries<double>
            {
                Name = "Damage",
                Values = matches.Select(match => (double)match.Damage).ToArray(),
                GeometrySize = 7,
                Fill = null
            }
        ];
        KillsSeries =
        [
            new ColumnSeries<double>
            {
                Name = "Kills",
                Values = matches.Select(match => (double)match.Kills).ToArray()
            }
        ];
    }

    private void OnGameStatusChanged(object? sender, GameStatus status)
    {
        RunOnUiThread(() =>
        {
            GameStatusText = status == GameStatus.Running ? "Apex is running" : "Waiting for Apex";
            GameStatusDetail = status == GameStatus.Running ? "r5apex.exe was detected" : "r5apex.exe was not detected";
        });
    }

    private void OnGameStatusChecked(object? sender, GameStatus status)
    {
        RunOnUiThread(() => LastUpdatedText = $"Last check: {_gameMonitorService.LastCheckedAt:HH:mm:ss}");
    }

    private void OnMonitoringError(object? sender, Exception exception)
    {
        RunOnUiThread(() => StatusMessage = $"Apex process detection failed: {exception.Message}");
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.BeginInvoke(action);
    }
}
