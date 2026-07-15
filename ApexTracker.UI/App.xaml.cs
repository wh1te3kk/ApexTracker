using System.Windows;
using ApexTracker.Data;

namespace ApexTracker.UI;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        await ApexTrackerDatabase.InitializeAsync();
        base.OnStartup(e);
    }
}
