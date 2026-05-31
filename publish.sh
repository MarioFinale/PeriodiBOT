#!/usr/bin/env bash
# publish-all.sh - Build single-file self-contained executables for Windows, Linux and macOS

set -e

# ================== CONFIGURATION ==================
PROJECT="./PeriodiBOT-IRC/PeriodiBOT-IRC.vbproj"
CONFIG="Release"
OUTPUT="./publish"

RIDS=(
  "win-x64"
  "win-arm64"
  "linux-x64"
  "linux-arm64"
  "osx-x64"
  "osx-arm64"
)
# ==================================================

echo "Cleaning old publish folder..."
rm -rf "$OUTPUT"

for RID in "${RIDS[@]}"; do
    echo "========================================"
    echo "Publishing for $RID ..."
    echo "========================================"

    dotnet publish "$PROJECT" \
        -c "$CONFIG" \
        -r "$RID" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "$OUTPUT/$RID" \
        --nologo

    echo "$RID completed → $OUTPUT/$RID/"
done

echo ""
echo "All builds finished!"
echo "Output folder: $OUTPUT/"
ls -l "$OUTPUT"