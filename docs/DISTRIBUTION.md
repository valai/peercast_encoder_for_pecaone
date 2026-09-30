# 配布と更新の手順

## v1.0.0の配布物

- `PecaOneConnect-v1.0.0-portable-r2-win-x64.zip`: 推奨する同梱版。展開直下は起動用EXE、はじめに.txt、appフォルダ。アプリ、.NETランタイム、FFmpeg、ライセンスをappにまとめています。
- `PecaOneConnect-v1.0.0-launcher-source.zip`: 起動用EXEの正確なソース、ビルド・梱包スクリプト、ライセンス。
- `SHA256SUMS-r2.txt`: 整理した同梱版と起動用EXEのソースZIPのチェックサム。
- `FFmpeg-9.0.2-x264-source.zip`: 同梱するFFmpegとx264の対応ソース、ビルド手順、構成、ライセンス。
- `PecaOneConnect-v1.0.0-portable-win-x64.zip`、`SHA256SUMS-portable.txt`: 以前の同梱版とチェックサム。既存の配布物として保持します。
- `PecaOneConnect-v1.0.0-source.zip`: リリース対象コミットのアプリソースとビルドスクリプト。
- `QRCoder-1.8.0-source.zip`: NuGetパッケージが示すコミットのQRCoderソース。
- `PecaOneConnect-v1.0.0-win-x64.zip`、`SHA256SUMS.txt`: 当初のFFmpeg別取得版とそのチェックサム。既存の配布物として保持します。

同梱版の利用者はZIPを展開して、直下の `ぺかわん コネクト.exe` を起動します。FFmpegの取得やスクリプト実行は不要です。PeerCastStationとTailscaleは別途必要です。

## 現在の配布構成の再現

`.github/workflows/layout-release.yml` は、検証済みの同梱版を再利用してフォルダ構成を整理します。アプリ本体とFFmpegのEXE/DLLを再ビルドせず、元のファイルとのSHA-256一致を確認します。

1. WindowsでVisual Studio C++ Build Tools（x64用C++ツール）とWindows SDKを用意します。
2. `./scripts/Build-Launcher.ps1` と `./scripts/Test-Launcher.ps1` を実行します。日本語・空白を含む展開先と、起動元の作業フォルダが異なる場合の動作を検証します。
3. 公開済み `PecaOneConnect-v1.0.0-portable-win-x64.zip` を `publish/` に取得し、`./scripts/Build-PortableLayout.ps1` を実行します。公開済みのSHA-256と照合してから `portable-r2` を作成します。
4. `./scripts/Test-PortableLayout.ps1 -LaunchApp` を実行します。ZIP構成・元の全EXE/DLLの一致・FFmpeg動作・アプリのウィンドウ初期化を検証します。実行はアプリを使用中のPCを避け、CIなどの検証環境で行ってください。
5. ワークフローと同様に対象コミットの起動用EXEの対応ソースZIPとチェックサムを作成し、対応ソース、同梱版、チェックサムの順に同じリリースへ添付します。FFmpegとQRCoder、アプリ本体の対応ソースも引き続き提供します。

## リリースの再現

アプリのEXE/DLLは当初のv1.0.0と同じです。対応するアプリソースは `v1.0.0` タグと `PecaOneConnect-v1.0.0-source.zip` で提供します。同梱版の作成は `.github/workflows/bundled-release.yml` で行います。

1. Ubuntu 24.04で `scripts/ffmpeg/README.md` に記載したMinGWとビルドツールを用意します。
2. `bash scripts/ffmpeg/build-win-x64.sh "$PWD/.tools/ffmpeg-build" "$PWD/artifacts"` を実行し、Windows用FFmpegと対応ソースZIPを作成します。元のソースから再ビルドするときは、FFmpegソースZIP内の手順に従ってください。
3. Windowsへ `artifacts/ffmpeg-win-x64` と `artifacts/FFmpeg-9.0.2-x264-source.zip` をコピーします。
4. FFmpegフォルダの内容を `vendor/ffmpeg` に配置し、.NET SDK 10.0.401で `./scripts/build.ps1 -TestOnly` を実行します。
5. 公開済みの `PecaOneConnect-v1.0.0-win-x64.zip` を `publish/` に取得し、`./scripts/Build-BundledPackage.ps1` を実行します。アプリZIPのSHA-256を公開済みの値と照合し、FFmpegと対応ソースの存在・チェックサムを確認してから同梱します。
6. 同梱版ZIP、FFmpegのソースZIP、`SHA256SUMS-portable.txt` を同じリリースに添付します。既存の配布物とタグは上書きしません。

## FFmpegを同梱して再配布する場合

FFmpegのGPL版を配布する場合は、GPL第6条に沿った対応ソースの提供が必要です。FFmpeg本体のリリースソースだけでなく、バイナリに静的リンクされた各依存ライブラリ、適用したパッチ、ビルドスクリプトと手順、ライセンス本文を揃えます。

1. 配布元から、そのバイナリを生成した完全な対応ソース一式と依存ライブラリの正確なバージョンを入手します。
2. 取得したソース・パッチ・設定で、配布するバイナリとの対応を確認します。
3. 同じリリースに対応ソースを添付するか、GPL第6条に沿って同等の取得手段を明示し、必要な期間利用できるように維持します。
4. ライセンスとソース取得先をZIPとリリース説明に記載してから、バイナリを含むZIPを公開します。

同梱版ではFFmpeg 9.0.2と固定コミットのx264だけを用いてビルドしています。ソースZIPには使用したソースアーカイブそのもの、ビルドスクリプト、構成、ライセンスを保存します。`BUILDINFO.txt` はコンパイラとツールのバージョン、WindowsシステムDLL以外への依存がないことを記録します。

通常の `build.ps1` がBtbNのFFmpegを利用して生成するZIPは、対応ソースを揃えていないローカル確認用です。同梱版の代わりにそのZIPを公開しないでください。

参考: [GPL第6条](https://www.gnu.org/licenses/gpl-3.0.html#section6)、[FFmpegのライセンス案内](https://ffmpeg.org/legal.html)。商用利用や特許など個別の法的判断が必要な場合は専門家へ確認してください。

## コード署名

v1.0.0にはコード署名を付けていません。署名用の証明書・秘密鍵をこの作業では使用していないため、発行者側で次の作業が必要です。

1. Windows向けコード署名用の証明書または署名サービスを用意します。
2. ZIPを作る前に、署名ツールでアプリのEXE/DLLへ署名し、タイムスタンプを付けます。
3. 署名を検証してからZIPとチェックサムを作り直します。
4. CIを利用する場合は、証明書や署名サービスの認証情報をGitHub Secretsなどで管理します。秘密鍵をリポジトリに入れません。

参考: [Microsoft SignTool](https://learn.microsoft.com/windows/win32/seccrypto/signtool)。チェックサムはダウンロードの整合性確認用で、コード署名とは異なります。

## 次のバージョンを公開する場合

プロジェクトのバージョン、ZIP名、導入手順、リリースノート、ワークフローの対象タグを更新します。.NETランタイムやQRCoderを更新した場合は `licenses/` の本文と第三者ソースの参照も更新し、テスト後に新しいタグから公開します。
