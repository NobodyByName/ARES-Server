# ARES-Server

Independent replacement servers for ARC Raiders: the backend a stock game client signs in to, and
the game-session server that runs its raids. Players connect with
[ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher) (a separate repository) and their own, unmodified game.

> **Unofficial project.** ARES is independent and is not affiliated with, endorsed by or sponsored
> by Embark Studios. ARC Raiders and all related names and trademarks belong to their respective
> owners. ARES works only with an installed, unmodified copy of the game.

## Components

| Component | Role |
| --- | --- |
| [Host](Host/) | ARES-Host: backend for sign-in, the main menu, parties and matchmaking; starts a Raid server for each raid. |
| [Raid](Raid/) | ARES-Raid: independent game-session server that runs the raid. |
| [Structs](Structs/) | Standalone library of decoded ARC Raiders structures, wire formats and game data for community projects. |
| [Common](Common/) | Shared server logic used by Host and Raid, and the map file format. |
| [MapData](MapData/) | ARES-MapData: operator tool that extracts the [map data](#map-data) from an installed game. |

## Content progress

[![ARES content map: individual maps, ARC enemies, items and equipment, world objects, objectives, skills, traders, furniture and appearance](docs/content-map/overview.svg)](docs/content-map/README.md)

[Explore content](docs/content-map/README.md) · [Current server support](docs/content-map/support.md)

Each cell is one content piece: **blue** = partial, **orange** = not implemented,
**gray** = not assessed. Group sizes are balanced for readability; internal names and
availability still need review.

## Requirements

- .NET 10 SDK to build from source (version in `global.json`). No game installation is needed to build or test.
- An operator-managed MongoDB replica set for Host persistence and integration tests (tested with MongoDB 8.0).
- Players need their own, unmodified copy of **ARC Raiders 2.4.1, Steam build 26439197**, and
  [ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher). ARES supports one game build at a time;
  players on a newer build must wait for ARES to support it.
- Operators must supply the game's [archive keys](#archive-keys). ARES includes no game keys or game files.
- For map collision, operators extract [map data](#map-data) from their own game installation.
- Developed and tested on Linux. Support for every ARC Raiders platform is a goal; Windows has not been tested yet.

## Archive keys

Operators must provide the supported build's pak and container keys in `archive-keys.txt` beside
`ares-host.conf`, or select another file with `archive-keys-file`. Host requires matching keys at
startup and supplies them to ARES-Launcher to read each player's own game files. Keep the key file
private and out of Git.

## Map data

ARES-Raid needs the map's collision (ground, walls, buildings and other solid objects) to move players
correctly. ARES does not include it: like the map extractors of other server emulators, ARES-MapData
reads it from the operator's own game installation and writes ARES's map files. The files are derived
from the game: keep them on your own servers and never distribute them.

**When to run it:** at first setup and again after every game update. Each file is stamped with the
game build it was extracted from. ARES-Raid refuses a file from another build and then runs raids
without collision, so extract again whenever the supported game build changes.

**What it needs:**

- **Game folder:** an installed copy of the supported game build (the folder holding `PioneerGame/`).
  It is only read, never changed.
- **Archive keys file:** the same `archive-keys.txt` Host uses (see [Archive keys](#archive-keys)).
- **TOC key file:** the key of the game's archive index (the `.utoc` files). Only ARES-MapData needs
  it; Host does not. Operators must provide the supported build's TOC key in its own file, for example
  `toc-key.txt`: the key as 64 hex digits, optionally prefixed with `0x`. Whitespace is ignored and
  `#` starts a comment. Keep the file private and out of Git, like the archive keys.

**Run it** from this repository (the .NET SDK is the only other requirement):

```sh
dotnet run --project MapData/src/Ares.MapData -- extract \
  --game "/path/to/Arc Raiders" \
  --keys /path/to/archive-keys.txt \
  --toc-key /path/to/toc-key.txt \
  --out /path/to/map-data
```

| Option | Meaning |
| --- | --- |
| `--game <dir>` | Game folder, the one holding `PioneerGame/`. |
| `--keys <file>` | Archive keys file (pak and container keys), the same file as Host's `archive-keys-file`. |
| `--toc-key <file>` | TOC key file. |
| `--out <dir>` | Folder for the map files; created when missing; must not be inside the game folder. |
| `--help`, `--version` | Show the usage, or the ARES version and supported game build. |

All four options are required. The run takes up to a minute and about 2 GB of memory. It writes one
file per supported map into the output folder (currently `TheDam_02_P.collision`, about 50 MB) and
replaces an earlier file only once the new one is complete. It prints what it wrote. Exit code 0 means
success, 1 that the game's files could not be read, and 2 invalid options or inputs: a missing folder
or file, keys for another game build, or a TOC key that does not decrypt the game's archive index.
The keys are never printed.

**Point Host at it:** set `map-data-dir` in `ares-host.conf` to the output folder (relative paths are
relative to the configuration file) and restart Host. Host passes the folder to every Raid it starts;
each Raid loads the file when it starts, so raids started after a new extraction use the new file.

**Servers without the game:** the server that runs Host and Raid does not need the game. Run
ARES-MapData on any machine with the supported game build installed, copy the output folder to the
server, for example `scp -r map-data user@server:/srv/ares/`, and set `map-data-dir` there.

**Check it worked:** when Host starts a raid, the Host log shows the Raid's event
`"map_collision": "loaded"` with its placement and height-field counts. `"map_collision": "unavailable"`
comes with a `warning` that says why: `map-data-dir` is not set, the folder or file is missing, or the
file is unusable (for example extracted from another game build). Raids then run without collision.

## Contributing

Contributions to the servers and standalone Structs library are welcome. See
[CONTRIBUTE.md](CONTRIBUTE.md) for architecture rules, workflow and development checks.

## License

ARES contributions in this repository are licensed under the GNU General Public License,
version 3 or (at your option) any later version (`GPL-3.0-or-later`). See [LICENSE](LICENSE).
Third-party material retains its applicable licenses and notices.
