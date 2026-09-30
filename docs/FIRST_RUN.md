# ぺかわん コネクトを初めて使う方へ

Windows 10 22H2 以降または Windows 11 の64ビット版に対応しています。

1. ZIPを右クリックして「すべて展開」を選び、書き込み可能なフォルダへ展開します。
2. PeerCastStation と Tailscale を別途インストールします。PCとスマホを同じ Tailscale ネットワークへ接続してください。
3. `PecaOneRelay.exe` を起動します。
4. 「ペアリングQRを作成」を押し、スマホアプリ「ぺかわん」で読み取ります。チャンネルはスマホ側で選択してください。

.NET ランタイムとFFmpegは同梱しています。別途取得したり、PowerShellのスクリプトを実行したりする必要はありません。展開したファイルはフォルダごと保持してください。

## ダウンロードするファイル

`PecaOneConnect-v1.0.0-portable-win-x64.zip` が同梱版です。以前の `PecaOneConnect-v1.0.0-win-x64.zip` はFFmpegを別途取得する構成のため、初めて使う場合は同梱版を選んでください。

同じリリースにあるソースコードのZIPは開発・改変用です。通常の利用では取得する必要はありません。

## 接続できない場合

- PeerCastStation と Tailscale が起動していることを確認します。
- Windows ファイアウォールでは、Tailscale上で使用するTCP 17444への接続を許可します。公開インターネットへのポート転送は行わないでください。
- ほかの視聴者へのリレーを利用するには、PeerCastStation 側の外部リレーポートも設定します。
- 初期の接続先は `http://127.0.0.1:7144` です。PeerCastStation の設定が異なる場合は、アプリの「接続設定」を変更します。

## 配布ファイルの確認

公開元は [valai/peercast_encoder_for_pecaone の Releases](https://github.com/valai/peercast_encoder_for_pecaone/releases)です。`SHA256SUMS-portable.txt` と、PowerShell の `Get-FileHash ./PecaOneConnect-v1.0.0-portable-win-x64.zip -Algorithm SHA256` の結果を比較できます。

この版にはコード署名を付けていません。取得先とチェックサムを確認してください。Windowsのセキュリティ機能を無効にする必要はありません。
