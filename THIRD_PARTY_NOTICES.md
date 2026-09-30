# Third-party notices

## FFmpeg

The recommended `PecaOneConnect-v1.0.0-portable-win-x64.zip` includes FFmpeg 9.0.2 and ffprobe, built by this repository from the exact sources below. The FFmpeg binaries are GPL-3.0-or-later (`--enable-gpl --enable-version3`) and include x264, which is GPL-2.0-or-later. FFmpeg copyright: the FFmpeg developers. x264 copyright notices and authors are retained in the supplied source files.

The complete corresponding-source package, **FFmpeg-9.0.2-x264-source.zip**, is available from the same release:

https://github.com/valai/peercast_encoder_for_pecaone/releases/tag/v1.0.0

It includes the two source archives used to build the executables, the unmodified build script and source pins, the configuration, compiler/package versions, source checksums, and license texts. The source need not be downloaded for normal application use.

- FFmpeg source: https://ffmpeg.org/releases/ffmpeg-9.0.2.tar.xz
- FFmpeg source SHA-256: `8c3850283eb25fa026482078a04051e0be17347b09ef81a0849bec15a96e002e`
- FFmpeg's release signature was verified with fingerprint `FCF986EA15E6E293A5644F10B4322F04D67658D8`.
- x264 source: https://code.videolan.org/videolan/x264/-/tree/0480cb05fa188d37ae87e8f4fd8f1aea3711f7ee
- Source/build details: `ffmpeg/BUILDINFO.txt`, `ffmpeg/README.md`, and the source ZIP.
- Complete FFmpeg license text: `ffmpeg/LICENSE.txt` and `ffmpeg/licenses/`.
- x264 license: `ffmpeg/licenses/x264-COPYING.txt`.
- MinGW/GCC copyright and runtime notices: `ffmpeg/licenses/*copyright.txt`.
- FFmpeg licensing information: https://ffmpeg.org/legal.html

The only external codec library is x264; no other third-party codec/rendering/device libraries are compiled in. The unmodified MinGW toolchain uses the Win32 thread model. The executables are statically linked and import only Windows system DLLs. There are no source patches. FFmpeg is a separate process invoked by the application.

The earlier `PecaOneConnect-v1.0.0-win-x64.zip` is retained for existing users. It excludes FFmpeg and has an optional setup script which downloads it directly from BtbN/FFmpeg-Builds. Development-only builds may also use that provider; they are not the binaries shipped in the portable package.

## QRCoder

QRCoder 1.8.0 is used to render the local pairing QR code. Its complete MIT license and copyright notices are included in `licenses/QRCoder-LICENSE.txt`.

- Project: https://github.com/Shane32/QRCoder
- Source commit recorded in the NuGet package: `443d5a1f76debf203b1e252efee6996a15d41f5c`
- Source: https://github.com/Shane32/QRCoder/tree/443d5a1f76debf203b1e252efee6996a15d41f5c
- `QRCoder-1.8.0-source.zip` is attached to the v1.0.0 release.

## .NET 10.0.12

The Windows ZIP includes the .NET, ASP.NET Core, and Windows Desktop runtimes. Their license texts and available third-party notices are supplied in `licenses/NET-Runtime-LICENSE.txt`, `licenses/NET-Runtime-THIRD-PARTY-NOTICES.txt`, `licenses/ASP-NET-Core-LICENSE.txt`, `licenses/ASP-NET-Core-THIRD-PARTY-NOTICES.txt`, and `licenses/Windows-Desktop-LICENSE.txt`.

Projects: https://github.com/dotnet/runtime, https://github.com/dotnet/aspnetcore, https://github.com/dotnet/wpf

## PeerCastStation

PeerCastStation is installed separately and is not included in this application's package. Project and license: https://github.com/kumaryu/peercaststation

Tailscale is also installed separately: https://tailscale.com/download

The application itself is GPL-3.0-or-later (`LICENSE`). Its exact source is provided in the same release as the public Windows ZIP.
