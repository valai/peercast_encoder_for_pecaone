# Third-party notices

## FFmpeg

The public v1.0.0 Windows ZIP does not include FFmpeg executables. `Install-FFmpeg.ps1` obtains the Windows x64 GPL build from the FFmpeg 9.0 release branch directly from [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases/tag/latest). It verifies the archive against the provider's GitHub SHA-256 digest, copies the provider's `LICENSE.txt`, and records the URL and digest in `ffmpeg/ORIGIN.json`.

The application has been tested with FFmpeg **n9.0.2-20260919**, build tag `autobuild-2026-09-19-13-11`:

- Binary archive: https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-09-19-13-11/ffmpeg-n9.0.2-win64-gpl-9.0.zip
- Archive SHA-256: `44083538105B4E64D439F9E67BD875BD264B4271239C808B2ACEA09773AD1AA3`
- Corresponding FFmpeg source: https://ffmpeg.org/releases/ffmpeg-9.0.2.tar.xz
- Build scripts and third-party component recipes: https://github.com/BtbN/FFmpeg-Builds/tree/3e6685e
- License text shipped in `ffmpeg/LICENSE.txt`; FFmpeg licensing information: https://ffmpeg.org/legal.html

FFmpeg and its encoders are separate programs invoked by this application. These source references do not represent a verified complete corresponding-source bundle for the static binary. Before redistributing FFmpeg binaries, obtain and provide the exact source, included dependency sources, patches, build instructions, and license texts, following GPL section 6. See `docs/DISTRIBUTION.md` in the source tree.

Daily binary releases have limited retention. The installer uses the maintained `latest` release and the 9.0 branch, with digest verification, instead of relying on an expiring daily tag.

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
