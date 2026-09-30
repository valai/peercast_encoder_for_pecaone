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
    private readonly System.Drawing.Icon _trayIcon;
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
        using (var iconStream = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/PecaOne_AppIcon.ico")).Stream)
        using (var icon = new System.Drawing.Icon(iconStream, new System.Drawing.Size(32, 32)))
        {
            _trayIcon = (System.Drawing.Icon)icon.Clone();
        }
        _tray = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "ぺかわん コネクト",
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
                ? (_server.Error is null
                    ? "スマホとの接続を準備しています"
                    : "スマホから接続できません。Tailscale の接続状態と設定を確認してください。")
                : "スマホから接続できる状態です";
            PeerStatus.Text = !status.PeerCastOnline
                ? "PeerCastStation に接続できません。起動状態と設定を確認してください。"
                : status.RelayReachable
                    ? $"PeerCastStation に接続しています。ほかの視聴者へリレーできます（リレー先: {status.DownstreamRelays} 件）"
                    : "PeerCastStation に接続しています。ほかの視聴者へのリレー設定を確認してください。";
            SessionStatus.Text = status.SessionId is null
                ? "「ぺかわん」からのチャンネルリクエストを待っています"
                : status.State switch
                {
                    "starting" => "「ぺかわん」での視聴を準備しています",
                    "ready" => "「ぺかわん」へチャンネルを配信しています",
                    "failed" => "チャンネルを配信できませんでした",
                    _ => "チャンネルの状態を確認しています"
                };
            SessionError.Text = status.State == "failed"
                ? "「ぺかわん」からチャンネルを選び直してください。改善しない場合は、PeerCastStation の接続状態を確認してください。"
                : "";
            PairStatus.Text = _settings.HasPairedDevice
                ? "「ぺかわん」とペアリング済みです"
                : "「ぺかわん」とのペアリングが必要です";
            StopButton.IsEnabled = status.SessionId is not null;
            var trayStatus = status.State switch
            {
                "starting" => "視聴準備中",
                "ready" => "配信中",
                "failed" => "配信エラー",
                _ => "待機中"
            };
            _tray.Text = trayStatus + " - ぺかわん コネクト";
        }
        catch (Exception)
        {
            VpnStatus.Text = "接続状態を確認できません。「状態を更新」を押して、もう一度お試しください。";
        }
        finally { _refreshing = false; }
    }

    private void Pair_Click(object sender, RoutedEventArgs e)
    {
        var payload = _server.NewPairPayload();
        if (payload is null)
        {
            PairExpires.Text = "Tailscale を接続してから、ペアリングQRを作成してください。";
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
        PairExpires.Text = "「ぺかわん」とのペアリングを解除しました";
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
        catch (Exception)
        {
            SettingsMessage.Text = "設定を保存できませんでした。PeerCastStation の接続先とスマホとの接続ポートを確認してください。";
        }
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
        _trayIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
