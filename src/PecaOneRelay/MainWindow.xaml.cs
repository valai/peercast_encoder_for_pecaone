using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QRCoder;
using Forms = System.Windows.Forms;

namespace PecaOneRelay;

public partial class MainWindow : Window
{
    private readonly AppState _settings;
    private readonly PeerCastClient _peerCast;
    private readonly SessionManager _sessions;
    private readonly RelayServer _server;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly Forms.NotifyIcon _tray;
    private bool _exiting;
    private bool _refreshing;

    internal MainWindow(AppState settings, PeerCastClient peerCast, SessionManager sessions, RelayServer server)
    {
        InitializeComponent();
        _settings = settings;
        _peerCast = peerCast;
        _sessions = sessions;
        _server = server;
        PeerUrl.Text = settings.PeerCastUrl;
        ListenPort.Text = settings.ListenPort.ToString();
        _tray = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "ぺかわん Windows リレー",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
        _tray.ContextMenuStrip.Items.Add("開く", null, (_, _) => Dispatcher.Invoke(ShowWindow));
        _tray.ContextMenuStrip.Items.Add("終了", null, async (_, _) =>
            await Dispatcher.InvokeAsync(() => ExitAsync()).Task.Unwrap());
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowWindow);
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();
        Loaded += async (_, _) => await RefreshAsync();
        StateChanged += (_, _) => { if (WindowState == WindowState.Minimized) Hide(); };
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exiting)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }

    private async Task RefreshAsync()
    {
        if (_refreshing || _exiting) return;
        _refreshing = true;
        try
        {
            await _server.EnsureBoundAsync();
            await _sessions.ExpireIdleAsync();
            var status = await _sessions.ViewAsync(_server.BaseUrl);
            VpnStatus.Text = _server.BaseUrl is null
                ? (_server.Error ?? "Tailscale を待っています")
                : "VPN配信: " + _server.BaseUrl;
            PeerStatus.Text = "PeerCastStation: " + status.RelayMessage;
            SessionStatus.Text = status.SessionId is null
                ? "スマホからの番組要求を待っています"
                : $"番組 {status.ChannelId} / 変換: {status.State}";
            SessionError.Text = status.Error ?? "";
            PairStatus.Text = _settings.HasPairedDevice ? "スマホ1台が登録されています" : "登録済みスマホはありません";
            StopButton.IsEnabled = status.SessionId is not null;
            _tray.Text = (status.State == "ready" ? "配信中" : "待機中") + " - ぺかわんリレー";
        }
        catch (Exception ex)
        {
            VpnStatus.Text = "状態を確認できません: " + ex.Message;
        }
        finally { _refreshing = false; }
    }

    private void Pair_Click(object sender, RoutedEventArgs e)
    {
        var payload = _server.NewPairPayload();
        if (payload is null)
        {
            PairExpires.Text = "Tailscale の接続後に作成できます";
            return;
        }
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(7);
        using var stream = new MemoryStream(bytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        PairQr.Source = bitmap;
        PairExpires.Text = "5分以内に読み取ってください。新しいQRを作ると前のコードは無効になります。";
    }

    private async void Revoke_Click(object sender, RoutedEventArgs e)
    {
        _settings.Revoke();
        await _sessions.StopAsync();
        PairQr.Source = null;
        PairExpires.Text = "登録端末を解除しました";
        await RefreshAsync();
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        await _sessions.StopAsync();
        await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.UpdatePeerCast(PeerUrl.Text, int.Parse(ListenPort.Text));
            await _sessions.StopAsync();
            await _server.EnsureBoundAsync();
            SettingsMessage.Text = "設定を保存しました";
            await RefreshAsync();
        }
        catch (Exception ex) { SettingsMessage.Text = ex.Message; }
    }

    private async Task ExitAsync()
    {
        if (_exiting) return;
        _exiting = true;
        _timer.Stop();
        await _server.DisposeAsync();
        await _sessions.DisposeAsync();
        _peerCast.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
