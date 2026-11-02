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
- `Ares.Common.Game`: shared game rules (identity claims so far).

Build, requirements, game updates and license: see the [repository README](../README.md).
