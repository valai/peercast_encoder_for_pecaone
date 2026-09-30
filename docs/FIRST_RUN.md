# ぺかわん コネクトを初めて使う方へ

Windows 10 22H2 以降または Windows 11 の64ビット版に対応しています。

1. ZIPを右クリックして「すべて展開」を選び、書き込み可能なフォルダへ展開します。
2. PeerCastStation と Tailscale を別途インストールします。PCとスマホを同じ Tailscale ネットワークへ接続してください。
3. 初回だけ、展開先の PowerShell で `./Install-FFmpeg.ps1` を実行します。FFmpeg 9.0 系を配布元の BtbN/FFmpeg-Builds から取得し、配布元が示す SHA-256 と一致したファイルだけを配置します。ネットワーク接続と、準備中の一時ファイルを含めて2GB程度の空き容量が必要です。
4. `PecaOneRelay.exe` を起動します。
5. 「ペアリングQRを作成」を押し、スマホアプリ「ぺかわん」で読み取ります。チャンネルはスマホ側で選択してください。

.NET ランタイムは同梱しています。FFmpeg は公開ZIPに含まれていません。初回の準備が完了した後は、FFmpegを毎回取得する必要はありません。

## スクリプトを実行できない場合の手動導入

1. [BtbN/FFmpeg-Builds の latest リリース](https://github.com/BtbN/FFmpeg-Builds/releases/tag/latest)を開き、`ffmpeg-n9.0-latest-win64-gpl-9.0.zip` を取得します。
2. アーカイブを展開し、その中の `bin/ffmpeg.exe` と `bin/ffprobe.exe`、ルートの `LICENSE.txt` を、アプリの展開先に作った `ffmpeg` フォルダへコピーします。
3. `PecaOneRelay.exe` を起動します。

## 接続できない場合

- PeerCastStation と Tailscale が起動していることを確認します。
- Windows ファイアウォールでは、Tailscale上で使用するTCP 17444への接続を許可します。公開インターネットへのポート転送は行わないでください。
- ほかの視聴者へのリレーを利用するには、PeerCastStation 側の外部リレーポートも設定します。
- 初期の接続先は `http://127.0.0.1:7144` です。PeerCastStation の設定が異なる場合は、アプリの「接続設定」を変更します。

## 配布ファイルの確認

公開元は [valai/peercast_encoder_for_pecaone の Releases](https://github.com/valai/peercast_encoder_for_pecaone/releases)です。`SHA256SUMS.txt` と、PowerShell の `Get-FileHash ./PecaOneConnect-v1.0.0-win-x64.zip -Algorithm SHA256` の結果を比較できます。

この版にはコード署名を付けていません。取得先とチェックサムを確認してください。Windowsのセキュリティ機能を無効にする必要はありません。
