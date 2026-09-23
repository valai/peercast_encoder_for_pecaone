using System.Windows;

namespace PecaOneRelay;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = new AppState();
        var peerCast = new PeerCastClient(settings);
        var sessions = new SessionManager(peerCast, settings.DataDirectory);
        var server = new RelayServer(settings, sessions);
        MainWindow = new MainWindow(settings, peerCast, sessions, server);
        MainWindow.Show();
    }
}
