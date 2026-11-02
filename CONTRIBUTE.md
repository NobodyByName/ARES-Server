# Contributing to ARES-Server

Contributions from people and AI coding agents are welcome. This file is the entry point for both;
every rule below applies to both. Read the [README](README.md) first.

## Client integrity rules

ARES works with the unmodified game. Contributions that break these rules are not accepted:

- Never patch, inject into or hook the game client, its launcher, game content, DLLs or process
  memory. Do not call native functions in the client or change its live objects. Do not substitute
  modified Wine/Proton DLLs.
- Nothing players run may read or write the game's process memory, attach a debugger, inject code
  or hook the client.
- Never bypass certificate pinning, signature verification, authentication or anti-cheat.
- Connect the game through its existing configuration options. Never substitute a client listen
  host or a map preview for a real server, and never advertise a game server that is not ready.
- Do not claim that something is safe from bans without evidence.
- Do not add game files, extracted assets, memory dumps, packet captures or other raw game data.
  Decoded game structures and values belong in ARES-Server's Structs component as typed C#.

## Architecture boundaries

| Component | May depend on | Rules |
| --- | --- | --- |
| Structs | nothing; no third-party packages, no ASP.NET or `Microsoft.Extensions` | Decoded game structures and data only, nothing ARES-specific: other community projects use it too. Everything is a C# type (structs, records, classes, enums), never JSON, text or binary blobs. Members whose meaning is not understood yet are named `Unknown…`. Only the supported game build. |
| Common | Structs | Logic that Host and Raid both use (identity tokens, the allocation contract, shared game rules and helpers). Code only one of them needs belongs in that component. |
| Host | Structs, Common | Never references Raid; talks to it only through the allocation contract in Common. Secrets come from files, never from the command line, and are never logged. |
| Raid | Structs, Common | Never references Host. Each raid owns its state and configuration; no mutable static state. |

Game names, ids and values live in Structs; the other components reference them instead of
repeating them as literals. The build rejects references outside the "May depend on" column, and
each component's `ArchitectureTests` check the compiled assemblies' references.

## Workflow

1. For larger changes, open an issue first to agree on the approach.
2. Fork the repository and work on a branch with focused commits.
3. Run the checks below; all of them must pass.
4. Open a pull request that says what changed and how you tested it. For tests with the game, give
   the game build, the platform and the flow you followed.

Tests with the game start from a fresh launch at the normal main menu and use normal Dam
matchmaking. Direct-start URLs, scripted travel, map previews and client listen hosts do not count.

## Checks

```sh
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Code style is defined in `.editorconfig` (dotnet/runtime conventions; explicit types instead of
`var`) and enforced at build time; warnings are errors. Fix the code instead of suppressing
warnings. `dotnet format` applies most style fixes.

## Reporting issues

Include:

- the ARES components and their versions (or commit): `--version` prints them for ARES-Host and
  ARES-Raid, and ARES-Launcher shows its version and the game's Steam build id below the server list;
- your platform (on Linux, also the Proton version);
- how the servers run: same PC, local network or internet;
- the steps from game launch, what happened and what you expected;
- relevant ARES-Host and ARES-Raid logs.

Never post operator tokens, identity keys or other secret files, and check logs for addresses you
do not want to share. Report security problems privately through the repository's security
advisories ("Report a vulnerability"), not in a public issue.

## License

By contributing, you agree that your contributions are licensed under the GNU General Public
License, version 3 or (at your option) any later version (`GPL-3.0-or-later`), like the rest of
this repository.
