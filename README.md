# SECinematicCamera

`SECinematicCamera` is a client-side Space Engineers Workshop mod that provides a GTA-style automatic cinematic camera for the locally controlled ship or rover.

V0 deliberately has no collision solver. It uses gravity-relative high camera placements and hard cuts between shots.

## Prerequisites

- Space Engineers client `01_210_014` (Steam build `24675677` when this project was bootstrapped).
- Space Engineers `Bin64`: `D:\ProgramFiles\Steam\steamapps\common\SpaceEngineers\Bin64` on the bootstrap machine.
- .NET SDK 9.0 or later. The bootstrap machine uses 9.0.317.
- MDK2 `mdk2mod` template and packages.
- Visual Studio 2022, Rider, VS Code, or the `dotnet` CLI. Visual Studio's .NET desktop development workload is recommended.
- Git.

Machine-specific Space Engineers paths belong in the ignored `mdk.local.ini`; never commit them or game DLLs.

## Build and deploy

From the repository root:

```powershell
dotnet build .\SECinematicCamera.sln -c Release
```

MDK2 resolves references from the current installed Space Engineers client, runs the ModAPI whitelist analyzer, compiles the mod, and deploys it to:

```text
C:\Users\artyo\AppData\Roaming\SpaceEngineers\Mods\SECinematicCamera
```

The local MDK2 configuration used on the bootstrap machine is:

```ini
[mdk]
output=auto
binarypath=auto
```

The initial clean-template build successfully resolved `D:\ProgramFiles\Steam\steamapps\common\SpaceEngineers\Bin64` and deployed the local mod.

## Controls

- `F9`: toggle cinematic mode.
- `/cine`: toggle cinematic mode.
- `/cine on` / `/cine off`: explicitly enable or disable it.
- `/cine next`: hard-cut to another suitable automatic shot.
- `/cine auto on|off`: enable or disable automatic shot changes.
- `/cine cam <rear|sideleft|sideright|front|overhead|flyby|1..6>`: manually select a shot.
- `/cine set <duration|distance|height|side|smoothing|lookahead> <value>`: tune runtime parameters.
- `/cine status`, `/cine get`, `/cine reset`, `/cine debug on|off`, `/cine help`.

Handled `/cine` commands remain local and are not sent to chat or the server.

## V0 behavior

- Targets only the locally controlled ship-controller/grid.
- Switches the camera to `SpectatorFixed` without releasing ship control.
- Restores the prior entity camera on exit or emergency shutdown.
- Uses hard cuts between shots; smoothing applies only while following within one shot.
- Uses natural gravity for stable Up on planets and grid orientation as a zero-gravity fallback.
- Stops safely if the target, player, controlled entity, or expected camera controller becomes invalid.

## Known V0 limitations

- No camera collision or occlusion handling.
- High shots can still clip into very large structures, cliffs, terrain, or grids.
- Runtime tuning is not persisted.
- Only the local controlled grid is supported.
- In-game validation is required after each client update.

## Architecture

- `src/Session`: lifecycle, update boundary, activation, and emergency shutdown.
- `src/Input`: hotkey and local chat command handling.
- `src/Target`: locally controlled ship/grid resolution and target snapshot.
- `src/Director`: shot selection, history, continuity, and timing.
- `src/Camera`: shot solving, per-shot following, smoothing, and isolated Space Engineers camera access.
- `src/Safety`: centralized safety checks and emergency-stop policy.
- `src/Config`: defaults and runtime tuning.
- `src/Debug`: non-spammy local diagnostics.

See [camera collision research](docs/CAMERA_COLLISION_RESEARCH.md) for the installed-client API investigation. Collision is intentionally outside V0.
