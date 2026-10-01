#!/usr/bin/env bash
# =============================================================================
# setup_interop.sh - regenerate ClassicUs.GameLibs from a Classic Us install
# =============================================================================
#
# ClassicUs.GameLibs is published to nuget.org only up to 2026.8.17.1, while the
# three csproj files in this repo pin a newer version. Nothing on any configured
# feed can satisfy that restore, so the package is generated here, on the machine
# that has the game.
#
# PIPELINE
#
#   <game>/GameAssembly.dll|so + il2cpp_data/Metadata/global-metadata.dat
#      |
#      |  Cpp2IL --output-as dummydll
#      v
#   .NET stubs (DummyDll/*.dll)
#      |
#      |  Il2CppInterop.CLI generate
#      v
#   interop tree (Assembly-CSharp.dll, Il2Cppmscorlib.dll, ...)
#      |
#      |  tools/make_gamelibs.py
#      v
#   packages/classicus.gamelibs.<csproj pin>.nupkg
#
# The generated tree is collected the same way BepInEx does it at runtime, so
# type names, signatures and the compiler-generated `_X_d__NN` renaming match
# what the mod sees in the running game.
#
# REQUIRED
#   .NET 6+ SDK, python3, unzip, git, ~2 GB free disk
#   a Classic Us install - OR an interop tree you already generated (--interop)
#
# USAGE
#   # full run from the repo root
#   CLASSICUS_GAME_PATH="/path/to/ClassicUs" bash tools/setup_interop.sh
#
#   # already have the interop? skip the two-tool build and the generation
#   bash tools/setup_interop.sh --interop /tmp/interop-real
#
#   # other inputs
#   bash tools/setup_interop.sh --interop DIR --base packages/x.nupkg --version 2026.9.20.1
#   WORKDIR=/big/tmp bash tools/setup_interop.sh --interop DIR
#
# Re-run after every Classic Us update, then walk the drift checklist printed at
# the end. An interop that is one game build behind compiles clean and crashes
# at runtime, which is the expensive kind of wrong.
# =============================================================================
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORKDIR="${WORKDIR:-/tmp/interop-pipeline}"
DOTNET="${DOTNET:-}"
GAME_PATH="${CLASSICUS_GAME_PATH:-}"
INTEROP_DIR=""
BASE=""
VERSION=""

CPP2IL_DIR="$WORKDIR/Cpp2IL"
IL2CPPINTEROP_DIR="$WORKDIR/Il2CppInterop"
CPP2IL_OUT="$WORKDIR/cpp2il-output"
CPP2IL_BUILT="$WORKDIR/cpp2il-built"
INTEROP_BUILT="$WORKDIR/interop-built"
GENERATED_INTEROP="$WORKDIR/interop-real"

die() { echo "error: $*" >&2; exit 1; }
have() { command -v "$1" >/dev/null 2>&1; }

usage() {
    sed -n '3,45p' "$0" | sed 's/^# \{0,1\}//'
}

while [ $# -gt 0 ]; do
    case "$1" in
        --interop) [ $# -ge 2 ] || die "--interop needs a directory"; INTEROP_DIR="$2"; shift 2 ;;
        --base)    [ $# -ge 2 ] || die "--base needs a file";       BASE="$2";        shift 2 ;;
        --version) [ $# -ge 2 ] || die "--version needs a value";   VERSION="$2";     shift 2 ;;
        --workdir) [ $# -ge 2 ] || die "--workdir needs a path";    WORKDIR="$2";     shift 2
                   CPP2IL_DIR="$WORKDIR/Cpp2IL"; IL2CPPINTEROP_DIR="$WORKDIR/Il2CppInterop"
                   CPP2IL_OUT="$WORKDIR/cpp2il-output"; CPP2IL_BUILT="$WORKDIR/cpp2il-built"
                   INTEROP_BUILT="$WORKDIR/interop-built"; GENERATED_INTEROP="$WORKDIR/interop-real" ;;
        -h|--help) usage; exit 0 ;;
        *) die "unknown argument: $1 (try --help)" ;;
    esac
done

# ---------------------------------------------------------------- prerequisites
echo "==> [1/5] Checking prerequisites"

if [ -z "$DOTNET" ]; then
    if have dotnet; then
        DOTNET="dotnet"
    elif [ -x "$HOME/.dotnet/dotnet" ]; then
        DOTNET="$HOME/.dotnet/dotnet"; export DOTNET_ROOT="$HOME/.dotnet"
    elif [ -x /tmp/dotnet/dotnet ]; then
        DOTNET=/tmp/dotnet/dotnet; export DOTNET_ROOT=/tmp/dotnet
    else
        die "no dotnet SDK found. Install .NET 6+ or set DOTNET=/path/to/dotnet"
    fi
fi

# A shim that prints 'not installed' on PATH still satisfies `command -v`, so
# prove the binary actually runs before building two tools with it.
"$DOTNET" --list-sdks >/dev/null 2>&1 \
    || die "\`$DOTNET --list-sdks\` failed - that is not a working SDK. Install .NET 6+ or set DOTNET=/path/to/dotnet"

have python3 || die "python3 not found"
have unzip   || die "unzip not found (tools/_nupkg_utils.py extracts the base package with it)"
have git     || die "git not found"

# The generated tree is only worth collecting from the game itself, so require
# the game only when we are the ones generating it.
GAME_BIN=""
if [ -z "$INTEROP_DIR" ]; then
    [ -n "$GAME_PATH" ] && [ -d "$GAME_PATH" ] \
        || die "CLASSICUS_GAME_PATH is unset or not a directory.
       export CLASSICUS_GAME_PATH=/path/to/ClassicUs
       It must contain GameAssembly.so or GameAssembly.dll and
       il2cpp_data/Metadata/global-metadata.dat.
       (Or pass --interop <dir> if you already generated the interop.)"

    if [ -f "$GAME_PATH/GameAssembly.so" ]; then
        GAME_BIN="$GAME_PATH/GameAssembly.so"; PLATFORM="linux"
    elif [ -f "$GAME_PATH/GameAssembly.dll" ]; then
        GAME_BIN="$GAME_PATH/GameAssembly.dll"; PLATFORM="windows"
    else
        die "no GameAssembly.so or GameAssembly.dll in $GAME_PATH"
    fi

    [ -f "$GAME_PATH/il2cpp_data/Metadata/global-metadata.dat" ] \
        || die "global-metadata.dat not found at $GAME_PATH/il2cpp_data/Metadata/"

    FREE_MB="$(df -Pk "$ROOT" | awk 'NR==2 {print int($4/1024)}')"
    [ "$FREE_MB" -ge 2048 ] \
        || die "${FREE_MB} MB free, need ~2 GB for the tool builds and the interop tree"
fi

"$DOTNET" --list-sdks | sed 's/^/    sdk: /'
echo "    platform: ${PLATFORM:-<using --interop, game not needed>}"

# ------------------------------------------------------------------- base nupkg
echo "==> [2/5] Resolving the base GameLibs package"

if [ -z "$BASE" ]; then
    # Newest existing local package: it is already normalized and carries the
    # Unity/BepInEx refs closest to the current game.
    BASE="$(ls -1 "$ROOT"/packages/classicus.gamelibs.*.nupkg 2>/dev/null | sort -V | tail -1 || true)"
fi

if [ -z "$BASE" ]; then
    have curl || die "no local packages/classicus.gamelibs.*.nupkg and curl is missing"
    echo "    none in packages/ - fetching the newest published package"
    INDEX="$(curl -fsSL https://api.nuget.org/v3-flatcontainer/classicus.gamelibs/index.json)" \
        || die "cannot reach nuget.org"
    PUBLISHED="$(printf '%s' "$INDEX" \
        | python3 -c 'import json,sys; print(json.load(sys.stdin)["versions"][-1])')" \
        || die "could not parse the nuget.org version index"

    TMP_PKG="$WORKDIR/classicus.gamelibs.$PUBLISHED.nupkg"
    mkdir -p "$WORKDIR" "$ROOT/packages"
    curl -fsSL -o "$TMP_PKG" \
        "https://api.nuget.org/v3-flatcontainer/classicus.gamelibs/$PUBLISHED/classicus.gamelibs.$PUBLISHED.nupkg" \
        || die "download of $PUBLISHED failed"

    BASE="$ROOT/packages/classicus.gamelibs.$PUBLISHED.nupkg"
    echo "    published $PUBLISHED -> normalizing paths into $BASE"
    python3 "$ROOT/tools/repack_gamelibs.py" "$TMP_PKG" "$BASE"
fi

[ -f "$BASE" ] || die "base package not found: $BASE"
echo "    base: $BASE"

# ------------------------------------------------------------ generate interop
if [ -n "$INTEROP_DIR" ]; then
    [ -d "$INTEROP_DIR" ] || die "--interop directory not found: $INTEROP_DIR"
    echo "==> [3/5] Using the interop tree at $INTEROP_DIR (skipping generation)"
else
    echo "==> [3/5] Building Cpp2IL and Il2CppInterop"

    # Both tools compile against newer SDKs than they target; roll forward rather
    # than editing their global.json.
    export DOTNET_ROLL_FORWARD=LatestMajor

    if [ ! -d "$CPP2IL_DIR" ]; then
        git clone --depth 1 https://github.com/SamboyCoding/Cpp2IL.git "$CPP2IL_DIR" 2>&1 | tail -3
    else
        echo "    reusing $CPP2IL_DIR (delete it to re-clone)"
    fi

    "$DOTNET" publish "$CPP2IL_DIR/Cpp2IL/Cpp2IL.csproj" -c Release -o "$CPP2IL_BUILT" 2>&1 | tail -5
    CPP2IL_BIN="$CPP2IL_BUILT/Cpp2IL.dll"
    [ -f "$CPP2IL_BIN" ] || die "Cpp2IL publish produced no $CPP2IL_BIN"

    if [ ! -d "$IL2CPPINTEROP_DIR" ]; then
        git clone --depth 1 https://github.com/BepInEx/Il2CppInterop.git "$IL2CPPINTEROP_DIR" 2>&1 | tail -3
    else
        echo "    reusing $IL2CPPINTEROP_DIR (delete it to re-clone)"
    fi

    if [ -f "$IL2CPPINTEROP_DIR/CLI/Il2CppInterop.CLI/Il2CppInterop.CLI.csproj" ]; then
        CLI_PROJ="$IL2CPPINTEROP_DIR/CLI/Il2CppInterop.CLI/Il2CppInterop.CLI.csproj"
    elif [ -f "$IL2CPPINTEROP_DIR/Il2CppInterop.CLI/Il2CppInterop.CLI.csproj" ]; then
        CLI_PROJ="$IL2CPPINTEROP_DIR/Il2CppInterop.CLI/Il2CppInterop.CLI.csproj"
    else
        find "$IL2CPPINTEROP_DIR" -name '*.csproj' | head -10 | sed 's/^/    /'
        die "cannot find Il2CppInterop.CLI.csproj inside $IL2CPPINTEROP_DIR"
    fi

    "$DOTNET" publish "$CLI_PROJ" -c Release -o "$INTEROP_BUILT" 2>&1 | tail -5
    INTEROP_BIN="$INTEROP_BUILT/Il2CppInterop.CLI.dll"
    [ -f "$INTEROP_BIN" ] || die "Il2CppInterop publish produced no $INTEROP_BIN"

    echo "    dumping game assemblies with Cpp2IL"
    rm -rf "$CPP2IL_OUT"
    "$DOTNET" "$CPP2IL_BIN" --game-path "$GAME_PATH" --output-as dummydll --output-to "$CPP2IL_OUT" 2>&1 | tail -10

    CPP2IL_DLL_DIR="$(find "$CPP2IL_OUT" -type d -name DummyDll 2>/dev/null | head -1 || true)"
    if [ -z "$CPP2IL_DLL_DIR" ]; then
        CPP2IL_DLL_DIR="$(find "$CPP2IL_OUT" -name Assembly-CSharp.dll -printf '%h\n' 2>/dev/null | head -1 || true)"
    fi
    [ -n "$CPP2IL_DLL_DIR" ] || die "could not locate Cpp2IL output under $CPP2IL_OUT"

    echo "    generating interop from $CPP2IL_DLL_DIR"
    rm -rf "$GENERATED_INTEROP"
    "$DOTNET" "$INTEROP_BIN" generate \
        --input "$CPP2IL_DLL_DIR" \
        --output "$GENERATED_INTEROP" \
        --game-assembly "$GAME_BIN" 2>&1 | tail -10

    [ -f "$GENERATED_INTEROP/Assembly-CSharp.dll" ] \
        || die "Il2CppInterop produced no $GENERATED_INTEROP/Assembly-CSharp.dll"
    INTEROP_DIR="$GENERATED_INTEROP"
fi

# -------------------------------------------------------------- build the pkg
echo "==> [4/5] Assembling the GameLibs package"

ARGS=("$ROOT/tools/make_gamelibs.py" "$BASE" "$INTEROP_DIR")
[ -n "$VERSION" ] || VERSION="$(python3 -c '
import re,sys
text=open(sys.argv[1],encoding="utf-8").read()
m=re.search(r"Include=\"ClassicUs\.GameLibs\"\s+Version=\"([^\"]+)\"",text)
print(m.group(1) if m else "")' "$ROOT/TownOfRoles.csproj")"
[ -n "$VERSION" ] || die "could not read the ClassicUs.GameLibs pin from TownOfRoles.csproj"
ARGS+=(--version "$VERSION")

OUT="$ROOT/packages/classicus.gamelibs.$VERSION.nupkg"
python3 "${ARGS[@]}"

# ---------------------------------------------------------------------- verify
echo "==> [5/5] Verifying $OUT"

python3 - "$OUT" "$VERSION" <<'PY'
import sys, zipfile
pkg, version = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(pkg) as z:
    names = z.namelist()
    if any("\\" in n for n in names):
        sys.exit("error: package still contains backslash paths")
    refs = [n for n in names if n.startswith("ref/net6.0/") and n.endswith(".dll")]
    if "ref/net6.0/Assembly-CSharp.dll" not in refs:
        sys.exit("error: ref/net6.0/Assembly-CSharp.dll is missing")
    key = z.getinfo("ref/net6.0/Assembly-CSharp.dll").file_size
    print(f"    {len(refs)} ref assemblies, Assembly-CSharp.dll {key:,} bytes")
    if key < 1_000_000:
        sys.exit("error: Assembly-CSharp.dll looks like a stub, not generated interop")
    nuspec = next(n for n in names if n.endswith(".nuspec") and "/" not in n)
    if f"<version>{version}</version>" not in z.read(nuspec).decode("utf-8-sig"):
        sys.exit(f"error: nuspec does not declare version {version}")
    print(f"    nuspec declares {version}")
PY

echo
echo "GameLibs $VERSION is in the local feed. Build against it with:"
echo
echo "    rm -rf ~/.nuget/packages/classicus.gamelibs && dotnet restore --force"
echo "    sh build.sh"
echo
echo "Check for interop drift: diff the new interop against what"
echo "the code references - _BeginTeam_d__NN, MeetingHud.Start, ExileController._Animate,"
echo "and every nameof() used as a Harmony target - before trusting the build. Drift is"
echo "compile-silent and only shows up as a crash on load."
