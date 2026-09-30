#!/usr/bin/env bash
set -euo pipefail
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
source "$script_dir/sources.env"
work=$(realpath -m "${1:-$PWD/.tools/ffmpeg-build}")
out=$(realpath -m "${2:-$PWD/artifacts}")
mkdir -p "$work/sources" "$work/build" "$work/prefix" "$out/ffmpeg-win-x64/licenses"
src="$work/sources"
prefix="$work/prefix"
binary="$out/ffmpeg-win-x64"
export SOURCE_DATE_EPOCH=1789729200
export LC_ALL=C
export TZ=UTC
export CC=x86_64-w64-mingw32-gcc-win32
export PKG_CONFIG_LIBDIR="$prefix/lib/pkgconfig"
export PKG_CONFIG_PATH="$PKG_CONFIG_LIBDIR"
jobs=${BUILD_JOBS:-$(nproc)}

if [[ ! -f "$src/ffmpeg-$FFMPEG_VERSION.tar.xz" ]]; then
    curl --fail --location --retry 3 "$FFMPEG_URL" -o "$src/ffmpeg-$FFMPEG_VERSION.tar.xz"
fi
printf '%s  %s\n' "$FFMPEG_SHA256" "$src/ffmpeg-$FFMPEG_VERSION.tar.xz" | sha256sum --check
if [[ ! -f "$src/x264-$X264_COMMIT.tar.gz" ]]; then
    git init --bare "$work/x264.git"
    git -C "$work/x264.git" fetch --depth=1 "$X264_REPOSITORY" "$X264_COMMIT"
    test "$(git -C "$work/x264.git" rev-parse FETCH_HEAD)" = "$X264_COMMIT"
    git -C "$work/x264.git" archive --format=tar.gz --prefix=x264/ \
        --output="$src/x264-$X264_COMMIT.tar.gz" FETCH_HEAD
fi
tar -xf "$src/ffmpeg-$FFMPEG_VERSION.tar.xz" -C "$work/build"
tar -xf "$src/x264-$X264_COMMIT.tar.gz" -C "$work/build"
cd "$work/build/x264"
./configure --host=x86_64-w64-mingw32 --cross-prefix=x86_64-w64-mingw32- \
    --prefix="$prefix" --enable-static --disable-cli --disable-opencl \
    --disable-lavf --disable-swscale --bit-depth=8 --chroma-format=420
make -j"$jobs"
make install
cd "$work/build/ffmpeg-$FFMPEG_VERSION"
./configure --arch=x86_64 --target-os=mingw32 --cross-prefix=x86_64-w64-mingw32- \
    --cc="$CC" --prefix="$prefix" --pkg-config=pkg-config --pkg-config-flags=--static \
    --extra-cflags="-I$prefix/include" --extra-ldflags="-L$prefix/lib -static" \
    --extra-libs=-static-libgcc --disable-autodetect --disable-shared --enable-static \
    --enable-gpl --enable-version3 --enable-libx264 --enable-schannel \
    --disable-debug --disable-doc --disable-ffplay --disable-everything \
    --enable-ffmpeg --enable-ffprobe --enable-network --enable-w32threads \
    --enable-decoder=h264,hevc,flv,vp6,vp6a,vp6f,aac,aac_fixed,mp3,mp3float,nellymoser,adpcm_swf,pcm_s16le,pcm_s16be \
    --enable-encoder=libx264,aac --enable-demuxer=flv,mpegts,mov \
    --enable-muxer=flv,hls,mpegts --enable-parser=aac,aac_latm,h264,hevc,mpegaudio \
    --enable-protocol=file,http,https,tcp,tls,pipe,crypto \
    --enable-filter=split,scale,format,null,anull,aresample,aformat,testsrc2,sine \
    --enable-indev=lavfi
make -j"$jobs"
make install
cp "$prefix/bin/ffmpeg.exe" "$prefix/bin/ffprobe.exe" "$binary/"
cp COPYING.GPLv3 "$binary/LICENSE.txt"
cp COPYING.GPLv2 COPYING.GPLv3 COPYING.LGPLv2.1 COPYING.LGPLv3 LICENSE.md "$binary/licenses/"
cp "$work/build/x264/COPYING" "$binary/licenses/x264-COPYING.txt"
for package in mingw-w64-common mingw-w64-x86-64-dev gcc-mingw-w64-base gcc-mingw-w64-x86-64-win32; do
    cp "/usr/share/doc/$package/copyright" "$binary/licenses/$package-copyright.txt"
done
cp "$script_dir/README.md" "$binary/README.md"
{
    printf 'FFmpeg version: %s\nx264 commit: %s\n' "$FFMPEG_VERSION" "$X264_COMMIT"
    printf 'Packaging commit: %s\n' "${GITHUB_SHA:-local}"
    printf 'Compiler: '; "$CC" --version | head -n 1
    dpkg-query -W -f='${Package} ${Version}\n' gcc-mingw-w64-x86-64-win32 mingw-w64-common nasm make pkg-config
    printf '\nFFmpeg configure command:\n'; cat ffbuild/config.sh
    printf '\nx264 build configuration:\n'; cat "$work/build/x264/config.mak"
    printf '\nWindows DLL imports:\n'
    x86_64-w64-mingw32-objdump -p "$binary/ffmpeg.exe" "$binary/ffprobe.exe" | grep 'DLL Name:'
} > "$binary/BUILDINFO.txt"
if x86_64-w64-mingw32-objdump -p "$binary/ffmpeg.exe" "$binary/ffprobe.exe" | \
    grep -Ei 'DLL Name:.*(libgcc|libstdc|libwinpthread|cygwin|msys|x264|avcodec|avformat)'; then
    echo 'An extra runtime DLL is required; refusing to package.' >&2
    exit 1
fi
source_bundle="$out/FFmpeg-9.0.2-x264-source"
mkdir -p "$source_bundle/sources" "$source_bundle/scripts" "$source_bundle/build-info"
cp "$src/ffmpeg-$FFMPEG_VERSION.tar.xz" "$src/x264-$X264_COMMIT.tar.gz" "$source_bundle/sources/"
cp "$script_dir/build-win-x64.sh" "$script_dir/sources.env" "$script_dir/README.md" "$source_bundle/scripts/"
cp "$binary/BUILDINFO.txt" ffbuild/config.log ffbuild/config.sh "$source_bundle/build-info/"
cp -r "$binary/licenses" "$source_bundle/"
(cd "$source_bundle" && sha256sum sources/* > SOURCES.sha256)
(cd "$binary" && sha256sum ffmpeg.exe ffprobe.exe > SHA256SUMS.txt)
(cd "$out" && zip -qr FFmpeg-9.0.2-x264-source.zip FFmpeg-9.0.2-x264-source)
echo 'Windows binaries and their corresponding source are ready.'
