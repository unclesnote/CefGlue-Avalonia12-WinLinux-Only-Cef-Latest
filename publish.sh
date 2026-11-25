#!/usr/bin/env bash
# Builds all Avalonia/WinLinux CefGlue.Next NuGet packages and saves them into
# Nuget/output/<YYYYMMDD>_<HHMM>/ without renaming the .nupkg files.
#
#   ./publish.sh                      # Release
#   CONFIGURATION=Debug ./publish.sh
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

UNAME_S="$(uname -s)"
case "$UNAME_S" in
    Linux*|MINGW*|MSYS*|CYGWIN*)
        ;;
    *)
        echo "Unsupported OS: $UNAME_S (this fork supports Avalonia on Windows and Linux x64 only)" >&2
        exit 1
        ;;
esac

CONFIGURATION="${CONFIGURATION:-Release}"
VERSION="$(sed -n 's/.*"cefglue_version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT_DIR/cef-version.json" | head -n 1 | tr -d '\r[:space:]')"
if [[ -z "$VERSION" ]]; then
    echo "Error: could not read cefglue_version from $ROOT_DIR/cef-version.json" >&2
    exit 1
fi

OUTPUT_DIR="$ROOT_DIR/Nuget/output"
TIMESTAMP="$(date +%Y%m%d_%H%M)"
RELEASE_DIR="$OUTPUT_DIR/$TIMESTAMP"
PACKAGE_IDS=(
    "CefGlue.Next.Core"
    "CefGlue.Next.Common.Shared"
    "CefGlue.Next.BrowserProcess.Core"
    "CefGlue.Next.Common"
    "CefGlue.Next.Avalonia"
)

echo "Packing: Configuration=$CONFIGURATION, Version=$VERSION -> $RELEASE_DIR"

# Remove any .nupkg files directly under Nuget/output before building (timestamped subfolders are kept).
rm -f "$OUTPUT_DIR"/*.nupkg

# Building CefGlue.Avalonia builds and packs all five packages into Nuget/output.
dotnet build "$ROOT_DIR/CefGlue.Avalonia/CefGlue.Avalonia.csproj" \
    -c "$CONFIGURATION" \
    --no-incremental

mkdir -p "$RELEASE_DIR"
FINAL_PACKAGES=()

for id in "${PACKAGE_IDS[@]}"; do
    src="$OUTPUT_DIR/$id.$VERSION.nupkg"
    if [[ ! -f "$src" ]]; then
        echo "Error: package not created: $src" >&2
        exit 1
    fi
    dst="$RELEASE_DIR/$id.$VERSION.nupkg"
    mv -f "$src" "$dst"
    FINAL_PACKAGES+=("$dst")
done

HEAD_HASH="$(git -C "$ROOT_DIR" rev-parse HEAD 2>/dev/null || echo "unknown")"
HEAD_SUBJECT="$(git -C "$ROOT_DIR" log -1 --pretty=%s 2>/dev/null || echo "unknown")"
HEAD_FILE="$RELEASE_DIR/head.txt"
cat > "$HEAD_FILE" <<EOF
commit: $HEAD_HASH
subject: $HEAD_SUBJECT
EOF

echo "Packages ($RELEASE_DIR):"
ls -lh "${FINAL_PACKAGES[@]}" "$HEAD_FILE"
cat "$HEAD_FILE"
