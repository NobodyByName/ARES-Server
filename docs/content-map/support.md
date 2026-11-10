# Current server support

[Back to ARES-Server](../../README.md) · [Content map](README.md)

A stock client goes from a fresh launch through sign-in, the main menu and normal Dam matchmaking
into a raid hosted by ARES-Raid: Dam loads with its lighting, the player gets the default Raider,
moves (walk, sprint, crouch, slide, jump), climbs ladders and uses door buttons, then leaves back to
the main menu.

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
Placement changes remain inside the active raid; post-raid inventory settlement
is not implemented.

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
validates whole-stack moves. Every raider also holds the Raider Tool and empty hands.
The Ferro I shows its three empty mod slots (muzzle, underbarrel, stock) in the menu and in Raid.

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
  There is no progression yet.
- Movement is trusted from the client; the server does not check it yet.
- No damage, loot, death or match results yet.
- Other equipment, higher augment tiers, other item types and inventory operations beyond whole-stack swaps are not implemented yet.
- Plain HTTP between the game and ARES-Host; connection trust is not verified.
- Tested on one PC only (loopback or its own LAN address). Other machines and the internet have not
  been tested yet.
