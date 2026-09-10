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

### Verified bootstrap machine

Checked on 2026-09-08:

- Visual Studio Community 2022 17.14.35 and Visual Studio Build Tools 2022 17.14.39 are installed.
- The aggregate `Microsoft.VisualStudio.Workload.ManagedDesktop` workload is not installed. The individual .NET Framework 4.8 SDK, .NET Framework 4.8 targeting pack, Roslyn compiler, and MSBuild components required by this project are installed; a clean MDK2 build proves no additional Visual Studio component is currently required.
- .NET SDK 9.0.317 is installed.
- Git 2.50.1.windows.1 is installed.
- `Mal.Mdk2.ScriptTemplates` 2.2.66 is installed and provides `mdk2mod`.
- The project uses `Mal.Mdk2.ModAnalyzers` 2.1.16, `Mal.Mdk2.ModPackager` 2.2.11, and `Mal.Mdk2.References` 2.2.9.
- The current Space Engineers client is `01_210_014` (Steam build `24675677`) at `D:\ProgramFiles\Steam\steamapps\common\SpaceEngineers`.

No game DLL was copied or manually referenced. MDK2 resolves references from the installed client.

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

The clean bootstrap and completed V0 builds both successfully resolved `D:\ProgramFiles\Steam\steamapps\common\SpaceEngineers\Bin64`. The completed Release build reports 0 warnings and 0 errors, including no MDK2 ModAPI whitelist analyzer diagnostics, and packs/deploys the source mod to the path above.

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

Safe tuning ranges are: duration 1–60 seconds, distance 0.1–20, height 0.1–10, side 0–10, smoothing 0.01–1, and lookahead 0–5 seconds. Runtime changes are not persisted. Selecting a manual camera disables automatic changes until `/cine auto on` is used.

`distance` is the overall vehicle-size-relative shot scale; `height` and `side` are ratios of that distance. The compiled defaults are distance `0.75`, height `1.2`, side `1.1`, smoothing `0.16`, and lookahead `0.05`. Look-ahead is capped to 15% of the vehicle scale so speed alone cannot move the ship near the edge of the frame.

## V0 behavior

- Targets only the locally controlled ship-controller/grid.
- Switches the camera to `SpectatorFixed` without releasing ship control.
- Explicitly sets spectator movement to `None` before and after activation and before every pose write, preventing ship-control input from also flying the spectator camera.
- Writes an absolute world-space spectator position and target every cinematic update; it never accumulates camera movement deltas.
- Compensates target translation before smoothing, so acceleration does not create a spring-like camera lag. Position smoothing affects the camera's relative shot offset instead of trailing the grid in world space.
- Separates the follow anchor from the point of interest: profiles can favor the nose, upper structure, cockpit, or bounded motion lead instead of always aiming at grid center.
- Composes the point of interest away from screen center using profile-specific thirds/lead-room rules.
- Uses the grid's oriented local bounding box and current camera projection to keep ordinary tracking shots inside 5% screen margins. This framing correction is not collision handling.
- Restores the prior entity camera on exit or emergency shutdown.
- Uses hard cuts between shots; smoothing applies only while following within one shot.
- Uses natural gravity for stable Up on planets and grid orientation as a zero-gravity fallback.
- Provides RearHighThreeQuarter, SideHighLeft, SideHighRight, FrontHighThreeQuarter, OverheadDrift, and WorldLockedFlyBy profiles.
- Selects automatic shots by weighted randomness using duration, recent-shot history, speed, angular motion, and side continuity. It never cuts directly between mirrored strong side shots.
- Stops safely if the target, player, controlled entity, or expected camera controller becomes invalid.
- Routes shutdown, camera takeover, invalid targets, and update/command exceptions through one idempotent `EmergencyStop(reason)` path.
- Debug mode reports changes immediately and emits a non-spammy status line every five seconds.

The installed `MySpectator` API in client 1.210.014 does not expose `DisableRotation()` or a matching restoration method. Mouse rotation is therefore neutralized by `SpectatorCameraMovement = None` plus the authoritative absolute `Position` and `SetTarget` write each update. Because no separate rotation-disable state is changed, there is no hidden rotation flag to restore on shutdown.

## Known V0 limitations

- No camera collision or occlusion handling.
- High shots can still clip into very large structures, cliffs, terrain, or grids.
- Runtime tuning is not persisted.
- Only the local controlled grid is supported.
- In-game validation is required after each client update.

## Manual in-game verification still required

The build, analyzer, packaging, and local deployment are automated and verified. A live game session is still required to confirm:

1. Add `SECinematicCamera` to a test world's mod list and load the world.
2. Enter a ship or rover controller and press F9.
3. Confirm the initial rear-high view, continued steering/thrust input, and F9 restoration of both first- and third-person views.
4. Exercise each `/cine cam` profile, `/cine next`, automatic cuts, and the tuning commands.
5. Trigger safe exits by leaving/changing the controlled seat and by manually taking camera control; confirm one shutdown message and no repeated camera fighting.

## Architecture

- `src/Session`: lifecycle, update boundary, activation, and emergency shutdown.
- `src/Input`: hotkey and local chat command handling.
- `src/Target`: locally controlled ship/grid resolution, oriented bounds, and semantic framing anchors.
- `src/Director`: shot selection, history, continuity, and timing.
- `src/Camera`: shot solving, screen-space composition, per-shot following, smoothing, and isolated Space Engineers camera access.
- `src/Safety`: centralized safety checks and emergency-stop policy.
- `src/Config`: defaults and runtime tuning.
- `src/Debug`: non-spammy local diagnostics.

See [camera collision research](docs/CAMERA_COLLISION_RESEARCH.md) for the installed-client API investigation. Collision is intentionally outside V0.

Use the [V0 in-game test checklist](docs/INGAME_TEST_CHECKLIST.md) for structured manual validation.
