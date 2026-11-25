#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

UNAME_S="$(uname -s)"
case "$UNAME_S" in
    Linux*)
        OS_TYPE="linux"
        EXE_NAME="Xilium.CefGlue.Demo.Avalonia"
        ;;
    MINGW*|MSYS*|CYGWIN*)
        OS_TYPE="win"
        EXE_NAME="Xilium.CefGlue.Demo.Avalonia.exe"
        ;;
    *)
        echo "Unsupported OS: $UNAME_S (this fork supports Avalonia on Windows and Linux x64 only)" >&2
        exit 1
        ;;
esac

UNAME_M="$(uname -m)"
case "$UNAME_M" in
    x86_64|amd64|AMD64)
        RID="${OS_TYPE}-x64"
        ;;
    *)
        echo "Unsupported architecture: $UNAME_M (this fork supports x64 only)" >&2
        exit 1
        ;;
esac

CONFIGURATION="${CONFIGURATION:-Debug}"
TFM="$(sed -n 's:.*<DotnetVersion>\(.*\)</DotnetVersion>.*:\1:p' "$ROOT_DIR/Directory.Build.props" | head -n 1 | tr -d '\r[:space:]')"
TFM="${TFM:-net10.0}"

DEMO_PROJECT="$ROOT_DIR/CefGlue.Demo.Avalonia/CefGlue.Demo.Avalonia.csproj"
OUT_DIR="$ROOT_DIR/CefGlue.Demo.Avalonia/bin/$CONFIGURATION/$TFM/$RID"
DEMO_BIN="$OUT_DIR/$EXE_NAME"

echo "Target environment: OS=$OS_TYPE, RID=$RID, Configuration=$CONFIGURATION, TFM=$TFM"

DO_CLEAN_BUILD=0
if [[ -f "$DEMO_BIN" ]]; then
    echo "Existing build found: $DEMO_BIN"
    read -r -p "Existing build found. Clean and rebuild the Avalonia demo? [y/N]: " answer || answer=""
    if [[ "$answer" =~ ^[Yy]$ ]]; then
        DO_CLEAN_BUILD=1
    fi
else
    echo "No existing build found for $RID ($CONFIGURATION, $TFM)."
    DO_CLEAN_BUILD=1
fi

if [[ "$DO_CLEAN_BUILD" -eq 1 ]]; then
    echo "Cleaning and building $DEMO_PROJECT ($CONFIGURATION, $RID)..."
    dotnet clean "$DEMO_PROJECT" -c "$CONFIGURATION" -r "$RID"
    rm -rf "$OUT_DIR"
    dotnet build "$DEMO_PROJECT" -c "$CONFIGURATION" -r "$RID"
fi

if [[ ! -f "$DEMO_BIN" ]]; then
    echo "Error: Demo executable not found at $DEMO_BIN" >&2
    exit 1
fi

if [[ "$OS_TYPE" != "win" ]]; then
    chmod +x "$DEMO_BIN"
fi

if [[ -z "${DOTNET_ROOT:-}" ]] && command -v dotnet >/dev/null 2>&1; then
    DOTNET_BIN="$(command -v dotnet)"
    if command -v realpath >/dev/null 2>&1; then
        DOTNET_BIN="$(realpath "$DOTNET_BIN")"
    elif readlink -f "$DOTNET_BIN" >/dev/null 2>&1; then
        DOTNET_BIN="$(readlink -f "$DOTNET_BIN")"
    fi
    export DOTNET_ROOT="$(dirname "$DOTNET_BIN")"
fi

if [[ "$OS_TYPE" == "linux" ]]; then
    for candidate in \
        "$OUT_DIR/libHarfBuzzSharp.so" \
        "$OUT_DIR/runtimes/${RID}/native/libHarfBuzzSharp.so"; do
        if [[ -f "$candidate" ]]; then
            export LD_PRELOAD="${candidate}${LD_PRELOAD:+:$LD_PRELOAD}"
            break
        fi
    done
fi

echo "Launching: $DEMO_BIN"
cd "$OUT_DIR"
exec "$DEMO_BIN" "$@"
