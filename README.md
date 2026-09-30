# ぺかわん コネクト

PeerCastStation による PeerCast リレーを Windows PC に任せ、Tailscale 経由のスマホへ1080p・480p・240pの HLS を送る WPF アプリです。解像度は画質ごとの上限で、元の映像を拡大しません。対応するスマホアプリでQRを読み取り、ペアリングして視聴できます。スマホアプリ本体はこのリポジトリには含まれません。連携仕様は [docs/MOBILE_API.md](docs/MOBILE_API.md) を参照してください。

アプリ本体のライセンスは GPL-3.0-or-later です。第三者の配布条件は [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。

## 前提

- Windows 10 22H2 以降または Windows 11、x64。
- PeerCastStation 6.0.1。ローカル管理 API が使えること。PeerCast 網へのリレーには同アプリの外部ポート開放が必要です。
- Windows PC とスマホが同じ Tailscale ネットワークに参加していること。HLS 用 TCP 17444 は Tailscale 上で Windows ファイアウォールに許可してください。公開インターネットへ転送しないでください。
- 公開する同梱版にはFFmpeg 9.0.2が含まれています。開発時は `scripts/prepare-ffmpeg.ps1` で準備できます。

起動するとトレイで常駐します。ウィンドウを閉じても待機し、終了はトレイメニューから行います。Tailscale 接続時だけ API を開始します。スマホは QR からペアリングし、YP 一覧で選んだ FLV チャンネルの ID と tracker を API に送ります。Windows経由での視聴時には、既定のSPチャンネル一覧をWindowsから取得するAPIも提供します。SPで使うポートを変更している場合は、Windows PCのブラウザからSPの設定を確認してください。

## 配布 ZIP から起動

[GitHub Releases](https://github.com/valai/peercast_encoder_for_pecaone/releases/tag/v1.0.0)から `PecaOneConnect-v1.0.0-portable-r3-win-x64.zip` を取得し、右クリックして「すべて展開」を選び、展開直下の `ぺかわん コネクト.exe` を起動してください。.NETランタイムとFFmpegを同梱しているため、別途取得やスクリプト実行は不要です。PeerCastStationとTailscaleは別途必要です。r3ではEXE・ウィンドウ・通知領域のアイコンを「ぺかわん」の画像に更新しています。

展開直下には `ぺかわん コネクト.exe`、`はじめに.txt`、`app/` の3つだけを置き、アプリ本体・DLL・FFmpeg・ライセンスは `app/` にまとめています。移動するときはフォルダ全体を移動してください。デスクトップにはEXEのショートカットを作成できます。

導入手順と接続設定は [docs/FIRST_RUN.md](docs/FIRST_RUN.md) を参照してください。`SHA256SUMS-r3.txt` はダウンロード後の整合性確認用です。展開せずZIP内から直接実行すると、必要なファイルを見つけられません。

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

通常のビルドでは `publish/win-x64` に .NET ランタイム、アプリ、FFmpeg を含むローカル確認用フォルダを作り、`publish/PecaOneRelay-win-x64.zip` と SHA-256 ファイルを生成します。

同梱版は `scripts/ffmpeg/build-win-x64.sh` でFFmpegと対応ソースを作成し、Windowsでテスト後、`scripts/Build-BundledPackage.ps1` で公開済みv1.0.0のアプリに同梱します。`publish/PecaOneConnect-v1.0.0-portable-win-x64.zip` とFFmpegの対応ソースZIP、チェックサムを作成します。詳細は [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) を参照してください。

現在の配布構成は、Visual Studio C++ Build ToolsとWindows SDKのあるWindowsで起動用EXEとアプリ本体をビルドし、公開済みの同梱版にアプリ本体を組み合わせて作成します。`scripts/Build-PortableLayout.ps1 -UpdatedAppDirectory artifacts/updated-app` と、対応する `scripts/Test-PortableLayout.ps1` で梱包と起動を検証します。具体的なコマンドと公開処理は `.github/workflows/layout-release.yml` と [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) にあります。

`./scripts/build.ps1 -ForRelease` は当初のFFmpeg別取得版を生成するコマンドです。公開済みのファイルを再配布するときは、ビルドに対応するソースを必ず揃えてください。

## 検証

```powershell
./scripts/build.ps1 -TestOnly
```

スマホアプリとの連携・視聴は実機で確認済みです。自動テストは API の認証・競合と FFmpeg の HLS 出力を検証します。外部下流リレーやモバイル回線での動作確認には、ポート開放済み PC とスマホ実機を使用してください。
