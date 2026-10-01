#!/usr/bin/env python3
"""Assemble packages/classicus.gamelibs.<version>.nupkg from a base GameLibs
package plus a freshly generated Il2CppInterop tree.

ClassicUs.GameLibs is not published for every game release - nuget.org stops at
2026.8.17.1 while the builds in this repo pin a newer version - so the package
they need is assembled here instead of downloaded. Two inputs:

  base     a normalized (forward-slash) GameLibs nupkg. Supplies the Unity,
           BepInEx and Il2Cpp reference assemblies, which the interop pipeline
           does not regenerate.
  interop  the directory `Il2CppInterop.CLI generate` wrote. Supplies the game's
           own assemblies, above all Assembly-CSharp.dll.

Every `ref/net6.0/<Name>.dll` in the base whose `<Name>.dll` also exists in the
interop tree is replaced. Assembly-CSharp.dll *must* be replaced - that is the
whole point - and the run fails if it is not there. The nuspec version is
rewritten to the pinned version and the stale NuGet signature is dropped, the
same treatment tools/_nupkg_utils.py applies to the other local packages.

The version is read from TownOfRoles.csproj rather than passed in, so the feed
entry and the csproj pins cannot drift apart.

Usage:
    python3 tools/make_gamelibs.py <base.nupkg> <interop-dir>
    python3 tools/make_gamelibs.py <base.nupkg> <interop-dir> --version 2026.9.20.1
    python3 tools/make_gamelibs.py <base.nupkg> <interop-dir> --out /tmp/probe.nupkg
"""
import argparse
import os
import re
import sys
import zipfile

from _nupkg_utils import rewrite_nupkg

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSPROJ = os.path.join(ROOT, "TownOfRoles.csproj")
PACKAGES = os.path.join(ROOT, "packages")

REF_PREFIX = "ref/net6.0/"
KEY_ASSEMBLY = "Assembly-CSharp.dll"

# The 9.10.1 package carries a 5.98 MB Assembly-CSharp.dll. Anything far under
# that is a stub (signature-only) tree, which compiles but has no method bodies
# and is the wrong artifact - see ~/preil2cpp's verify.sh for the same trap.
MIN_INTEROP_BYTES = 1_000_000

PIN_RE = re.compile(r'Include="ClassicUs\.GameLibs"\s+Version="([^"]+)"')
NUSPEC_VERSION_RE = re.compile(r"<version>([^<]+)</version>")


def pinned_version():
    """The ClassicUs.GameLibs version all three csproj files pin."""
    try:
        with open(CSPROJ, encoding="utf-8") as f:
            text = f.read()
    except OSError as e:
        sys.exit(f"error: cannot read {CSPROJ}: {e}")

    match = PIN_RE.search(text)
    if not match:
        sys.exit("error: no ClassicUs.GameLibs PackageReference in TownOfRoles.csproj")
    return match.group(1)


def read_entries(path):
    """Entry names of a zip, or a fatal error if the path is not one."""
    if not os.path.isfile(path):
        sys.exit(f"error: base package not found: {path}")

    try:
        with zipfile.ZipFile(path) as z:
            return z.namelist()
    except zipfile.BadZipFile as e:
        sys.exit(f"error: {path} is not a readable zip: {e}")


def nuspec_version(path):
    """The <version> of the root .nuspec, or None."""
    with zipfile.ZipFile(path) as z:
        for name in z.namelist():
            if name.endswith(".nuspec") and "/" not in name:
                match = NUSPEC_VERSION_RE.search(z.read(name).decode("utf-8-sig"))
                return match.group(1) if match else None
    return None


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("base", help="normalized base GameLibs nupkg")
    ap.add_argument("interop", help="directory written by Il2CppInterop.CLI generate")
    ap.add_argument("--version", default=None, help="target version (default: the csproj pin)")
    ap.add_argument("--out", default=None, help="output nupkg (default: packages/<id>.<version>.nupkg)")
    args = ap.parse_args()

    version = args.version or pinned_version()
    out = args.out or os.path.join(PACKAGES, f"classicus.gamelibs.{version}.nupkg")

    if not os.path.isdir(args.interop):
        sys.exit(f"error: interop directory not found: {args.interop}")

    entries = read_entries(args.base)

    # A raw nuget.org download uses backslash separators, so NuGet matches none
    # of its assets. Normalize it first rather than producing a broken package.
    backslashed = [n for n in entries if "\\" in n]
    if backslashed:
        sys.exit(
            f"error: {args.base} uses backslash path separators "
            f"({len(backslashed)} entries), so NuGet sees no compile assets.\n"
            f"       Normalize it first:\n"
            f"         python3 tools/repack_gamelibs.py {args.base} {args.base}"
        )

    refs = sorted(n for n in entries if n.startswith(REF_PREFIX) and n.endswith(".dll"))
    if not refs:
        sys.exit(f"error: no {REF_PREFIX}*.dll entries in {args.base}")

    replacements = {}
    replaced, inherited = [], []
    for name in refs:
        simple = name[len(REF_PREFIX):]
        source = os.path.join(args.interop, simple)
        if os.path.isfile(source):
            with open(source, "rb") as f:
                replacements[name] = f.read()
            replaced.append(simple)
        else:
            inherited.append(simple)

    if KEY_ASSEMBLY not in replaced:
        sys.exit(
            f"error: {args.interop} has no {KEY_ASSEMBLY}.\n"
            f"       That is the one assembly the interop pipeline exists to produce;\n"
            f"       without it the package would be a relabelled copy of the base."
        )

    key_size = len(replacements[f"{REF_PREFIX}{KEY_ASSEMBLY}"])
    if key_size < MIN_INTEROP_BYTES:
        print(
            f"warning: {KEY_ASSEMBLY} is only {key_size:,} bytes. That is the size of a\n"
            f"         signature-only stub, not generated interop - check the Cpp2IL and\n"
            f"         Il2CppInterop output before trusting this package.",
            file=sys.stderr,
        )

    os.makedirs(os.path.dirname(out), exist_ok=True)
    tmp = out + ".tmp"
    try:
        rewrite_nupkg(args.base, tmp, replacements, nuspec_version=version)
        os.replace(tmp, out)
    finally:
        if os.path.exists(tmp):
            os.remove(tmp)

    written = nuspec_version(out)
    if written != version:
        sys.exit(f"error: wrote {out} with nuspec version {written!r}, expected {version!r}")

    print(f"Wrote {out}")
    print(f"  version   {version} (from {'--version' if args.version else 'TownOfRoles.csproj'})")
    print(f"  replaced  {len(replaced)} of {len(refs)} refs from {args.interop}")
    for name in replaced:
        print(f"              {name}")
    if inherited:
        print(f"  inherited {len(inherited)} from the base package, not regenerated:")
        for name in inherited:
            print(f"              {name}")
        print()
        print("  Inherited refs are the Unity/BepInEx/Il2Cpp reference assemblies. They are")
        print("  correct only while the base package's Unity version still matches the game's.")
        print("  If the game moved to a new Unity version, refresh the base package first.")


if __name__ == "__main__":
    main()
