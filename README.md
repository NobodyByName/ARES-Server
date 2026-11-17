# ARES-Server

Independent replacement servers for ARC Raiders: the backend a stock game client signs in to, and
the game-session server that runs its raids. Players connect with
[ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher) (a separate repository) and their own, unmodified game.

> **Unofficial project.** ARES is independent and is not affiliated with, endorsed by or sponsored
> by Embark Studios. ARC Raiders and all related names and trademarks belong to their respective
> owners. ARES works only with an installed, unmodified copy of the game.

## Help wanted: multiplayer testers

Everything so far has been tested by one person on one PC, so parties, two players in the same
raid and PvP have never been tried with a real second player. If you have ARC Raiders (build
26439197) and can spare an evening, I'd love to play a few test raids with you. Say hi in the
[Matrix room](https://matrix.to/#/#ares-arc-raiders-emulation-server:matrix.org). No setup
knowledge needed: I host, you install
[ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher) and join.

## Components

| Component | Role |
| --- | --- |
| [Host](Host/) | ARES-Host: backend for sign-in, the main menu, parties and matchmaking; starts a Raid server for each raid. |
| [Raid](Raid/) | ARES-Raid: independent game-session server that runs the raid. |
| [Structs](Structs/) | Standalone library of decoded ARC Raiders structures, wire formats and protocol for community projects. |
| [Common](Common/) | Shared server logic used by Host and Raid, and the map file format. |
| [GameDataExtract](GameDataExtract/) | ARES-GameDataExtract: operator tool that extracts the [game data](#game-data) from an installed game. |

## Content progress

[![ARES content map: individual maps, ARC enemies, items and equipment, world objects, objectives, skills, traders, furniture and appearance](docs/content-map/overview.svg)](docs/content-map/README.md)

[Explore content](docs/content-map/README.md) · [Current server support](docs/content-map/support.md)

Each cell is one content piece: **green** = done, **blue** = partial, **orange** = not implemented,
**gray** = not assessed. Group sizes are balanced for readability; internal names and
availability still need review.

## Requirements

- .NET 10 SDK to build from source (version in `global.json`). No game installation is needed to build or test.
- An operator-managed MongoDB replica set for Host persistence and integration tests (tested with MongoDB 8.0).
- Players need their own, unmodified copy of **ARC Raiders 2.4.1, Steam build 26439197**, and
  [ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher). ARES supports one game build at a time;
  players on a newer build must wait for ARES to support it.
- Operators must supply the game's [archive keys](#archive-keys). ARES includes no game keys or game files.
- Operators extract the [game data](#game-data) (items, loot, movement and map data, map collision) from their own
  game installation. Host and Raid refuse to start without it.
- Developed and tested on Linux. Support for every ARC Raiders platform is a goal; Windows has not been tested yet.

## Archive keys

Operators must provide the supported build's pak and container keys in `archive-keys.txt` beside
`ares-host.conf`, or select another file with `archive-keys-file`. Host requires matching keys at
startup and supplies them to ARES-Launcher to read each player's own game files. Keep the key file
private and out of Git.

## Game data

Host and Raid need the game's data: the item catalogue, loot tables, movement tuning, each map's world tables
(loot sockets, doors, ladders, ziplines, player starts, extraction points…) and its collision (ground, walls,
buildings and other solid objects). ARES does not include it: like the map extractors of other server emulators,
ARES-GameDataExtract reads it from the operator's own game installation and writes ARES's game data files. The files are derived
from the game: keep them on your own servers and never distribute them.

**When to run it:** at first setup and again after every game update. Each file is stamped with the
game build it was extracted from. Host and Raid refuse game data from another build, so extract again
whenever the supported game build changes.

**What it needs:**

- **Game folder:** an installed copy of the supported game build (the folder holding `PioneerGame/`).
  It is only read, never changed.
- **Archive keys file:** the same `archive-keys.txt` Host uses (see [Archive keys](#archive-keys)).
- **TOC key file:** the key of the game's archive index (the `.utoc` files). Only ARES-GameDataExtract needs
  it; Host does not. Operators must provide the supported build's TOC key in its own file, for example
  `toc-key.txt`: the key as 64 hex digits, optionally prefixed with `0x`. Whitespace is ignored and
  `#` starts a comment. Keep the file private and out of Git, like the archive keys.

**Run it** from this repository (the .NET SDK is the only other requirement):

```sh
dotnet run --project GameDataExtract/src/Ares.GameDataExtract -- extract \
  --game "/path/to/Arc Raiders" \
  --keys /path/to/archive-keys.txt \
  --toc-key /path/to/toc-key.txt \
  --out /path/to/gamedata
```

| Option | Meaning |
| --- | --- |
| `--game <dir>` | Game folder, the one holding `PioneerGame/`. |
| `--keys <file>` | Archive keys file (pak and container keys), the same file as Host's `archive-keys-file`. |
| `--toc-key <file>` | TOC key file. |
| `--out <dir>` | Folder for the game data; created when missing; must not be inside the game folder. |
| `--help`, `--version` | Show the usage, or the ARES version and supported game build. |

All four options are required. The run takes up to a minute and about 2 GB of memory. It writes the global
tables at the top of the output folder (items, menu item tables, item values, loot container types and classes, loot
tables, movement tables; JSON, one file per table), each supported map's files under `maps/<map>/` (currently
`TheDam_02_P`: the collision `TheDam_02_P.collision`, about 115 MB, and its JSON world tables, about 7 MB), then
`manifest.json`, which records the game build, the ARES version and the SHA-256 of every file. Each file replaces an
earlier one only once it is complete. It prints what it wrote. Exit code 0 means success, 1 that the game's files
could not be read, and 2 invalid options or inputs: a missing folder or file, keys for another game build, or a TOC
key that does not decrypt the game's archive index. The keys are never printed.

**Point Host at it:** set `game-data-dir` in `ares-host.conf` to the output folder (relative paths are
relative to the configuration file) and restart Host. It is required: Host loads the game data when it
starts and refuses to start (exit code 2, with the reason on standard error) when the setting is empty,
the folder or its `manifest.json` is missing, it was extracted from another game build, a file is missing,
malformed or changed after extraction, or a game name ARES needs (an item, a map, a gameplay tag…) is missing.
Extract again after updating ARES too, with the ARES-GameDataExtract of the same version. Host passes the folder
to every Raid it starts, and each Raid loads it the same way when it starts, so raids started after a new extraction
use the new data.

**Servers without the game:** the server that runs Host and Raid does not need the game. Run
ARES-GameDataExtract on any machine with the supported game build installed, copy the output folder to the
server, for example `scp -r gamedata user@server:/srv/ares/`, and set `game-data-dir` there.

**Check it worked:** at startup the Host log shows `"game_data": "loaded"` with the folder, game build,
item count and maps. When Host starts a raid, the Host log shows the Raid's same event and
`"map_collision": "loaded"` with its placement and height-field counts. Map collision stays optional:
`"map_collision": "unavailable"` comes with a `warning` that says why (the collision file is missing or
unusable), and raids then run without collision.

## ARES content

Some backend data the game's menu needs is not in the installed game: the stash and loadout
structures, what recycling each item gives, the trader offers and item quality levels. ARES ships it
as default content, built into ARES-Host and ARES-Raid and copied next to
ARES-Host in `default-content/` (`stash.json`, `loadout.json`, `item-recycling.json`,
`trader-offers.json`, `item-quality-levels.json`; JSON in the backend's wire format). To change it,
copy a file into a folder of your own, edit it, set `content-dir` in `ares-host.conf` to that folder
and restart Host: each file there replaces the default of the same name, the others stay built in.
Host passes the folder to every Raid it starts (`--content-dir`), and each Raid loads it the same way
when it starts. Host and Raid refuse to start (exit code 2, with the reason on standard error) when a
file is malformed, inconsistent or does not fit the game data, or when the folder holds another JSON
file. Stash upgrade prices stay in `stash-upgrade-coins`, which needs one price per upgrade of the
content's stash.

## Contributing

Contributions to the servers and standalone Structs library are welcome. See
[CONTRIBUTE.md](CONTRIBUTE.md) for architecture rules, workflow and development checks.

## License

ARES contributions in this repository are licensed under the GNU General Public License,
version 3 or (at your option) any later version (`GPL-3.0-or-later`). See [LICENSE](LICENSE).
Third-party material retains its applicable licenses and notices.
