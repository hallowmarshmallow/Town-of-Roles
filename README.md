# Town Of Roles

A roles mod for **Classic Us**. It brings the roles from the old Town Of Us mod (Among Us) to Classic Us.

Made for BOTH Windows and Linux (Android soon, im working on a client for it).

## What is in the mod

27 roles and 4 modifiers.

| Team      | Roles                                                                                                     |
|-----------|-----------------------------------------------------------------------------------------------------------|
| Crewmate  | Sheriff, Engineer, Medic, Seer, Vigilante, Altruist, Mayor, Swapper, Spy, Investigator, Time Lord, Snitch |
| Impostor  | Assassin, Janitor, Morphling, Camouflager, Swooper, Underdog, Undertaker, Miner                           |
| Neutral   | Jester, Executioner, Arsonist, Lovers, Phantom, Shifter, The Glitch                                       |
| Modifiers | Torch, Diseased, Flash, Tiebreaker                                                                        |


## Install

Download the zip for your system from the releases page.

**Windows:** `TOR-Windows.zip`
This one includes BepInEx. Unzip it into the game folder, then start the game like normal.

**Linux:** `TOR-Linux.zip`
This one includes BepInEx. Unzip it into the game folder and start the game with the script:

```bash
chmod +x run_bepinex.sh
./run_bepinex.sh ./classicus.x86_64
```

If you already have BepInEx, and wants to update the mod manually, use '`TOR.dll`' instead.

Important: if you have dll files called `ClassicUs.ManuAPI.dll` or `ClassicUs.Manactor.dll` in `BepInEx/plugins`, delete them first. 
Those plugins are not compatible with the mod.

## Settings

The first time you start the game, a config file is made at `BepInEx/config/TownOfRoles.cfg`.

- Every role has an on/off switch. A role that is off will never be given to anyone.
- Every role has a `Count` and a `Chance`, plus its own settings (cooldowns, uses, and so on).
- Modifiers use `<Modifier>Probability` (0 to 100).

You can also change settings via the BepInEx/config/townofroles.cfg under the names [Crewmate Roles] [Impostor Roles] [Neutral roles] and [Modifiers].


Classic Us settings also has a **TOR** tab with three functions:
- **Disable Mod (until restart):** turns the mod off until you restart the game. (NOT WORKING RIGHT NOW)
- **No Game End:** the game will not end. Good for testing (host only).
- **Force Start:** starts the round now (host only).

(These are only useful for host, nogameend and forcestart also can be accessed thru commands, read below.)

## Chat commands

Type these in the game chat. They only run on your side and are not sent as chat. Commands that change the lobby or other players need you to be the host.

```text
/forcestart
/nickname <new name>
/setrole <role> (buggy)
/setrole <player name or id> <role> (buggy)
/revive [player name or id] (could be buggy)
/nogameend [on|off]
/tpin [player name or id]
/tpout [player name or id]
/system <message>
/rainbow [on|off]
/gradient [on|off]
/guess <player name or id> <role>     (Assassin only, in meetings)
```

`/color rainbow` and `/color gradient` do the same as `/rainbow` and `/gradient`.

Notes:
- `/setrole`, `/revive`, `/system` and most others are host only.
- `/tpin` and `/tpout` are for testing. Use them only when a map is loaded.
- `/rainbow` and `/gradient` only change how you see your own color.

To turn all commands off:

```ini
[Commands]
Enabled = false
```

## Build it yourself

```bash
sh build.sh
```

This builds three DLLs into `dist/plugins/`: `Atomic.dll`, `MarshAPI.dll` and `TownOfRoles.dll`. It also puts an updater DLL into `dist/patchers/`.

Copy `dist/plugins/*.dll` to `<game>/BepInEx/plugins/` and `dist/patchers/*.dll` to `<game>/BepInEx/patchers/`.

## Folders

```
Core/           shared mod code (config, UI, updater, reflection)
Roles/          one folder per role implementation
Commands/       chat commands system
Assets/         original Town Of Us icons
UpdaterPatcher/ preloader patcher for self-updates
tools/          utility and packaging scripts
```

Dependencies (cloned as sibling repositories):
- `Atomic/`: Atomic (networking & RPC framework)
- `MarshAPI/`: MarshAPI (custom role and ability SDK)

To add a new role, copy `Roles/Sheriff/` and rename it.

## Credits

- **Classic Us** game: DlovanSl
- **Town Of Us** (the original mod): slushiegoose and others
  https://github.com/slushiegoose/Town-Of-Us

The role icons come from the original Town Of Us mod.
