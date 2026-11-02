# Structs

Decoded ARC Raiders structures and data needed to emulate its servers: wire formats and their
codecs, class/replication schemas, and decoded game data, all as plain C# types. It is a standalone
library: other ARC Raiders community projects can use it and contribute to it.

**Depends on:** nothing (no other component and no third-party packages).

## Status

Used by Host and Raid. It covers what those servers need so far, not the whole game. Members whose
meaning is not understood yet are named `Unknown…`.

- `Ares.Structs.Core`: the supported game build, geometry types and bit-stream serialization.
- `Ares.Structs.Net`: session handshake and packet framing, control-channel messages, replication
  (object exports, actor and subobject content, property and RPC codecs, the schema model) and
  gameplay-tag queries.
- `Ares.Structs.Game`: decoded data of the current build: replication schemas, the Dam map
  (player starts, data layers, streaming cells, lighting, rounds, doors), player classes,
  character defaults and abilities, and Pendulum movement (input registry, frame codec, pawn
  blackboard).
- `Ares.Structs.Backend`: backend API shapes and default content.

Only the supported game build is described (`GameBuild` in `Ares.Structs.Core`); older builds
live in the Git history.

Build, requirements, game updates and license: see the [repository README](../README.md).
