# Raid

ARES game-session server (`ARES-Raid`): transport, replication and the raid world.

**Depends on:** Structs, Common.

## Status

Hosts one Dam raid at a time for the player Host reserves it for. A stock client completes the
handshake and login, loads Dam with its lighting, is given its default Raider, moves (walk, sprint,
crouch, slide, jump) and uses door buttons, then leaves back to the main menu.

Limitations:

- One player and one raid at a time; Dam only.
- Movement is trusted from the client; the server does not check it yet.
- No combat, loot, death or match results yet.

Projects:

- `Ares.Raid.Transport`: packets, reliable channels, handshake and login.
- `Ares.Raid.Replication`: actor and property replication.
- `Ares.Raid.World`: the raid world, players, movement input and doors.
- `Ares.Raid`: the server process and the allocation API Host calls.

## Options

```sh
dotnet run --project src/Ares.Raid -- --help
```

```
  --port <n>                  UDP game port (default 7777)
  --bind <ip>                 Game listen address (default 127.0.0.1)
  --advertise-host <host>     Host name or IPv4 address players connect to (required)
  --control-port <n>          Allocation API port, on loopback (required)
  --control-token-file <f>    Operator token shared with the Host (required)
  --lighting <tag>            Lighting of every raid (default: picked by the map's weights)
  --version                   Show the ARES version and supported game build
```

Host reaches the allocation API with the same operator token (its `--session-control-url` and
`--session-control-token-file` options). Players never connect to it. The
[repository README](../README.md#run) shows a complete setup with ARES-Host.

Build, requirements, game updates and license: see the [repository README](../README.md).

## Operator console

This server has its own interactive console with readable logs and a fixed command row.
Use `help`, `status`, `clear`, or `stop`. See [console controls and output modes](../README.md#operator-consoles).
