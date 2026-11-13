# Current server support

[Back to ARES-Server](../../README.md) · [Content map](README.md)

A stock client goes from a fresh launch through sign-in, the main menu and normal Dam matchmaking
into a raid hosted by ARES-Raid: Dam loads with its lighting, the player gets the default Raider,
moves (walk, sprint, crouch, slide, jump), climbs ladders, uses door buttons and breaches barricaded
doors, searches loot containers, calls an extraction elevator and descends with it, then leaves back
to the main menu.

Host starts one ARES-Raid process per raid during matchmaking, up to a configured number at once,
and each process exits when its raid is over. Host persists each account's stash and selected
loadout. A raid's loadout stays locked until that raid is over; restarting Host ends its raids and
unlocks the loadout.

A raid holds up to `raid-players` players across its parties (default 1). A new party joins a
running raid that has room during that raid's first `raid-fill-seconds` (default 60); otherwise
Host starts another raid. Players in the same raid see each other's characters walk, crouch, hold
their gun and aim. Each player leaves on their own, and the raid ends when the last one leaves.

The standard player containers and rubber duck appear in the raid inventory, and whole-stack drag
moves between compatible slots are
validated by Raid. Inventory and map screens show their shared menu frame.
When a raid ends, Host saves what the player carries. Extracting by the elevator brings the whole
loadout home as it was at departure (augment, shield, both weapons, backpack, quick-use items and
Safe Pockets) with picked-up loot, split stacks, current amounts and durability. Rounds loaded in
guns are unloaded back into ammo stacks, or new stacks in free backpack slots; rounds with no room
are lost. Items left in the world, even in the elevator, are gone. Surrendering, or disconnecting
and not returning (final after the reservation times out, about two minutes), brings home only the
Safe Pocket items; everything else, the augment included, is lost. A player who never got into the
raid keeps their original inventory. The menu shows the result at once. This is stock-client
verified with one player.

Combat, Looting and Tactical/Utility Mk.1 augments support persistent menu selection, atomic
swaps, capacity validation and matching Raid frames. Raid initializes the carrying limit and
current weight from the selected loadout. Higher tiers and augment skills remain unsupported.

Light Shields support persistent menu selection, atomic swaps and Raid backpack/armor-slot moves.
Raid replicates their initial shield health and includes their weight. Damage mitigation, depletion,
equip charge-up, repair, heavier shields and movement penalties remain future work. Shield support
has automated coverage; menu equipment, Raid shield bars and inventory display are stock-client verified.

Ferro I and Heavy Ammo have menu item definitions and persistent storage. Ferro I fits both
weapon slots and the backpack; Heavy Ammo stacks hold 1–60 rounds and move as whole stacks through
the stash, backpack and safe pocket. Raid accepts both, includes them in the carried weight and
validates whole-stack moves. In the menu and in Raid, stacks can also be split and merged up to their stack limit.
Every raider also holds the Raider Tool and empty hands.
The Ferro I shows its three empty mod slots (muzzle, underbarrel, stock) in the menu and in Raid.

Carried items can be dropped from the Raid inventory and picked up again. A dropped stack appears about a
metre ahead of the player at chest height, a dropped amount merges into a nearby pile of the same item, and
pressing interact takes the stack back; if the backpack is full, the rest stays on the ground. Dropping and
picking up Heavy Ammo and the Ferro I is stock-client verified with one player. Dropped items float where
they appear, because there is no physics yet; merging piles, partial pickups and a second player seeing
the items have not been tried with the stock client.

Dam raids have loot containers: drawers, lockers, cabinets, car trunks, crates and the other containers
on the map's props, plus free-standing weapon cases and the placed machine doors. Each raid rolls which
of them appear, with chances per container category raised in the map's medium- and high-value areas and
limited per area and per prop, so a raid has a few hundred of them. Players search a container (holding
interact, with the Raider Tool where the game breaches it), see its contents and take items into their
own inventory; an opened container stays open for every player. Every container currently holds one
rubber duck: loot tables are not decoded yet. In the Raid inventory, stacks can also be split and merged
up to their stack limit. Searching, taking items and splitting are stock-client verified with one player.

Operators can steer running raids from the Host console with `raid players`, `raid teleport`,
`raid teleport-to-loot` and `raid spawn-loot` (see `help`); each raid's answer appears in the Host log.
These are development tools.

With [map data](../../README.md#map-data) configured, each raid loads the map's collision when it
starts. Raid does not use it for movement yet.

In Raid the slot-1 Ferro I starts drawn, and equipped guns start loaded from carried Heavy Ammo.
Switching between the Ferro I, the Raider Tool and empty hands plays the put-away and draw
animations; the Ferro I fires at its semi-automatic cadence and reloads one round at a time from
carried Heavy Ammo. These are stock-client verified, with known issues. Damage, ballistics,
fitting mods and upgrades remain future work.

To grant menu test items from the Host console before a fresh client launch:

```text
inventory add DA_Item_BattleRifle_BreachAction_01
inventory add DA_Item_HeavyAmmo 60
```

`inventory add <technical-name> [quantity]` adds a new stack; quantity defaults to one and must
fit that item's stack limit. Existing stacks remain unchanged. Full stashes and raid reservations
reject grants. Starter provisioning still supplies only one rubber duck.

Limitations:

- Shared raids are a first version with known issues: players see each other's movement, held gun
  and aim only. Shots, effects, health and interactions between players are not shared yet, and
  every player is sent to everyone whatever the distance. Parties hold one player; Dam only.
- Accounts are made by the operator on the Host console; players sign in through ARES-Launcher.
  Experience and quests are not recorded yet.
- Movement is trusted from the client; the server does not check it yet.
- Locked and keycard doors cannot be unlocked yet. A cancelled breach starts over instead of keeping
  its progress.
- No damage, death or end-of-round results screen yet, and no rejoin after leaving a raid.
- Loot containers hold a placeholder rubber duck. Plants and other nature containers, weapon racks
  (they need the Security Breach skill) and keycard rooms are not supported, and containers are only
  in Dam.
- Broken-elevator repair and other extraction types are not implemented.
- Other equipment, higher augment tiers, other item types and inventory operations beyond whole-stack moves, drops, pickups, splitting and merging are not implemented yet.
- Dropped items have no physics, never expire, and a dropped weapon carries no mods; dropping the weapon in hand is not handled.
- Plain HTTP between the game and ARES-Host; connection trust is not verified.
- Tested on one PC only (loopback or its own LAN address). Other machines and the internet have not
  been tested yet.
