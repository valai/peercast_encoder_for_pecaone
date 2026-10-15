ぺかわん コネクト v1.1.0 を公開します。Windowsへのサインイン時にアプリを起動する設定を追加しました。

### ダウンロードと起動

1. **[PecaOneConnect-v1.1.0-portable-win-x64.zip](https://github.com/valai/peercast_encoder_for_pecaone/releases/download/v1.1.0/PecaOneConnect-v1.1.0-portable-win-x64.zip)** を取得し、右クリックして「すべて展開」を選びます。
2. PeerCastStation と Tailscale を別途インストールし、PCとスマホを同じ Tailscale ネットワークへ接続します。
3. 展開直下の **ぺかわん コネクト.exe** を起動し、スマホアプリ「ぺかわん」でペアリングQRを読み取ります。

.NETランタイムとFFmpegを同梱しています。Windowsへのサインイン時にアプリを起動する設定は、「接続設定」で変更できます。初期状態はオフです。

### 配布物

- `PecaOneConnect-v1.1.0-portable-win-x64.zip`: Windows 10 22H2以降またはWindows 11、x64向けの同梱版。
- `PecaOneConnect-v1.1.0-source.zip`: リリース対象コミットのアプリ、起動用EXE、ビルドスクリプトのソース。
- `FFmpeg-9.0.2-x264-source.zip`、`QRCoder-1.8.0-source.zip`: 同梱バイナリの対応ソース。
- `SHA256SUMS.txt`: 配布ZIPのチェックサム。

アプリと起動用EXEはGPL-3.0-or-laterです。FFmpeg、QRCoder、.NETのライセンス本文と第三者通知はZIP内の `app/` に含まれています。コード署名は付けていません。ダウンロード後、`SHA256SUMS.txt` と照合してください。

PeerCastStationとTailscaleは別途必要です。接続設定を公開インターネットへ開放せず、Tailscale内で使用してください。
