ぺかわん コネクトの最初の公開版です。PCで受信したチャンネルを、スマホアプリ「ぺかわん」で視聴できます。

- QRコードによるペアリングに対応。
- 1080p・480p・240pと自動画質に対応。解像度は上限で、元の映像を拡大しません。
- チャンネルの選択はスマホ側で行います。
- Windows 10 22H2以降またはWindows 11の64ビット版に対応。

### ダウンロードと起動

1. **[PecaOneConnect-v1.0.0-portable-win-x64.zip](https://github.com/valai/peercast_encoder_for_pecaone/releases/download/v1.0.0/PecaOneConnect-v1.0.0-portable-win-x64.zip)** を取得し、「すべて展開」で展開します。
2. PeerCastStationとTailscaleを別途インストールし、PCとスマホを同じTailscaleネットワークへ接続します。
3. **PecaOneRelay.exe** を起動し、スマホアプリ「ぺかわん」でペアリングQRを読み取ります。

.NETランタイムとFFmpegを同梱しています。FFmpegの取得やスクリプト実行は不要です。アプリ本体は当初のv1.0.0と同じものです。

以前の **PecaOneConnect-v1.0.0-win-x64.zip** も保持していますが、FFmpegを別途取得する構成のため、通常は上記の同梱版を選んでください。

### 配布について

- アプリはGPL-3.0-or-laterです。対応するアプリソースとQRCoder 1.8.0のソースを同じリリースに添付しています。同梱FFmpegのソース・x264のソース・ビルド手順は **FFmpeg-9.0.2-x264-source.zip** に含めています。ライセンス本文はZIP内の `LICENSE`、`THIRD_PARTY_NOTICES.md`、`licenses/`、`ffmpeg/licenses/` を参照してください。ソースZIPは通常の利用では取得不要です。
- この版はコード署名を付けていません。公式リリースから取得し、必要に応じて **SHA256SUMS-portable.txt** とファイルのSHA-256を比較してください。
- スマホ側の画質名が旧表示の場合は、`high`が1080p、`medium`が480p、`low`が240pに対応します。スマホアプリ側の表示更新は別途行います。
- 接続設定を公開インターネットへ開放せず、Tailscale内で使用してください。

同梱するWindows版FFmpegを使い、認証・セッション制御・映像出力・音声なしのチャンネルを含む14件の自動テストを実施しています。
