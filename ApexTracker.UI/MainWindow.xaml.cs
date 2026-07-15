using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using ApexTracker.UI.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace ApexTracker.UI;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();
    private readonly TaskbarIcon _taskbarIcon;
    private bool _exitRequested;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _taskbarIcon = CreateTaskbarIcon();
        StateChanged += OnWindowStateChanged;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitRequested)
        {
            e.Cancel = true;
            HideToTray();
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _taskbarIcon.Dispose();
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private TaskbarIcon CreateTaskbarIcon()
    {
        var contextMenu = new ContextMenu();
        var openItem = new MenuItem { Header = "Open Apex Tracker" };
        openItem.Click += (_, _) => RestoreWindow();
        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApplication();
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        var taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Apex Tracker",
            Icon = SystemIcons.Application,
            ContextMenu = contextMenu
        };
        taskbarIcon.TrayMouseDoubleClick += (_, _) => RestoreWindow();
        return taskbarIcon;
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            HideToTray();
        }
    }

    private void HideToTray()
    {
        Hide();
        _taskbarIcon.ShowBalloonTip("Apex Tracker", "The app is still running in the system tray.", BalloonIcon.Info);
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        Close();
    }
}
