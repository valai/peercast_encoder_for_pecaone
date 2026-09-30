# FFmpeg used in the portable Windows package

These binaries are built by this repository, from FFmpeg 9.0.2 and x264 commit
`0480cb05fa188d37ae87e8f4fd8f1aea3711f7ee`. FFmpeg is configured with GPL and
version 3 enabled; the resulting FFmpeg executables are GPL-3.0-or-later.
x264 is GPL-2.0-or-later. Complete source archives, the build script,
configuration and license texts are supplied in `FFmpeg-9.0.2-x264-source.zip`
on the same v1.0.0 GitHub release:

https://github.com/valai/peercast_encoder_for_pecaone/releases/tag/v1.0.0

Copyright notices are retained in the supplied source files. FFmpeg copyright:
the FFmpeg developers. x264 copyright: the x264 authors named in its source files.
See `LICENSE.txt`, `licenses/` and `BUILDINFO.txt` next to the binaries.

## Build from the supplied source

Use Ubuntu 24.04 x86_64 and install the standard distribution tools:

```sh
sudo apt-get update
sudo apt-get install -y gcc-mingw-w64-x86-64-win32 mingw-w64-x86-64-dev nasm make pkg-config curl git xz-utils zip
```

Extract this source ZIP, then run from its root:

```sh
sha256sum --check SOURCES.sha256
mkdir -p work/sources
cp sources/* work/sources/
bash scripts/build-win-x64.sh "$PWD/work" "$PWD/output"
```

The script uses the included source archives when present. Network access to
FFmpeg or x264 is not required when rebuilding from this source bundle.
`BUILDINFO.txt` records the original compiler/package versions and configure
options. The toolchain is unmodified, uses the Win32 thread model, and the
executables only import Windows system DLLs. The included MinGW/GCC notices
describe their copyright and runtime-library exceptions.

The build supports the app's FLV input, H.264/AAC output, 1080p/480p/240p
conversion, and HTTP/HTTPS streams. It does not include unrelated external
codec, rendering or device libraries. The app still uses the same API and
playlists. There are no local source patches.
