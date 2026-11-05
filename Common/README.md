# Common

Logic shared by Host and Raid: shared game rules, identity tokens, the Host/Raid control contract
and common hosting infrastructure.

**Depends on:** Structs.

## Status

Implemented and used by Host and Raid.

- `Ares.Common`: shared command-line, endpoint and logging helpers.
- `Ares.Common.Security`: HS256 identity tokens, operator tokens and secret files.
- `Ares.Common.Control`: the Host/Raid allocation contract and the client Host uses.
- `Ares.Common.Control.Server`: the allocation endpoints a Raid server exposes.
- `Ares.Common.Game`: identity claims, inventory validation and shared loadout/frame definitions.

Menu inventory snapshots allow up to 1,024 nodes, including empty frames used for local augment
changes. Raid handoffs select only the active frame and augment trees and retain the 256-item limit.

Allocation status accepts either a match id or a persisted ticket id. Ticket lookup is read-only;
Host validates the frozen player/loadout receipt before trusting it after a restart.

Build, requirements, game updates and license: see the [repository README](../README.md).
