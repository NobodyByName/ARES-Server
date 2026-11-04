# ARES-Server

Independent replacement servers for ARC Raiders: the backend a stock game client signs in to, and
the game-session server that runs its raids. Players connect with
[ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher) (a separate repository) and their own, unmodified game.

> **Unofficial project.** ARES is independent and is not affiliated with, endorsed by or sponsored
> by Embark Studios. ARC Raiders and all related names and trademarks belong to their respective
> owners. ARES works only with an installed, unmodified copy of the game.

## Components

| Folder | What it is | May depend on |
| --- | --- | --- |
| [Structs](Structs/README.md) | Library: decoded game structures, wire formats and data | nothing |
| [Common](Common/README.md) | Library: logic shared by Host and Raid | Structs |
| [Host](Host/README.md) | Backend (`ARES-Host`): sign-in, main menu, party, matchmaking, raid assignment | Structs, Common |
| [Raid](Raid/README.md) | Game-session server (`ARES-Raid`) that runs the raid | Structs, Common |

The build enforces these boundaries: a project that references a component outside its list fails
to build, Structs may not use third-party packages, and each component's `ArchitectureTests` check
the compiled assemblies' references. Host and Raid never reference each other; they talk only
through the allocation contract in Common.

## Status

A stock client goes from a fresh launch through sign-in, the main menu and normal Dam matchmaking
into a raid hosted by ARES-Raid: Dam loads with its lighting, the player gets the default Raider,
moves (walk, sprint, crouch, slide, jump), climbs ladders and uses door buttons, then leaves back to
the main menu.

Host persists the local account's stash and selected loadout. The standard player containers and
rubber duck appear in the raid inventory, and whole-stack drag moves between compatible slots are
validated by Raid. Inventory and map screens show their shared menu frame.
Placement changes remain inside the active raid; post-raid inventory settlement
is not implemented.

Limitations:

- One player and one raid at a time; Dam only.
- Identity is synthetic: every player uses one local account, with no real sign-in or separate
  player accounts and progression yet.
- Movement is trusted from the client; the server does not check it yet.
- No combat, loot, death or match results yet.
- Equipment, other item types and inventory operations beyond whole-stack swaps are not implemented yet.
- Plain HTTP between the game and ARES-Host; connection trust is not verified.
- Tested on one PC only (loopback or its own LAN address). Other machines and the internet have not
  been tested yet.

## Requirements

- The .NET 10 SDK (version in `global.json`) to build, test and run from source.
- No game installation is needed to build or test. Players need the supported game build.
- Developed and tested on Linux. Every platform ARC Raiders supports is a goal; Windows has not been
  tested yet.

## Build and test

```sh
dotnet build                          # build everything (warnings are errors)
dotnet test                           # run the tests
dotnet format --verify-no-changes     # check code style
```

## Run

ARES-Host and ARES-Raid run side by side. They share a private operator token for the Raid's
allocation API (on loopback); ARES-Host also signs player identities with a private key. Neither
secret ever goes to players.

```sh
# Secrets: an operator token (at least 32 ASCII characters) and an identity key (32-4096 bytes).
openssl rand -hex 32 > operator-token
openssl rand 32 > identity-key

dotnet run --project Raid/src/Ares.Raid -- --advertise-host 127.0.0.1 --control-port 17778 \
  --control-token-file operator-token

# Create your editable Host configuration from the documented template.
cp Host/ares-host.conf.dist ares-host.conf
# Set these values in ares-host.conf:
#   session-control-url = http://127.0.0.1:17778
#   session-control-token-file = operator-token
#   auth-key-file = identity-key

dotnet run --project Host/src/Ares.Host
```

ARES-Host loads `ares-host.conf` from the working directory and creates it on first launch if
missing. Edit it and restart Host to apply changes. Use `--config <path>` to select an existing
file; existing arguments still override its settings. See [Host configuration](Host/README.md#configuration).
Host also needs `archive-keys.txt`, the game's archive keys, beside the configuration file;
see [Archive keys](#archive-keys).

Players then add the server `127.0.0.1` in [ARES-Launcher](https://github.com/NobodyByName/ARES-Launcher) (port 18080 is
the default). To serve other machines, set Host's `bind = <address>`, `allow-network-clients = true`
and `public-origin = http://<address>:18080`, and start ARES-Raid with `--bind <address>
--advertise-host <address>`. All settings: [Host](Host/README.md#configuration), [Raid](Raid/README.md#options).

## Operator consoles

Host and Raid each have their own console. In a terminal, logs appear above a fixed command row;
Host shows matchmaking activity and Raid shows the player's loading phase. Events have timestamps
and readable field names; warnings and errors stand out in color.

Type `help` for commands, `status` for configuration and the latest reported activity, `clear` to
clear the visible log history, or `stop` to shut down that server gracefully. `Ctrl+C` also stops
it. Commands act only on the server whose console you are using. Gameplay commands are not
implemented yet.

Use Left/Right, Ctrl+Home/Ctrl+End, Backspace/Delete to edit; Up/Down recall commands and Escape
clears the input. Home scrolls up one page through the latest 1,000 log events; End scrolls down
one page. New events continue to arrive while browsing, and scrolling to the bottom resumes live
output. The input row stays available while browsing logs.

Both servers accept `--console <mode>`:

| Mode | Behavior |
| --- | --- |
| `auto` (default) | Interactive in a terminal; readable line output when streams are redirected. |
| `interactive` | Requests the terminal interface; falls back to readable output if no terminal is available. |
| `plain` | Readable line output without cursor control or command input. |
| `json` | One JSON object per event, suitable for log collectors and files; no command input. |

Use `--log-file raid.log` to append structured JSON events to a file while keeping the console
interactive. The file is flushed after each event; parent directories are created as needed.
Alternatively, use `--console json > raid.log` for redirected output. In Rider, choose
**Terminal mode → Enable pseudoterminal** for each server's run configuration. The terminal interface
needs terminal input and output; plain stream redirection cannot provide a fixed input row.
Disable Rider's **Save console output to file** and input redirection when using pseudoterminal
mode; use `--log-file` to save events independently of the terminal.

## Archive keys

ARES-Launcher builds each player's connection config from their own game files, which are
encrypted. Reading them needs two keys of the game: the pak key and the container key. ARES does
not include them. The operator puts them in `archive-keys.txt` beside `ares-host.conf`
(`archive-keys-file` names another path), and ARES-Host hands them to launchers at
`GET /ares/v1/archive-keys` (no sign-in, like the status page). ARES-Host does not start without
them. `archive-keys.txt` is git-ignored; never commit it.

Both keys are published by community tools for Unreal Engine game files:

- **Container key:** the eight `ArcRaidersKey` words in
  [CUE4Parse](https://github.com/FabianFG/CUE4Parse), file
  `CUE4Parse/GameTypes/Theia/Encryption/TheiaDecryptor.cs`.
- **Pak key:** the ARC Raiders AES key used with FModel, for example in
  [FModelMats](https://github.com/MeyouVA/FModelMats), file
  `CUE4Parse/CUE4Parse/GameTypes/ArcRaiders/Encryption/Aes/ArcRaidersAes.cs`.

Write them into `archive-keys.txt` (`0x` prefixes are optional, `#` starts a comment):

```
pak-key = 0x<64 hex digits>
container-key = 0x<word>, 0x<word>, 0x<word>, 0x<word>, 0x<word>, 0x<word>, 0x<word>, 0x<word>
```

At startup ARES-Host compares the keys with the fingerprint of the supported build
(`GameBuild.ArchiveKeysSha256`, a SHA-256 hash that does not reveal them) and refuses keys that do
not match.

## Supported game build and updates

ARES supports one game build at a time: **Steam build 26159202** (`GameBuild.SteamBuildId` in
Structs). Steam updates ARC Raiders automatically. After an update:

- ARES-Raid refuses players whose game build differs from the supported one (connection handshake).
- ARES-Launcher warns when a server's game build differs from the installed one.
- Players on the new build cannot join until the server runs an ARES release for it.

Supporting a new build: update `GameBuild` (Steam build id, network compatibility identity and
checksum, and the archive keys fingerprint if the keys changed) and every decoded value in Structs
that changed, build and test, then repeat the full
flow with the game: fresh launch → main menu → Dam matchmaking → raid. Host and Raid take the build
id and network checksum from `GameBuild`. Older builds live in the Git history.

## Using Structs in other projects

Structs depends on nothing else and contains nothing ARES-specific, so other ARC Raiders community
projects can use it: reference its projects from a checkout of this repository, for example as a Git
submodule limited to `Structs/` with a sparse checkout.

## Versions

Versions are release dates with a counter: `YYYYMMDDNN`, for example `2026110201` for the first
release on 2 November 2026 and `2026110202` for the second. The version is set in one place,
`AresVersion` in `Directory.Build.props`. It is the only version shown to people. The .NET assembly
and file versions stay `1.0.0.0`, because .NET limits each version part to 65534.

## Contributing

See [CONTRIBUTE.md](CONTRIBUTE.md).

## License

ARES contributions in this repository are licensed under the GNU General Public License,
version 3 or (at your option) any later version (`GPL-3.0-or-later`). See [LICENSE](LICENSE).
Third-party material retains its applicable licenses and notices.
