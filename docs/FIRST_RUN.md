# ぺかわん コネクトを初めて使う方へ

Windows 10 22H2 以降または Windows 11 の64ビット版に対応しています。

[はじめての接続ガイド（PDF）](PecaOneConnect-v1.0.0-quick-start-ja.pdf)では、ZIP展開からTailscaleの導入・接続、スマホとのペアリング、視聴開始までを2ページで説明しています。[GitHubリリースからPDFをダウンロード](https://github.com/valai/peercast_encoder_for_pecaone/releases/download/v1.0.0/PecaOneConnect-v1.0.0-quick-start-ja.pdf)することもできます。

1. ZIPを右クリックして「すべて展開」を選び、書き込み可能なフォルダへ展開します。
2. PeerCastStation と Tailscale を別途インストールします。PCとスマホを同じ Tailscale ネットワークへ接続してください。
3. 展開直下の `ぺかわん コネクト.exe` を起動します。
4. 「ペアリングQRを作成」を押し、スマホアプリ「ぺかわん」で読み取ります。チャンネルはスマホ側で選択してください。

.NET ランタイムとFFmpegは同梱しています。別途取得したり、PowerShellのスクリプトを実行したりする必要はありません。展開直下は `ぺかわん コネクト.exe`、`はじめに.txt`、`app` フォルダの3つです。`app` をそのまま残し、移動するときはフォルダ全体を移動してください。デスクトップから起動したい場合は、EXEのショートカットを作成できます。

## ダウンロードするファイル

`PecaOneConnect-v1.0.0-portable-r3-win-x64.zip` が現在の推奨する同梱版です。r3ではアプリアイコンを更新しています。以前の同梱版やFFmpeg別取得版は既存の配布物として保持しています。初めて使う場合は `portable-r3` を選んでください。

同じリリースにあるソースコードのZIPは開発・改変用です。通常の利用では取得する必要はありません。

## 接続できない場合

- PeerCastStation と Tailscale が起動していることを確認します。
- Windows ファイアウォールでは、Tailscale上で使用するTCP 17444への接続を許可します。公開インターネットへのポート転送は行わないでください。
- ほかの視聴者へのリレーを利用するには、PeerCastStation 側の外部リレーポートも設定します。
- 初期の接続先は `http://127.0.0.1:7144` です。PeerCastStation の設定が異なる場合は、アプリの「接続設定」を変更します。

## 配布ファイルの確認

公開元は [valai/peercast_encoder_for_pecaone の Releases](https://github.com/valai/peercast_encoder_for_pecaone/releases)です。`SHA256SUMS-r3.txt` と、PowerShell の `Get-FileHash ./PecaOneConnect-v1.0.0-portable-r3-win-x64.zip -Algorithm SHA256` の結果を比較できます。

この版にはコード署名を付けていません。取得先とチェックサムを確認してください。Windowsのセキュリティ機能を無効にする必要はありません。
