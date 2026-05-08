#!/usr/bin/env bash
# =============================================================================
#  Ashes of Velsingrad — DocFX local generator (Linux / macOS / Git Bash)
#
#  Mirrors generate-docs.cmd but cross-platform. Run from the repo root:
#      ./generate-docs.sh             (build + open the site in your browser)
#      ./generate-docs.sh --serve     (build, then docfx serve on :8080)
#      ./generate-docs.sh --no-build  (skip dotnet build — useful for fast
#                                      doc-only iterations)
# =============================================================================

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVE=0
BUILD=1
PORT=8080

for arg in "$@"; do
    case "$arg" in
        --serve) SERVE=1 ;;
        --no-build) BUILD=0 ;;
        --port=*) PORT="${arg#--port=}" ;;
        -h|--help)
            grep -E '^#( |!)' "$0" | sed -E 's/^# ?//'
            exit 0 ;;
        *) echo "Unknown flag: $arg" >&2; exit 1 ;;
    esac
done

cd "$REPO_ROOT"

# ------- 1. DocFX install check -----------------------------------------------
if ! command -v docfx &>/dev/null; then
    echo "DocFX not found — installing as a global .NET tool…"
    dotnet tool install -g docfx
    # PATH may need a refresh; the tool lives in ~/.dotnet/tools
    export PATH="$PATH:$HOME/.dotnet/tools"
fi
echo "DocFX: $(docfx --version)"

# ------- 2. Build the C# projects so DocFX has up-to-date metadata ------------
if [[ "$BUILD" == "1" ]]; then
    echo "Building C# solution (Debug)…"
    dotnet build --nologo --verbosity quiet
fi

# ------- 3. Clean previous output ---------------------------------------------
rm -rf documentation/_site documentation/api
echo "Cleaned documentation/_site and documentation/api."

# ------- 4. Generate API metadata ---------------------------------------------
echo "Generating API metadata…"
docfx metadata docfx.json --warningsAsErrors false --logLevel Warning

# ------- 5. Build the static site ---------------------------------------------
if [[ "$SERVE" == "1" ]]; then
    echo "Building + serving on http://localhost:${PORT}/  (Ctrl-C to stop)"
    docfx docfx.json --serve --port "$PORT" --warningsAsErrors false
else
    echo "Building static site…"
    docfx build docfx.json --warningsAsErrors false
    echo
    echo "================================================================"
    echo "  Documentation built at: documentation/_site/index.html"
    echo "  Open it in a browser to preview, or rerun with --serve to host"
    echo "  it on http://localhost:${PORT} for live navigation."
    echo "================================================================"

    # Try to open it automatically — works on the three usual platforms.
    SITE="$REPO_ROOT/documentation/_site/index.html"
    if command -v xdg-open &>/dev/null;     then xdg-open "$SITE" &>/dev/null || true
    elif command -v open &>/dev/null;       then open "$SITE" &>/dev/null || true
    elif command -v start &>/dev/null;      then start "$SITE" &>/dev/null || true
    fi
fi
