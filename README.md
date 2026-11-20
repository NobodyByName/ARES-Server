# ARES-Server

Independent replacement servers for ARC Raiders: the backend a stock game client signs in to, and
the game-session server that runs its raids. Players connect with
[ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher) (a separate repository) and their own, unmodified game.

> **Unofficial project.** ARES is independent and is not affiliated with, endorsed by or sponsored
> by Embark Studios. ARC Raiders and all related names and trademarks belong to their respective
> owners. ARES works only with an installed, unmodified copy of the game.

## Help wanted: multiplayer testers

Everything so far has been tested by one person on one PC, so parties, two players in the same
raid and PvP have never been tried with a real second player. If you have ARC Raiders and can
spare an evening, I'd love to play a few test raids with you. Say hi in the
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
- The client target for this trial is **ARC Raiders 2.6.0, Steam build 26619995**. Players connect with
  an unmodified game and [ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher).
  Connection and raid compatibility still need a stock-client test.
- Operators must supply the game's [archive keys](#archive-keys). ARES includes no game keys or game files.
- For this trial, Host and Raid require [game data](#game-data) previously extracted from build 26439197.
  Fresh extraction from the 2.6.0 install stops at the wrapped script cache.
- Developed and tested on Linux. Support for every ARC Raiders platform is a goal; Windows has not been tested yet.

## Archive keys

Operators must provide the supported build's pak and container keys in `archive-keys.txt` beside
`ares-host.conf`, or select another file with `archive-keys-file`. Host requires matching keys at
startup and supplies them to ARES-Launcher to read each player's own game files. Keep the key file
private and out of Git.

## Game data

Host and Raid need the game's data: the item catalogue, loot tables, movement tuning, each map's world tables
(loot sockets, loose item sockets, doors, ladders, ziplines, player starts, extraction points…) and its collision (ground, walls,
buildings and other solid objects). ARES does not include it: like the map extractors of other server emulators,
ARES-GameDataExtract reads it from the operator's own game installation and writes ARES's game data files. The files are derived
from the game: keep them on your own servers and never distribute them.

The manifest and collision header identify the source game build. Run the extractor at first setup
and whenever the required **data build** changes. For the 2.6.0 client trial, point `game-data-dir`
at your existing 2.4.1 extract.

**What it needs:**

- **Game folder:** an installed copy of the required data build (the folder holding `PioneerGame/`).
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

Some data ARES needs is not in the installed game: the stash and loadout structures, what recycling each
item gives, the trader offers, item quality levels, and how loot spawns and what it holds. ARES ships it in
`content/` (`stash.json`, `loadout.json`, `item-recycling.json`, `trader-offers.json`, `item-quality-levels.json`,
`loot-spawn.json`, `loot-contents.json`, `loot-rules.json`), as close to the official game as known, and copies it next to ARES-Host. That folder is
the server's content: to change it, edit its files and restart Host. `content-dir` in `ares-host.conf` names
the folder (default `content`). Host passes it to every Raid it starts (`--content-dir`), and each Raid loads
it when it starts. Host and Raid refuse to start (exit code 2, with the reason on standard error) when the
folder or one of its files is missing, a file is malformed, inconsistent or does not fit the game data, or
the folder holds another JSON file. Stash upgrade prices stay in `stash-upgrade-coins`, which needs one price per upgrade of the
content's stash.

### Loot

The game files say where loot containers can stand and what each loot table can give, but not how often
containers spawn or which tables fill which container, so all of that is content, shipped as close to the
official game as known:

- `loot-spawn.json`: the spawn chance per container tag (a tag without an entry uses its nearest parent's),
  a multiplier per area loot value (`Unknown` is outside every valued area), whether the game's per-group caps
  (`groupCaps`) and placement limiters (`limiters`) apply, the container classes dynamic spawners may place
  (`spawnerClasses`), and the chance per loose item socket tag (`looseChances`, nearest parent tag like `chances`;
  the area multipliers and limiters apply to loose items too).
- `loot-contents.json`: item drop tags no container gets, the item tiers (drop tag, weight, per-area
  multipliers; the tier without a drop tag takes every other item) and the tier of a container's first stack,
  the loot tables each container category draws from (`tablePools`), the extra stack some containers get, and
  the item pools of containers without tables (`typePools`), and what loose item sockets hold (`looseItems`):
  one stack each, either an item of the listed `tables` whose own placement list accepts the socket's tag (by tier,
  `amount` per stack) or, for the tags a `pools` entry lists (with the tags below them), an item of its `entries`
  (drop tag, amount, weight) drawn against the table items' `tableWeight`.
  Its `profiles` change those defaults per container or loose item socket type: each profile names one `tag`
  (`Socket.LootContainer…` or `Socket.Loot…`; it also covers the tags below it) or one container `class`. A profile can
  keep `only` items matching a drop tag or item name, scale an item's pick weight (`weights`, `multiplier` 0 removes it),
  `add` items the tables do not offer (with an `amount` and optional `tier`; their own placement lists are not checked),
  scale tier weights (`tiers`), and for containers set `firstStackTier` (`null` turns it off), `stacks` and
  `extraStack` (`chance`, `entries`; setting it gives the extra stack even to pools without one). A container combines
  the profiles of its tag and every parent tag, then of its class, the nearer one winning for each setting; weights of
  several profiles multiply. A `loot-rules.json` contents list still replaces everything.
- `loot-rules.json`: per-container and per-loose-item-socket decisions, below.

`loot-rules.json` decides, for chosen loot containers and loose item sockets, whether they spawn and what they
hold; every other one follows the two files above. The shipped rules set how many doors of each server rack and
locker rack hold a container, as on the official game. A rule
covers the containers and loose item sockets it lists by id, every one whose tag (`Socket.LootContainer.*` for
containers, `Socket.Loot` / `Socket.Loot.*` for loose item sockets) is one of its tags or below it, or all of them
when it lists neither. It can set a spawn chance, a group fill, the contents, a reveal delay, or several of them:

```json
{
  "rules": [
    { "name": "More tech", "tags": ["Socket.LootContainer.Tech"], "spawnChance": 0.5 },
    {
      "name": "Armory",
      "containers": ["<cell>/<actor>/<component>/<socket>", "<cell>/<actor>"],
      "spawnChance": 1,
      "loot": {
        "stacks": { "min": 2, "max": 4 },
        "items": [
          { "item": "DA_Item_BattleRifle_BreachAction_01", "weight": 1 },
          { "item": "DA_Item_HeavyAmmo", "weight": 3, "amount": { "min": 10, "max": 30 } }
        ]
      }
    }
  ]
}
```

- **Container ids** (listed in `containers`) come from the game data's map tables: a prop socket is
  `cell/actor/component/socket` (`loot-sockets.json`), and so is a loose item socket (`loose-item-sockets.json`); a
  placed container or a dynamic loot spawner is `cell/actor` (`placed-loot.json`, `dynamic-loot-spawners.json`;
  every container a spawner places shares its id). The cell is empty for the map's persistent level. Raid logs each
  container's id when it is first opened.
- **`spawnChance`** (0 to 1) replaces the game's decision for the containers and loose item sockets the rule
  covers: no area value, group cap or limiter applies to them, and a spawner with a chance runs on it instead of its
  group's pick.
- **`groupFill`** (container sockets only) replaces the game's decision for a group of sockets: a weight for
  each number of filled sockets, such as `{"0": 6, "1": 4, "2": 9}`; each group draws a number (at most its
  sockets), then that many of its sockets at random get a container. The weights are those outside every area with a
  loot value: in an area, the weight of each number is multiplied by the area's `areaMultipliers` factor (from
  `loot-spawn.json`, taken at the group's first socket) to the power of that number, which multiplies every
  socket's odds by the factor and keeps the group's shape (with the shipped ×2 Medium, a locker rack's 32.6 % chance of
  2 filled doors and 2.6 % of 5 become 20.8 % and 13.3 %; ×3 High: 10.9 % and 23.4 %). The containers a rule lists form one group;
  otherwise each group is the game's own (the sockets of one placed prop with the same tag). Placed containers,
  spawners and loose item sockets ignore it.
- **`loot`** replaces the rolled contents: `stacks` (default: the game's usual 2 or 3, always capped by the
  container's slots) items drawn by `weight` (default 1; an item can come up more than once), each with an
  `amount` in its range (default 1, at most the item's stack size), at full durability. Items are named by
  technical name; any item players can carry in a raid is allowed. A loose item socket gets one stack instead
  (`stacks` does not apply): an item drawn by `weight` among the listed items that can lie in the world as a
  pickup (plain items, not weapons or shields), with its `amount`; with none of those it keeps its default roll.
- **`revealDelaySeconds`** (0 or more) replaces the container's own wait between the end of a first search's hold
  and the start of the reveal (the game's lid opening time, from the game data), after which the hidden items show
  one at a time. Loose item sockets ignore it.
- Rules apply in order: for each setting, the last rule that covers a container or loose item socket wins
  (`spawnChance` and `groupFill` count as one setting for container sockets). Raid's `raid_loot` log counts the
  loose item sockets a rule decided (`loose.decided_by_rule`).

Host and Raid refuse to start when a loot file names a tag, container, loose item socket, class, table, item or
drop tag the game data does not have.

## Contributing

Contributions to the servers and standalone Structs library are welcome. See
[CONTRIBUTE.md](CONTRIBUTE.md) for architecture rules, workflow and development checks.

## License

ARES contributions in this repository are licensed under the GNU General Public License,
version 3 or (at your option) any later version (`GPL-3.0-or-later`). See [LICENSE](LICENSE).
Third-party material retains its applicable licenses and notices.
