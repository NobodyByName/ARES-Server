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

## Status

A stock client goes from a fresh launch through sign-in, the main menu and normal Dam matchmaking
into a raid hosted by ARES-Raid: Dam loads with its lighting, the player gets the default Raider,
moves (walk, sprint, crouch, slide, jump), climbs ladders and uses door buttons, then leaves back to
the main menu.

Host persists the local account's stash and selected loadout. Persisted raid reservations are
checked at startup and periodically; inventory unlocks when Raid confirms the old reservation is gone.
The standard player containers and rubber duck appear in the raid inventory, and whole-stack drag
moves between compatible slots are
validated by Raid. Inventory and map screens show their shared menu frame.
Placement changes remain inside the active raid; post-raid inventory settlement
is not implemented.

Combat, Looting and Tactical/Utility Mk.1 augments support persistent menu selection, atomic
swaps, capacity validation and matching Raid frames. Raid initializes the carrying limit and
current weight from the selected loadout. Higher tiers and augment skills remain unsupported.

Light Shields support persistent menu selection, atomic swaps and Raid backpack/armor-slot moves.
Raid replicates their initial shield health and includes their weight. Damage mitigation, depletion,
equip charge-up, repair, heavier shields and movement penalties remain future work. Shield support
has automated coverage; menu equipment, Raid shield bars and inventory display are stock-client verified.

Limitations:

- One player and one raid at a time; Dam only.
- Identity is synthetic: every player uses one local account, with no real sign-in or separate
  player accounts and progression yet.
- Movement is trusted from the client; the server does not check it yet.
- No combat, loot, death or match results yet.
- Other equipment, higher augment tiers, other item types and inventory operations beyond whole-stack swaps are not implemented yet.
- Plain HTTP between the game and ARES-Host; connection trust is not verified.
- Tested on one PC only (loopback or its own LAN address). Other machines and the internet have not
  been tested yet.

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
