#!/usr/bin/env python3
"""Turn a real round's BepInEx log into an ability-button verdict.

Parses the once-per-round diagnostic line the mod emits:

    [Info :TownOfRoles] Ability buttons: nativeRole=Impostor assigned=townofroles.Miner visible=[Miner]; not created:waiting for a playable HUD x2; hidden:not this role (or role disabled in config) x14

plus the per-button tick-failure and template lines, and prints a human verdict.

Exit code 0 = buttons OK (at least one shown, or every hidden button has a
              legitimate reason),
            1 = buttons BROKEN (a tick failed, the template was never captured,
              or nothing is shown and nothing explains why),
            2 = no diagnostic found in the log (the round never started, or an
              old build without the diagnostic is installed).

Usage:
    python3 tools/ability_button_report.py [path/to/LogOutput.log]
Without an argument it probes the usual locations for the most recent log.
"""

import glob
import os
import re
import sys

DIAG_RE = re.compile(r"Ability buttons: (?P<rest>.*)$")
TICKFAIL_RE = re.compile(r"Ability button '([^']+)' tick failed: (.*)")
TEMPLATE_RE = re.compile(r"Ability button template at ([^:]+): (.*)")
NO_KILL_RE = re.compile(r"Ability buttons: HudManager\.KillButton is null")

# Reasons that are the mod working as designed (button hidden because it should be).
LEGITIMATE = {
    "not this role (or role disabled in config)",
    "local player is dead",
    "no local player",
    "meeting in progress",
}


def default_logs():
    """Most recent LogOutput.log in the usual places."""
    patterns = [
        os.path.expanduser("~/.steam/steam/steamapps/common/**/BepInEx/LogOutput.log"),
        os.path.expanduser("~/Desktop/**/BepInEx/LogOutput.log"),
        os.path.expanduser("~/**/BepInEx/LogOutput.log"),
    ]
    hits = []
    for pat in patterns:
        hits.extend(glob.glob(pat, recursive=True))
    return sorted(hits, key=os.path.getmtime, reverse=True)


def parse(path):
    diag = None
    tick_fails = []
    template = None
    no_kill = False
    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            m = DIAG_RE.search(line)
            if m and "KillButton is null" not in m.group(1):
                diag = m.group(1).strip()
                continue
            m = TICKFAIL_RE.search(line)
            if m:
                tick_fails.append((m.group(1), m.group(2)))
                continue
            m = TEMPLATE_RE.search(line)
            if m:
                template = (m.group(1), m.group(2))
                continue
            if NO_KILL_RE.search(line):
                no_kill = True
    return diag, tick_fails, template, no_kill


def verdict(diag, tick_fails, template, no_kill):
    lines = []
    ok = True

    if template is not None:
        when, what = template
        lines.append(f"template  : {what}  (captured at {when})")
        if "captured" not in what:
            ok = False
    else:
        lines.append("template  : no capture line found (boot-screen/menu hooks may be missing)")

    if no_kill:
        lines.append("kill btn  : absent — placement falls back to the BottomRight grid (informational)")

    for name, err in tick_fails:
        ok = False
        lines.append(f"TICK FAIL : {name} — {err}")

    if diag is None:
        print("\n".join(lines))
        print("diagnostic: NOT FOUND in this log")
        return 2

    shown = re.findall(r"visible=\[([^\]]*)\]", diag)
    shown_list = [s for s in (shown[0].split(",") if shown and shown[0] else []) if s]

    reasons = dict(re.findall(r"([^;:\[\]]+:[^;]*?) x(\d+)", diag))
    hidden_total = sum(int(n) for n in re.findall(r"x(\d+)", diag.split("visible=")[-1]))

    lines.append(f"shown     : {', '.join(shown_list) if shown_list else '(none)'}")

    unexplained = 0
    for reason, count in reasons.items():
        reason = reason.strip()
        if reason.startswith("hidden:"):
            bare = reason[len("hidden:"):].strip()
            tag = "ok" if bare in LEGITIMATE else "UNEXPLAINED"
            if tag == "UNEXPLAINED":
                unexplained += int(count)
            lines.append(f"hidden    : {bare} x{count}  [{tag}]")
        elif reason.startswith("not created:"):
            bare = reason[len("not created:"):].strip()
            tag = "ok" if bare == "waiting for a playable HUD" else "UNEXPLAINED"
            if tag == "UNEXPLAINED":
                unexplained += int(count)
            lines.append(f"not creat.: {bare} x{count}  [{tag}]")

    if not shown_list and hidden_total == 0 and not tick_fails:
        ok = False
        lines.append("verdict   : BROKEN — no button shown, nothing hidden, nothing created")
    elif unexplained:
        ok = False
        lines.append(f"verdict   : BROKEN — {unexplained} button(s) hidden/absent for no known reason")
    elif shown_list:
        lines.append("verdict   : OK — button(s) on screen")
    else:
        lines.append("verdict   : OK — all buttons legitimately hidden for this role/state")
    print("\n".join(lines))
    return 0 if ok else 1


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else (default_logs() or [None])[0]
    if not path or not os.path.isfile(path):
        print("no LogOutput.log found; pass the path explicitly:")
        print("  python3 tools/ability_button_report.py '<game>/BepInEx/LogOutput.log'")
        return 2

    print(f"log: {path}\n")
    diag, tick_fails, template, no_kill = parse(path)
    code = verdict(diag, tick_fails, template, no_kill)
    if code == 2 and diag is None:
        print("\nNo 'Ability buttons:' line — either no round was played with a\n"
              "button-capable role enabled, or the installed TownOfRoles.dll predates\n"
              "the diagnostic. Install the current build and play one round.")
    return code


if __name__ == "__main__":
    sys.exit(main())
