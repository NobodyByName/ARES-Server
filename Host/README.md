# Host

ARES backend (`ARES-Host`): synthetic identity, accounts, party, matchmaking and raid assignment.

**Depends on:** Structs, Common.

## Status

Answers the backend requests a stock client makes from sign-in through the main menu and Dam
matchmaking, then assigns the player to a raid through the Raid allocation API. Playing a raid also
needs ARES-Raid.

Limitations:

- Identity is synthetic: every player uses one local account. There is no real sign-in and no
  per-player progress or inventory yet.
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
