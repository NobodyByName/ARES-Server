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
| [Host](Host/) | ARES-Host: backend for sign-in, the main menu, parties, matchmaking and raid assignment. |
| [Raid](Raid/) | ARES-Raid: independent game-session server that runs the raid. |
| [Structs](Structs/) | Standalone library of decoded ARC Raiders structures, wire formats and game data for community projects. |
| [Common](Common/) | Shared server logic used by Host and Raid. |

## Content progress

[![ARES content map: individual maps, ARC enemies, items and equipment, world objects, objectives, skills, traders, furniture and appearance](docs/content-map/overview.svg)](docs/content-map/README.md)

[Explore content](docs/content-map/README.md) · [Current server support](docs/content-map/support.md)

Each cell is one content piece: **blue** = partial, **orange** = not implemented,
**gray** = not assessed. Group sizes are balanced for readability; internal names and
availability still need review.

## Requirements

- .NET 10 SDK to build from source (version in `global.json`). No game installation is needed to build or test.
- An operator-managed MongoDB replica set for Host persistence and integration tests (tested with MongoDB 8.0).
- Players need their own, unmodified copy of **ARC Raiders, Steam build 26159202**, and
  [ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher). ARES supports one game build at a time;
  players on a newer build must wait for ARES to support it.
- Operators must supply the game's [archive keys](#archive-keys). ARES includes no game keys or game files.
- Developed and tested on Linux. Support for every ARC Raiders platform is a goal; Windows has not been tested yet.

## Archive keys

Operators must provide the supported build's pak and container keys in `archive-keys.txt` beside
`ares-host.conf`, or select another file with `archive-keys-file`. Host requires matching keys at
startup and supplies them to ARES-Launcher to read each player's own game files. Keep the key file
private and out of Git.

## Contributing

Contributions to the servers and standalone Structs library are welcome. See
[CONTRIBUTE.md](CONTRIBUTE.md) for architecture rules, workflow and development checks.

## License

ARES contributions in this repository are licensed under the GNU General Public License,
version 3 or (at your option) any later version (`GPL-3.0-or-later`). See [LICENSE](LICENSE).
Third-party material retains its applicable licenses and notices.
