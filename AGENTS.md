# SECinematicCamera contributor instructions

- This is an MDK2 Space Engineers Workshop mod. Build with `dotnet build .\SECinematicCamera.sln -c Release` from the repository root.
- Treat the currently installed Space Engineers assemblies as authoritative. Do not use the old Keen source repository as the project base.
- Never commit or redistribute Space Engineers DLLs, local game data, build output, MDK caches, decompiler output, or `mdk.local.ini`.
- Do not modify files under the Space Engineers installation.
- Do not add Harmony, DLL patches, unsafe memory access, or a client plugin.
- V0 has no collision system. Do not add ray-fan or other collision approximations unless a later task explicitly authorizes it.
- Keep all Space Engineers camera-controller and spectator calls inside `src/Camera/SECameraAdapter.cs`.
- Keep lifecycle and emergency shutdown centralized. Every unexpected failure while cinematic mode is active must route through the idempotent `EmergencyStop(reason)` path.
- Do not fight camera ownership. If another system takes the camera, disable cinematic mode and restore only when safe.
- Shot changes are hard cuts. Never interpolate between different shot poses; smoothing is allowed only while following within a single shot.
- Camera work is client-local. Dedicated servers and sessions without a local player must no-op safely.
- Keep components small and preserve the directory architecture described in `README.md`.
- New Space Engineers API usage must pass the MDK2 ModAPI whitelist analyzer with zero warnings or errors.
- Verify a Release build and inspect the deployed local mod tree before committing changes.
- Keep commits small and meaningful. Do not rewrite or discard unrelated user changes.
