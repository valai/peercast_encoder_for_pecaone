# ぺかわん Windows リレー

PeerCastStation による PeerCast リレーを Windows PC に任せ、Tailscale 経由のスマホへ高・中・低の HLS を送る WPF アプリです。スマホ側の連携機能はこのリポジトリには含めません。連携仕様は [docs/MOBILE_API.md](docs/MOBILE_API.md) を参照してください。

アプリ本体のライセンスは GPL-3.0-or-later です。第三者の配布条件は [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。

## 前提

- Windows 10 22H2 以降または Windows 11、x64。
- PeerCastStation 6.0.1。ローカル管理 API が使えること。PeerCast 網へのリレーには同アプリの外部ポート開放が必要です。
- Windows PC とスマホが同じ Tailscale ネットワークに参加していること。HLS 用 TCP 17444 は Tailscale 上で Windows ファイアウォールに許可してください。公開インターネットへ転送しないでください。
- 配布物には FFmpeg 9.0.2 GPL ビルドを同梱します。開発時は `scripts/prepare-ffmpeg.ps1` で取得します。

起動するとトレイで常駐します。ウィンドウを閉じても待機し、終了はトレイメニューから行います。Tailscale 接続時だけ API を開始します。スマホは QR からペアリングし、YP 一覧で選んだ FLV 番組の ID と tracker を API に送ります。Windows経由での視聴時には、既定のSPチャンネル一覧をWindowsから取得するAPIも提供します。SPで使うポートを変更している場合は、Windows PCのブラウザからSPの設定を確認してください。

## 配布 ZIP から起動

`publish/PecaOneRelay-win-x64.zip` を右クリックして「すべて展開」を選び、展開先の `PecaOneRelay.exe` を実行します。.NET ランタイムと FFmpeg は ZIP 内に入っているため、別途インストールする必要はありません。ZIP と同じ場所の `.sha256` ファイルはダウンロード後の整合性確認用です。展開せず ZIP 内から直接実行すると、必要な同梱ファイルを見つけられません。

## 開発環境から起動

.NET 10 SDK と PowerShell が必要です。PeerCastStation と Tailscale を起動した状態で、リポジトリのルートから次を実行します。FFmpeg が未取得ならスクリプトが準備し、NuGet 復元とビルドも行います。終了はトレイメニューから操作します。

```powershell
.\scripts\run-dev.ps1
```

SDK が `.tools/dotnet` にある場合はそれを使用し、なければ `PATH` 上の `dotnet.exe` を使用します。初回は NuGet と FFmpeg の取得にネットワーク接続が必要です。

## ビルド

.NET 10 SDK をインストールした PowerShell で実行します。SDK が `.tools/dotnet` にある場合もスクリプトが検出します。

```powershell
./scripts/prepare-ffmpeg.ps1
./scripts/build.ps1
```

`publish/win-x64` に .NET ランタイム、アプリ、FFmpeg を含む配布フォルダができ、`publish/PecaOneRelay-win-x64.zip` と SHA-256 ファイルも生成されます。PeerCastStation と Tailscale は別途必要です。FFmpeg のライセンスと対応ソースは [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。

## 検証

```powershell
./scripts/build.ps1 -TestOnly
```

自動テストは API の認証・競合と FFmpeg の HLS 出力を検証します。実際の外部下流リレーおよびモバイル回線からの視聴は、ポート開放済み PC とスマホ実機で確認してください。
