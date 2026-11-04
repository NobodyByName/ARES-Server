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
- Main-menu storage starts with one rubber duck and an empty standard player loadout. Moving whole
  items between compatible empty slots is supported. Raid displays the standard containers and carried
  duck and supports whole-stack drag moves. Equipment, additional item types, loot and result settlement
  are not implemented yet.
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
`public-origin`, `game-build` and `data-dir` select their documented defaults. Archive keys remain
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

The configuration keys match the existing long options (`port`, `bind`, `data-dir`,
`server-name`, `motd`, `game-build`, `raid-capacity`, `public-origin`, `allow-network-clients`,
`session-control-url`, `session-control-token-file`, `auth-key-file`, `archive-keys-file`,
`console`, `log-file`). `config`, `help` and `version` are command-line options only.

## Operator console

This server has its own interactive console with readable logs and a fixed command row.
Use `help`, `status`, `clear`, or `stop`. See [console controls and output modes](../README.md#operator-consoles).

## Inventory

An empty local account receives one standard rubber duck in a basic stash with 64 slots. This
starter grant is saved once; restarting Host or signing in again does not grant another duck.
Existing non-empty inventories are preserved, and clearing/importing inventory after the grant
does not trigger it again.

The starter stash also receives an empty standard player loadout, including a ten-slot backpack,
quick-use slots, a safe pocket and equipment slots. Existing starter saves gain this layout once,
preserving their stash items and instance IDs. Clearing/importing inventory afterward does not
repeat the layout grant.

The menu commits pending moves when Storage closes. Host authenticates the batch, checks the
destination's allowed items and capacity, and saves the entire batch atomically. A rejected batch
changes nothing. Recent nonempty request IDs are persisted so retries cannot reapply a completed
move. The stock menu's empty request ID is also accepted; repeating the same move is a no-op.
Moves preserve item identity, amount and durability. Inventory reserved for matchmaking is locked.
Splitting, merging, crafting, recycling and other economy operations are not supported.

Items, item definitions, selected loadout roots and the pending reservation are stored in
`inventory-state.json` under the configured `data-dir`. The save identifies the supported game build. Malformed
saves or saves from a different game build cause startup to fail instead of resetting the account.

In the interactive console, `inventory` shows the revision, item count and reserved ticket.
`inventory import <path>` replaces an inventory that has no outstanding loadout reservation.
Quote paths containing spaces. Import files contain:

```json
{
  "items": [],
  "definitions": [],
  "loadoutRoots": []
}
```

Each item uses the backend's `instanceId`, `gameAssetId`, `amount`, `slots`, `durability`,
`maxDurability`, `lockedInRound`, `etag` and `updatedAt` fields. `slots` lists child instance IDs;
`loadoutRoots` selects complete root trees for Raid. Other trees stay in the Host inventory.
Imported items must have an empty `lockedInRound`. Each item needs a definition in `definitions`
or the built-in tables; definition fields are described by `ItemDescription` in Structs.Backend.
Imports reject missing references, multiple parents, cycles, duplicate IDs and invalid amounts.
The limit is 256 item instances and a 1 MiB import file. Use definitions compatible with the
supported game; provisioning does not establish that an item renders correctly in the client.
Start a fresh client after changing definitions.

Host freezes the selected trees before allocating a raid. Raid acknowledges their player,
revision and content hash. A lost reply retains the reservation, including across a Host restart;
retrying uses the saved ticket and loadout. When Raid confirms that its allocation no longer
exists, Host releases the reservation without changing item contents. Raid can change placement inside
the active raid, but Host retains the original reserved tree: post-raid placement and inventory gains/losses
need result settlement before they can persist. Loot and death are not enabled. Both servers must support
the loadout acknowledgement contract. Raid currently accepts the standard frame and rubber duck;
unsupported loadouts fail preparation.
