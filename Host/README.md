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
  items between compatible empty slots is supported. Raid inventory UI/equipment replication,
  loot and result settlement are not implemented yet.
- One raid at a time.
- Plain HTTP. Serving other machines needs `--allow-network-clients`; connection trust is not
  verified.

The log records requests without authentication headers or request bodies.

## Options

```sh
dotnet run --project src/Ares.Host -- --help
```

```
  --port <n>                        Listen port (default 18080)
  --bind <ip>                       Listen address (default 127.0.0.1); non-loopback requires --allow-network-clients
  --public-origin <http://host:port>  Origin players configure; also the token issuer (default http://127.0.0.1:<port>)
  --allow-network-clients           Serve players beyond this machine (plain HTTP; trust unverified)
  --data-dir <dir>                  Persistent Host data (default: per-user application data/ARES-Host)
  --session-control-url <url>       Raid allocation API (loopback HTTP or HTTPS)
  --session-control-token-file <f>  Operator token shared with Raid
  --auth-key-file <f>               Identity signing key, 32-4096 bytes (default: none; tokens are not checked)
  --archive-keys-file <f>           Game archive keys for ARES-Launcher, required (default archive-keys.txt; see README)
  --server-name <text>              Shown in launcher server lists (default ARES-Host)
  --motd <text>                     Message of the day for launcher server lists
  --game-build <digits>             Steam build id this server supports (default 26159202)
  --raid-capacity <n>               Concurrent raids this deployment hosts (default 1)
  --version                         Show the ARES version and supported game build
```

Without `--session-control-url`, matchmaking never finds a raid. ARES-Host does not start without
the game's archive keys ([Archive keys](../README.md#archive-keys)). The
[repository README](../README.md#run) shows a complete setup with ARES-Raid.

Build, requirements, game updates and license: see the [repository README](../README.md).

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
`inventory-state.json` under `--data-dir`. The save identifies the supported game build. Malformed
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
exists, Host releases the reservation without changing item contents. This preserves carried
items for the current read-only handoff; inventory gains/losses need result settlement before
loot or death can be enabled. Both servers must support the loadout acknowledgement contract.
