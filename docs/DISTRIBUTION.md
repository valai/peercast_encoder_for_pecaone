# 配布と更新の手順

## アプリアイコン

`assets/PecaOne_AppIcon.png` はアイコンの原画像です。WindowsのEXE、ウィンドウ、通知領域と起動用EXEには、共通の `assets/PecaOne_AppIcon.ico` を使います。原画像を更新した場合はWindowsで `./scripts/Convert-AppIcon.ps1` を実行し、16・20・24・32・40・48・64・128・256ピクセルのICOを再生成してから、アプリ本体と起動用EXEをビルドします。

アイコン更新版のr3は `Build-PortableLayout.ps1 -UpdatedAppDirectory artifacts/updated-app` で、再ビルドしたアプリ本体を公開済みの同梱版に組み合わせて作成します。アプリ本体と起動用EXEの対応ソースをr3用のソースZIPに保存します。

## v1.0.0の配布物

- `PecaOneConnect-v1.0.0-portable-r3-win-x64.zip`: 推奨する同梱版。r3はアプリアイコンの更新版です。展開直下は起動用EXE、はじめに.txt、appフォルダ。アプリ、.NETランタイム、FFmpeg、ライセンスをappにまとめています。
- `PecaOneConnect-v1.0.0-r3-source.zip`: r3のアプリ本体・起動用EXE・アイコン・ビルドスクリプトの対応ソース。
- `SHA256SUMS-r3.txt`: r3の同梱版と対応ソースZIPのチェックサム。
- `PecaOneConnect-v1.0.0-portable-r2-win-x64.zip`: アイコン更新前の整理された同梱版。
- `PecaOneConnect-v1.0.0-launcher-source.zip`: 起動用EXEの正確なソース、ビルド・梱包スクリプト、ライセンス。
- `SHA256SUMS-r2.txt`: 整理した同梱版と起動用EXEのソースZIPのチェックサム。
- `FFmpeg-9.0.2-x264-source.zip`: 同梱するFFmpegとx264の対応ソース、ビルド手順、構成、ライセンス。
- `PecaOneConnect-v1.0.0-portable-win-x64.zip`、`SHA256SUMS-portable.txt`: 以前の同梱版とチェックサム。既存の配布物として保持します。
- `PecaOneConnect-v1.0.0-source.zip`: リリース対象コミットのアプリソースとビルドスクリプト。
- `QRCoder-1.8.0-source.zip`: NuGetパッケージが示すコミットのQRCoderソース。
- `PecaOneConnect-v1.0.0-win-x64.zip`、`SHA256SUMS.txt`: 当初のFFmpeg別取得版とそのチェックサム。既存の配布物として保持します。

同梱版の利用者はZIPを展開して、直下の `ぺかわん コネクト.exe` を起動します。FFmpegの取得やスクリプト実行は不要です。PeerCastStationとTailscaleは別途必要です。

## 現在の配布構成の再現

`.github/workflows/layout-release.yml` は、アプリ本体と起動用EXEをアイコン付きで再ビルドし、検証済みの同梱版のランタイムとFFmpegを再利用します。アプリ本体以外のEXE/DLLは元の配布物とのSHA-256一致を確認します。

1. Windowsで.NET SDK 10.0.401、Visual Studio C++ Build Tools（x64用C++ツール）、Windows SDKを用意します。
2. `./scripts/Build-Launcher.ps1` と `./scripts/Test-Launcher.ps1` を実行します。日本語・空白を含む展開先と、起動元の作業フォルダが異なる場合の動作を検証します。
3. 公開済み `PecaOneConnect-v1.0.0-portable-win-x64.zip` を `publish/` に取得します。同梱FFmpegを使って自動テストを実行し、`dotnet restore src/PecaOneRelay/PecaOneRelay.csproj -r win-x64 --configfile NuGet.Config` と `dotnet publish src/PecaOneRelay/PecaOneRelay.csproj -c Release -r win-x64 --self-contained true --no-restore -p:IncludeFfmpeg=false -o artifacts/updated-app` でアプリ本体を作成します。`./scripts/Build-PortableLayout.ps1 -UpdatedAppDirectory artifacts/updated-app` は公開済みのSHA-256と照合してから `portable-r3` を作成します。
4. `./scripts/Test-PortableLayout.ps1 -Archive publish/PecaOneConnect-v1.0.0-portable-r3-win-x64.zip -UpdatedAppDirectory artifacts/updated-app -LaunchApp` を実行します。ZIP構成・依存ファイルの一致・EXEのアイコン・FFmpeg動作・アプリのウィンドウ初期化を検証します。実行はアプリを使用中のPCを避け、CIなどの検証環境で行ってください。
5. ワークフローと同様に対象コミット全体の対応ソースZIPとチェックサムを作成し、対応ソース、同梱版、チェックサムの順に同じリリースへ添付します。以前の配布物とタグは保持し、FFmpegとQRCoderの対応ソースも引き続き提供します。

## 以前の同梱版の再現

アイコン更新前の同梱版では、アプリのEXE/DLLは当初のv1.0.0と同じです。対応するアプリソースは `v1.0.0` タグと `PecaOneConnect-v1.0.0-source.zip` で提供します。同梱版の作成は `.github/workflows/bundled-release.yml` で行います。

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

### 発行者名と本名の公開

一般のWindows PCで信頼されるコード署名証明書のCommon Nameには、認証された個人または組織の正式名称が必要です。個人の証明書に自由なハンドルネームだけを指定して、本名を隠す用途には使えません。組織名で署名するには、その名称の組織として認証を受ける必要があります。屋号だけで本名を隠せるとは限らないため、発行前に証明書の全Subject項目とWindowsでの発行者表示を認証局に確認してください。Azure Artifact Signingも任意のCN・Oへの変更には対応していません。

自己署名なら `CN=valai` などを指定できます。ただし、利用者のPCがその証明書を信頼しない限り、一般のPCで信頼される発行者にはなりません。SmartScreenの警告をなくす目的の代替にもなりません。信頼された証明書を使用しても、SmartScreenの評価によって警告が出る場合があります。

参考: [CA/Browser Forumコード署名要件の7.1.4.2.2](https://cabforum.org/working-groups/code-signing/requirements/)、[Artifact Signing FAQ](https://learn.microsoft.com/azure/artifact-signing/faq)、[New-SelfSignedCertificate](https://learn.microsoft.com/powershell/module/pki/new-selfsignedcertificate)。

## 次のバージョンを公開する場合

プロジェクトのバージョン、ZIP名、導入手順、リリースノート、ワークフローの対象タグを更新します。.NETランタイムやQRCoderを更新した場合は `licenses/` の本文と第三者ソースの参照も更新し、テスト後に新しいタグから公開します。
