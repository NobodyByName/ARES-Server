# Host

ARES backend (`ARES-Host`): synthetic identity, accounts, party, matchmaking and raid assignment.

**Depends on:** Structs, Common.

## Status

Answers the backend requests a stock client makes from sign-in through the main menu and Dam
matchmaking, then assigns the player to a raid through the Raid allocation API. Playing a raid also
needs ARES-Raid.

Limitations:

- Identity is synthetic: every player uses one local account. There is no real sign-in or
  separate per-player progress. Inventory is stored for this local account only.
- Main-menu storage starts with one rubber duck and an empty standard player loadout. Whole-item
  moves and swaps between compatible slots are supported. Combat, Looting and Tactical/Utility Mk.1
  augments determine the loadout's frame capacities and carrying limit. Menu commits release
  confirmed lost raid reservations before saving. Higher tiers, other equipment, loot and result
  settlement remain unimplemented.
- One raid at a time.
- Plain HTTP. Serving other machines needs `allow-network-clients = true`; connection trust is not
  verified.

The log records requests without authentication headers or request bodies.

## Configuration

Start Host without a long argument list:

```sh
dotnet run --project Host/src/Ares.Host
```

Run this command from the repository root. On first launch, Host creates `ares-host.conf` in the
working directory from the bundled [reference template](ares-host.conf.dist), then loads it.
Edit the file and restart Host to apply changes. Existing files and comments are left untouched;
new settings omitted from an older file use built-in defaults. The `.conf.dist` file is shipped
beside the executable for reference and is never loaded as configuration.

The format follows Minecraft's simple `key = value` settings, with a commented distribution
template inspired by AzerothCore/TrinityCore. A minimal local setup with Raid allocation is:

```properties
# Paths are relative to this configuration file.
archive-keys-file = "archive-keys.txt"
session-control-url = http://127.0.0.1:17778
session-control-token-file = "operator-token"
auth-key-file = "identity-key"
server-name = "My ARES server"
motd = "Welcome to Dam"
```

All settings are documented in [ares-host.conf.dist](ares-host.conf.dist), including their defaults
and limits. Use one setting per line. Blank lines and full-line `#` comments are accepted; keys
are case-sensitive. Optional outer double quotes preserve surrounding spaces. `#`, `=` and
backslashes within values are literal; there are no inline comments, escape sequences or
variable expansion. Booleans use `true` or `false`. Unknown keys, repeated keys, malformed lines
and invalid effective values stop startup. Empty optional paths disable that setting; empty
`public-origin` and `game-build` select their documented defaults. Archive keys remain
required, even on first launch; Host leaves the generated file available if key loading fails.

Select another existing file with:

```sh
dotnet run --project Host/src/Ares.Host -- --config /path/to/ares-host.conf
```

An explicitly selected file must exist; Host never creates it. Relative paths **inside the file**
resolve from the file's directory, including the default `archive-keys.txt`. Absolute paths are
accepted. Keep tokens and keys in separate private files: configuration stores only their paths.
`ares-host.conf` is git-ignored; keep any custom-named operator configurations private too.

Existing command-line settings still work as overrides, with precedence **arguments → file →
built-in defaults**. Command-line paths retain their existing working-directory interpretation.
`--allow-network-clients` enables access regardless of the file's boolean value; to disable it,
set `allow-network-clients = false` in the file and omit that flag. `--help` lists all overrides;
`--help` and `--version` neither load nor create a configuration file.

For LAN access, set `bind = 0.0.0.0`, `allow-network-clients = true` and
`public-origin = http://<server-address>:18080` (matching `port`). Without
`session-control-url` and `session-control-token-file`, matchmaking never finds a raid.
ARES-Host also needs the game's archive keys ([Archive keys](../README.md#archive-keys)).
The [repository README](../README.md#run) shows a complete setup with ARES-Raid.

Build, requirements, game updates and license: see the [repository README](../README.md).

## Options

From the repository root:

```sh
dotnet run --project Host/src/Ares.Host -- --help
```

The configuration keys match the existing long options (`port`, `bind`,
`server-name`, `motd`, `game-build`, `raid-capacity`, `public-origin`, `allow-network-clients`,
`session-control-url`, `session-control-token-file`, `auth-key-file`, `archive-keys-file`,
`console`, `log-file`, `mongodb-uri`, `mongodb-uri-file`, `mongodb-database`). `config`, `help` and `version` are command-line options only.

## Operator console

This server has its own interactive console with readable logs and a fixed command row.
Use `help`, `status`, `clear`, or `stop`. See [console controls and output modes](../README.md#operator-consoles).

## Database

Host connects to an operator-managed MongoDB replica set. Configure `mongodb-uri` and
`mongodb-database` in `ares-host.conf`. A private file selected by `mongodb-uri-file` takes
precedence over `mongodb-uri`; use it for connections containing credentials. The generated
configuration contains a localhost URI and the database name `ares_host`.

Host creates and validates player documents in the configured database. Use MongoDB Compass
to browse documents and `mongosh` for terminal access.

## Inventory

Player documents store owned items in `inventory.stash.items` and `inventory.loadout`.
The loadout has single augment, shield, main weapon and secondary weapon fields, plus backpack,
QuickUse and Safe Pocket containers. Items share a single type containing the instance ID,
technical name, quantity, condition and optional container position.
Stash and single equipment items have no position. Backpack, QuickUse and Safe Pocket containers
store positioned items; their capacities are derived from shared loadout definitions. Empty equipment fields are null.
Empty slots, the frame/container tree and carried roots are generated for the client with stable IDs.
Host builds client definitions from the shared game data.

A fresh stash receives the starter duck. Purchased stash capacity preserves initialization state
when items are removed.

The menu commits moves when Storage closes. Host authenticates the batch, validates destinations
and capacity, and commits it atomically. Moving within stash is a no-op; stash display order is
derived. Moving between stash and carried slots preserves identity, quantity and durability.
Responses reconcile client prediction as well as saved changes: requested slots and each moved
item's canonical parent are reasserted even when storage is unchanged, so stash sorting cannot
leave a predicted duplicate behind.
Recent request receipts prevent duplicate application after retries/restarts. Split/merge,
crafting, recycling, currency purchases and post-raid settlement remain unimplemented.

The empty augment slot and all supported frames are always generated. Equipping Combat, Looting
or Tactical/Utility Mk.1 selects its shared frame definition and capacities. Each frame has its own
stable slot IDs, allowing the stock menu to migrate items and refresh its layout immediately before
committing the batch. Inactive frames stay empty and are excluded from Raid handoffs.
An equip/remove/swap batch must leave every item within capacity; otherwise no change is saved.

To add test items from the local Host console before a fresh client launch:

```text
inventory add DA_Item_Augment_Combat_T1
inventory add DA_Item_Augment_Looting_T1
inventory add DA_Item_Augment_Utility_T1
```

Tab also completes commands and subcommands: `inv` becomes `inventory`, `inventory a` becomes
`inventory add`, and `inventory im` becomes `inventory import`. Completion matches text anywhere
in the current token, ignoring case: `inventory add augment` cycles the
three supported augments. Prefix searches first expand their common prefix; repeated Tab/Shift+Tab
cycles matching supported names.
Each command adds one item to the stash, preserving existing items. Full stashes and reserved
inventories reject additions. Starter provisioning remains one duck; augments require explicit grants.

Use `inventory` in the console to see owned stack count, capacities, revision and the pending raid
ticket. `inventory import <path>` reads a player inventory object and replaces the idle inventory
using its current revision.

Raid handoffs are stored separately in `raidReservations`. Host freezes the carried inventory and
retains its ticket across restarts or lost replies. Raid acknowledges the player, revision and
content hash. Release removes the handoff when Raid confirms it is gone; it does not apply a raid
result. Raid accepts the standard frame, supported Mk.1 augment frames, and their carried duck or
spare augments. Loot, death, equipment use and inventory gains/losses remain unimplemented.
