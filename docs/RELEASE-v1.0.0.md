ぺかわん コネクトの最初の公開版です。PCで受信したチャンネルを、スマホアプリ「ぺかわん」で視聴できます。

- QRコードによるペアリングに対応。
- 1080p・480p・240pと自動画質に対応。解像度は上限で、元の映像を拡大しません。
- チャンネルの選択はスマホ側で行います。
- Windows 10 22H2以降またはWindows 11の64ビット版に対応。

### ダウンロードと起動

1. **[PecaOneConnect-v1.0.0-portable-r2-win-x64.zip](https://github.com/valai/peercast_encoder_for_pecaone/releases/download/v1.0.0/PecaOneConnect-v1.0.0-portable-r2-win-x64.zip)** を取得し、「すべて展開」で展開します。
2. PeerCastStationとTailscaleを別途インストールし、PCとスマホを同じTailscaleネットワークへ接続します。
3. 展開直下の **ぺかわん コネクト.exe** を起動し、スマホアプリ「ぺかわん」でペアリングQRを読み取ります。

.NETランタイムとFFmpegを同梱しています。FFmpegの取得やスクリプト実行は不要です。展開直下は **ぺかわん コネクト.exe**、**はじめに.txt**、**app** フォルダの3つに整理しました。`app` はそのまま残し、移動するときはフォルダ全体を移動してください。デスクトップにはEXEのショートカットを作成できます。アプリ本体は当初のv1.0.0と同じものです。

以前の配布物も保持しています。通常は上記の **portable-r2** を選んでください。

### 配布について

- アプリと起動用EXEはGPL-3.0-or-laterです。アプリ本体のソース、起動用EXEのソース **PecaOneConnect-v1.0.0-launcher-source.zip**、QRCoder 1.8.0のソースを同じリリースに添付しています。同梱FFmpegのソース・x264のソース・ビルド手順は **FFmpeg-9.0.2-x264-source.zip** に含めています。ライセンス本文はZIP内の `app/LICENSE`、`app/THIRD_PARTY_NOTICES.md`、`app/licenses/`、`app/ffmpeg/licenses/` を参照してください。ソースZIPは通常の利用では取得不要です。
- この版はコード署名を付けていません。公式リリースから取得し、必要に応じて **SHA256SUMS-r2.txt** とファイルのSHA-256を比較してください。
- スマホ側の画質名が旧表示の場合は、`high`が1080p、`medium`が480p、`low`が240pに対応します。スマホアプリ側の表示更新は別途行います。
- 接続設定を公開インターネットへ開放せず、Tailscale内で使用してください。

同梱するWindows版FFmpegを使い、認証・セッション制御・映像出力・音声なしのチャンネルを含む14件の自動テストを実施しています。

整理した配布構成では、日本語・空白を含む展開先や異なる作業フォルダからの起動、FFmpegの動作、アプリのウィンドウ初期化を追加検証しています。元のアプリとFFmpegの全EXE/DLLが同じであることもSHA-256で確認しています。
