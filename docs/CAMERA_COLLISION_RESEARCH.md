# Current game build

Inspection date: 2026-09-08 (Europe/Berlin)

Installed client:

- Steam app: `244850`
- Steam library: `D:\ProgramFiles\Steam`
- Install: `D:\ProgramFiles\Steam\steamapps\common\SpaceEngineers`
- Binary directory: `D:\ProgramFiles\Steam\steamapps\common\SpaceEngineers\Bin64`
- Steam manifest build ID: `24675677`
- Steam manifest state: fully installed (`StateFlags 4`)
- Game version: `01_210_014`, reported by the most recent client log at `C:\Users\artyo\AppData\Roaming\SpaceEngineers\SpaceEngineers_20260908_000107247.log`

The generic Windows file/product versions do not encode the Space Engineers release number, so the client log is the useful version source. The Steam manifest and hashes below identify the installed binary set.

| DLL | Assembly / file version | Last write (UTC) | SHA-256 |
|---|---|---|---|
| `Sandbox.Game.dll` | `0.1.1.0` / `0.1.1` | 2026-08-15 13:35:55Z | `DE04E9162BCF87FC07CEC377213D99533BCFE1C34C9E1E2231168144C9D67EDF` |
| `Sandbox.Common.dll` | `1.0.0.0` / `1.0.0.0` | 2026-08-15 13:35:57Z | `EAC978813C906C4C055EF8A9BF86BD71065ECE91FD3DA8866A3D810D81BD4D24` |
| `VRage.Game.dll` | `1.0.0.0` / `1.0.0.0` | 2026-08-15 13:35:55Z | `F0970C2B60335F7690E158A485CD528703717C8C5002F2713556E04F09C508E1` |
| `VRage.Library.dll` | `1.0.0.0` / `1.0.0.0` | 2026-08-15 13:35:55Z | `2757D7F23D41D90CD6AEE9E8BF966172916352D5FF2DF86ACAB0767E0A7C766D` |
| `VRage.Math.dll` | `1.0.0.0` / `1.0.0.0` | 2026-08-15 13:35:55Z | `953ED575327FFEC5A72BAF13C893843C26F7D248CD1ECE1372EC34D650618F1C` |
| `VRage.dll` | `1.0.0.0` / `1.0.0.0` | 2026-08-15 13:35:55Z | `EC2410B3D916ADBD7404C3082CCC25A70C8BAEC89BACB57001C67597068E24CA` |
| `HavokWrapper.dll` | `0.1.1.0` / `0.1.1` | 2026-08-15 13:35:57Z | `10FE39AC65933DB3B0C34D05C018B4AC4F906DBBBB3DCA2DB5F05E6DABA4D184` |
| `SpaceEngineers.Game.dll` | `1.0.0.0` / `1.0.0.0` | 2026-08-15 13:35:55Z | `6CEFEA4A73A5B806AF4E565BE5B41ED0B4199AF2FC5031C575C4BE7EB644C0C8` |

The camera implementation is principally in `Sandbox.Game.dll`. `VRage.dll` contains the free/fixed spectator base. `VRage.Game.dll` contains the official camera and physics ModAPI interfaces. `HavokWrapper.dll` supplies the collision shape and hit types used internally.

The assemblies were inspected with ILSpy command-line 9.1.0.7988. No game file was changed, copied into the deliverable, or redistributed.

# Vanilla camera architecture

`Sandbox.Engine.Utils.MyThirdPersonSpectator` is a public session component in `Sandbox.Game.dll`. Despite its name, it is the object that maintains the requested third-person boom and the collision-corrected eye position. `Sandbox.Engine.Utils.MySpectatorCameraController` is a different class: it implements the free/fixed/orbit spectator modes and does not provide third-person collision.

Requested-position path:

```text
Sandbox.Game.World.MySession.Update(VRage.Library.Utils.MyTimeSpan)
  -> MyThirdPersonSpectator.Static.Update()
     -> controlled entity GetHeadMatrix(includeY: true)
     -> m_target = head translation
     -> m_transformedLookAt = Transform(m_clampedlookAt, m_targetOrientation)
     -> m_desiredPosition = m_target + m_transformedLookAt
     -> HandleIntersection(controlledEntity, ref m_target)
```

The session calls `MyThirdPersonSpectator.Update()` whenever the active camera controller is null or is not in first person. Thus its state can continue updating while a normal spectator camera is active, but that state is not exposed through ModAPI.

For a cockpit, final application is:

```text
Sandbox.Game.Entities.MyCockpit.IMyCameraController.ControlCamera(MyCamera)
  -> MyCockpit.GetViewMatrix()
     -> MyThirdPersonSpectator.Static.GetViewMatrix()
        -> MatrixD.CreateLookAt(m_positionSafe, m_target, m_targetOrientation.Up)
  -> MyCamera.SetViewMatrix(...)
```

`MyCharacter.GetViewMatrix()` has the equivalent third-person branch. `GetViewMatrix()` is therefore the final internal read of the corrected position, and `MyCamera.SetViewMatrix(...)` applies it to the render camera.

# Collision algorithm

Complete collision path:

```text
MyThirdPersonSpectator.Update()
  -> MyThirdPersonSpectator.HandleIntersection(MyEntity, ref Vector3D)
     -> ComputeEntitySafeOBB(MyEntity)
     -> RaycastOccludingObjects(
          MyEntity,
          MyOrientedBoundingBoxD,
          ref Vector3D origin,
          ref Vector3D end,
          ref Vector3D safeStart,
          ref Vector3D outSafePosition)
        -> character-only MyPhysics.CastRay(...)
        -> MyPhysics.GetPenetrationsBox(...) near the cast origin
        -> MyGamePruningStructure.GetAllVoxelMapsInBox(...)
        -> new HkSphereShape(m_currentCameraRadius)
        -> MyPhysics.CastShapeReturnContactBodyDatas(...)
        -> filter hits and choose the nearest acceptable hit fraction
        -> optional MyPhysics.GetPenetrationsBox(...) near the result
        -> optional voxel MyPhysics.CastRay(...) safety test
     -> PerformZoomInOut(safeStart, outSafePosition)
     -> m_positionSafe
  -> MyThirdPersonSpectator.GetViewMatrix()
  -> controlling entity's IMyCameraController.ControlCamera(MyCamera)
  -> MyCamera.SetViewMatrix(...)
```

This is a combined solver, not a ray-only solver:

1. It builds a safe oriented bounding box around the controlled entity. For grid groups it merges mechanical-grid bounds and selected physical-grid bounds. It expands the resulting OBB half-extents by `2 * m_currentCameraRadius`. If the requested point remains inside, it pushes the point outward to the OBB boundary.
2. It performs a small box penetration test near the cast origin. The box half-extents are `m_currentCameraRadius * 0.02` on all axes, using collision layer 15.
3. It separately checks a point-sized AABB against voxel maps and asks each candidate voxel whether an AABB corner is inside.
4. It creates `new Havok.HkSphereShape(m_currentCameraRadius)` inside `RaycastOccludingObjects`, sweeps it toward the requested endpoint with `Sandbox.Engine.Physics.MyPhysics.CastShapeReturnContactBodyDatas`, then calls `shape.RemoveReference()`.
5. It rejects back-facing/irrelevant hits and filters the controlled entity, phantom bodies, handheld weapons, floating objects, debris, characters, safe zones, the same topmost entity, mechanically connected grids, and some included physical-group grids.
6. It uses the nearest acceptable sweep fraction to interpolate a safe sphere-center position. The sphere radius itself supplies the principal surface clearance.
7. It performs an additional tiny penetration check when the result lies inside the safe OBB. It also has character/voxel special cases and a forced-first-person/no-space state.
8. `PerformZoomInOut` applies immediate zoom-in and delayed/smoothed zoom-out behavior, producing `m_positionSafe`.

## Camera radius

The live `Update()` assigns the radius dynamically:

- Character: `0.125f` metres.
- `MyTerminalBlock`: `0.5f` metres.
- `MyCubeGrid`: `myCubeGrid.GridSize` (normally 0.5 m for a small grid or 2.5 m for a large grid).
- Initial/default field value: `0.125f`.

The class also declares `public const float DEFAULT_CAMERA_RADIUS_CUBEGRID_MUL = 1.5f`, but the installed class does not use that constant in its collision path. In particular, the current `Update()` assigns `myCubeGrid.GridSize`, not `GridSize * 1.5f`.

Maximum attempted camera boom length is 500 m. User third-person distance is clamped to 2.5–200 m.

# Relevant classes

“Whitelisted” below means a normal `Data/Scripts/<ModName>/` ModAPI assembly can reference the symbol under both the installed runtime whitelist and MDK2 mod analyzer 2.1.16. It does not mean programmable-block access.

| Class | Assembly | Method/member (exact current signature) | Access | Whitelisted | Useful |
|---|---|---|---|---|---|
| `Sandbox.Engine.Utils.MyThirdPersonSpectator` | `Sandbox.Game.dll` | `public static MyThirdPersonSpectator Static` | public static field | **No** | Would locate the live solver, but MDK01 prohibits it. |
| same | same | `public void Update()` | public instance | **No** | Computes requested and corrected state, but only for the actual controlled entity. |
| same | same | `private void HandleIntersection(MyEntity controlledEntity, ref Vector3D lastTargetPos)` | private instance | **No** | Collision orchestration; not callable even outside the whitelist without reflection/patching. |
| same | same | `private MyCameraRaycastResult RaycastOccludingObjects(MyEntity controlledEntity, MyOrientedBoundingBoxD entitySafeObb, ref Vector3D raycastOrigin, ref Vector3D raycastEnd, ref Vector3D raycastSafeCameraStart, ref Vector3D outSafePosition)` | private instance | **No** | Closest match to the requested reusable solver, but it is private, stateful, and controlled-entity-specific. |
| same | same | `public MatrixD GetViewMatrix()` | public instance | **No** | Returns a view built from private `m_positionSafe`; MDK01 prohibits it. |
| same | same | `public bool ResetViewerDistance(double? newDistance = null)` | public instance | **No** | Changes distance only, not an arbitrary target/end/radius. |
| same | same | `public double GetViewerDistance()` | public instance | **No** directly | Exposed indirectly as `IMySession.CameraTargetDistance`, which is requested distance rather than resolved distance. |
| `Sandbox.Engine.Physics.MyPhysics` | `Sandbox.Game.dll` | `public static bool CastShapeReturnContactBodyDatas(Vector3D to, HkShape shape, ref MatrixD transform, uint collisionFilter, float extraPenetration, List<MyPhysics.HitInfo> result, bool ignoreConvexShape = true)` | public static | **No** | The actual sphere sweep. The class, `HkShape`, `HkSphereShape`, and internal hit type are all prohibited by MDK2. |
| same | same | `public static void GetPenetrationsBox(ref Vector3 halfExtents, ref Vector3D translation, ref Quaternion rotation, List<HkBodyCollision> results, int filter)` | public static | **No** | Internal overlap test; Havok result type is also prohibited. |
| `VRage.Game.ModAPI.IMyPhysics` | `VRage.Game.dll` | `bool CastRay(Vector3D from, Vector3D to, out IHitInfo hitInfo, int raycastFilterLayer = 0)` | public interface instance | **Yes** | Official single-hit ray query. |
| same | same | `void CastRay(Vector3D from, Vector3D to, List<IHitInfo> toList, int raycastFilterLayer = 0)` | public interface instance | **Yes** | Official all-hits ray query; preferable when own-grid hits must be filtered. |
| same | same | `void CreateSpherePhysics(PhysicsSettings settings, float radius)` | public interface instance | **Yes** | Creates/assigns an entity physics body. It is not a sweep or overlap query and is not a practical camera solver. |
| `VRage.Game.ModAPI.IHitInfo` | `VRage.Game.dll` | `Position`, `HitEntity`, `Normal`, `Fraction` getters | public interface instance | **Yes** | Supplies public ray hit data and hit entity. |
| `VRage.Game.ModAPI.IMySession` | `VRage.Game.dll` | `void SetCameraController(MyCameraControllerEnum cameraControllerEnum, IMyEntity cameraEntity = null, Vector3D? position = null)` | public interface instance | **Yes** | Selects `SpectatorFixed`, third person, or the controlled entity camera. |
| same | same | `double CameraTargetDistance { get; set; }` | public interface property | **Yes** | Indirectly calls the vanilla distance getter/resetter; does not expose collision output. |
| `VRage.ModAPI.IMyCamera` | `VRage.Game.dll` | `Vector3D Position { get; }`, `MatrixD ViewMatrix { get; }`, `MatrixD WorldMatrix { get; }` | public interface getters | **Yes** | Reads the currently applied render camera only. |
| `VRage.MySpectator` | `VRage.dll` | `public static MySpectator Static`; `Position`; `Target`; `SetTarget(Vector3D, Vector3D?)`; `SetViewMatrix(MatrixD)` | public | **Yes** | Suitable for placing and aiming the final `SpectatorFixed` camera. Contains no collision solver. |
| `Sandbox.Engine.Utils.MySpectatorCameraController` | `Sandbox.Game.dll` | `public new static MySpectatorCameraController Static` | public static field | **No** | MDK2 permits the type name and `IsLightOn` only; `Static` and its other concrete members are prohibited. It has no third-person collision logic. |

## Whitelist verification

The installed runtime configuration was traced to `SpaceEngineers.Game.MySpaceGameDefaultIlChecker` in `SpaceEngineers.Game.dll`. It allows the official `Sandbox.ModAPI.*`, `VRage.Game.ModAPI.*`, and `VRage.ModAPI.*` namespaces. It explicitly allows all members of `VRage.MySpectator`, only the type plus `IsLightOn` for `MySpectatorCameraController`, and only `MyPhysicsHelper` and nested `MyPhysics.CollisionLayers` from the engine physics area. It does not allow `MyThirdPersonSpectator`, engine `MyPhysics`, or Havok query types.

An analyzer-only compile against the installed assemblies and `Mal.Mdk2.ModAnalyzers` 2.1.16 produced `MDK01` for:

- `MyThirdPersonSpectator`, `MyThirdPersonSpectator.Static`, `Update()`, and `GetViewMatrix()`.
- `MySpectatorCameraController.Static`.
- `HkShape`, `HkSphereShape`, and `HkBodyCollision`.
- `MyPhysics`, `MyPhysics.HitInfo`, `CastShapeReturnContactBodyDatas(...)`, and `GetPenetrationsBox(...)`.

The same probe produced no MDK diagnostic for `MyAPIGateway.Physics.CastRay(...)`, `MyAPIGateway.Session.SetCameraController(...)`, or the permitted `IsLightOn` member. This matches the installed runtime whitelist.

# Can a Workshop mod reuse it?

**NO.**

There is no public ModAPI function with semantics equivalent to:

```text
ResolveCameraCollision(targetPosition, desiredCameraPosition, radius)
```

The closest function is `MyThirdPersonSpectator.RaycastOccludingObjects(...)`, but it is private, depends on substantial private solver state, requires concrete game/Havok types, and is not whitelisted. The underlying public-CLR shape cast is also not ModAPI-whitelisted. Public CLR visibility alone does not make either route usable by a Workshop mod.

Reflection would not make this a clean Workshop API: the required reflection members/types are not generally whitelisted, it would still depend on private implementation details, and it violates the intended no-patch/no-internal-trick boundary of this investigation.

## Can vanilla third person be a hidden proxy?

Not cleanly.

A mod can use only this indirect sequence:

1. Save the current camera mode.
2. Call `IMySession.SetCameraController(MyCameraControllerEnum.ThirdPersonSpectator, controlledEntity)`.
3. Set `IMySession.CameraTargetDistance`.
4. Allow the game to update and apply that real camera.
5. Read the subsequently applied `IMySession.Camera.Position`.
6. Switch to `SpectatorFixed` and copy the position to `MySpectator.Static`.

This is not a general solver and is not hidden:

- It temporarily makes the controlled entity's normal third-person camera the actual camera controller.
- The result is unavailable through ModAPI until it has been applied to the main camera; the private `m_positionSafe` cannot be read.
- The desired direction comes from the controlled entity/head orientation. ModAPI exposes distance but no arbitrary third-person endpoint or collision radius.
- `SetCameraController` raises the game's camera-controller network event, so repeated proxy switches introduce multiplayer state traffic and side effects.
- A component cannot pass `IMySession.Camera` back into `IMyCameraController.ControlCamera`, because that method requires the concrete, non-whitelisted `MyCamera` type.
- Timing it around simulation/render updates would be version-sensitive and can produce a visible frame or stale camera data.

Therefore option B is unsuitable for a cinematic camera.

# Best implementation for our cinematic camera

**C. Reimplement the collision envelope using public physics API.**

The public API exposes raycasts but no sphere/convex sweep and no penetration query. A Workshop-only implementation can reproduce the small useful part at a high level:

```text
target and desired cinematic eye
  -> construct boom direction, distance, camera radius, and an orthonormal cross-section
  -> cast center plus radius-offset rays through MyAPIGateway.Physics.CastRay(..., List<IHitInfo>, ...)
  -> discard the controlled grid and its mechanical/physical group hits
  -> choose the smallest remaining IHitInfo.Fraction
  -> convert fraction to corrected boom distance and apply a small safety margin
  -> optionally delay/smooth outward motion, but move inward immediately
  -> place MySpectator.Static and use SpectatorFixed
```

Exact available query surface:

```csharp
void VRage.Game.ModAPI.IMyPhysics.CastRay(
    Vector3D from,
    Vector3D to,
    List<VRage.Game.ModAPI.IHitInfo> toList,
    int raycastFilterLayer = 0);

bool VRage.Game.ModAPI.IMyPhysics.CastRay(
    Vector3D from,
    Vector3D to,
    out VRage.Game.ModAPI.IHitInfo hitInfo,
    int raycastFilterLayer = 0);

int VRage.Game.ModAPI.IMyPhysics.GetCollisionLayer(string strLayer);
```

Call these through `Sandbox.ModAPI.MyAPIGateway.Physics` on the main simulation thread. `IHitInfo` provides `Fraction`, `Position`, `Normal`, and `HitEntity`. The all-hits overload is important because the public query cannot reproduce vanilla's Havok system-ID filter; the mod must ignore its controlled grid and connected grids after the query.

A center ray alone is not adequate. A small ring of parallel rays offset in the plane perpendicular to the boom is the minimum practical public approximation to a sphere sweep. More ring samples, adaptive sampling near a hit, and conservative clearance reduce edge/corner leakage at additional cost. This is still not geometrically identical to a swept sphere. No ray approximation was implemented as part of this research.

`IMyPhysics.CreateSpherePhysics(...)` is not a substitute. It assigns a sphere rigid body or detector to an entity and provides no swept fraction or nearest-contact result. Teleporting a phantom sphere along the boom would be discrete, stateful, callback-driven, and more intrusive than ray queries.

For final camera control, the permitted route is:

```csharp
MyAPIGateway.Session.SetCameraController(
    MyCameraControllerEnum.SpectatorFixed,
    null,
    resolvedPosition);
MySpectator.Static.Position = resolvedPosition;
MySpectator.Static.SetTarget(targetPosition, upVector);
```

Changing the camera controller does not relinquish the player's controlled ship, but the implementation should save and restore the prior entity camera mode and first/third-person state.

An exact vanilla-style sphere sweep from arbitrary endpoints would require option D, a client plugin with access to the internal engine physics/Havok types. It is not required if the Workshop ray-envelope approximation is acceptable.

# Proof-of-concept decision

No mod was created. The requested condition for creating it—direct Workshop access to the vanilla collision solver—was not met. Creating a ray approximation now would contradict the requested sequencing and would test a different algorithm.

# Risks

- **Game updates:** Private class names, fields, filters, radius policy, and Havok calls can change. The installed build result applies specifically to `01_210_014` / Steam build `24675677`.
- **Whitelist changes:** A future release could expose or remove APIs. Re-run the installed whitelist and MDK analyzer checks after updates.
- **Internal API changes:** A client plugin that calls `MyPhysics.CastShapeReturnContactBodyDatas` would be tightly coupled to method signatures, Havok reference ownership, filters, and cluster behavior.
- **Multiplayer:** Run camera logic only for the local human player. Avoid per-frame `SetCameraController` calls because they raise camera-controller network events. Do not change the controlled entity.
- **Dedicated server:** There is no local camera/player. The component should no-op when dedicated or when `LocalHumanPlayer`/`ControlledObject` is null.
- **Voxel collisions:** Public physics raycasts hit voxel physics, but sparse ray envelopes can miss terrain edges between samples. Vanilla also has explicit voxel safety checks that ModAPI does not expose directly in equivalent form.
- **Grid filtering:** Vanilla uses a Havok collision-system filter and detailed mechanical/physical-group rules. Workshop code must approximate this using hit entities and public grid-group relationships.
- **Static geometry:** Static grids and voxel physics are covered by public rays, but edge/corner coverage depends on the ray envelope.
- **Performance:** Physics casts must run on the main thread. Use a bounded number of rays, cache group membership, avoid allocations, and consider reducing query frequency or adapting sample count when unobstructed.
- **Camera jitter:** Move inward immediately on a nearer hit; delay and smooth only outward recovery. Otherwise alternating edge hits will visibly pump the camera.

# Conclusion

VANILLA COLLISION REUSE: NO

Best path:
C. Reimplement the camera collision envelope with whitelisted `IMyPhysics.CastRay` queries, then drive `MySpectator` in `SpectatorFixed` mode.

Reason:
The real solver and its sphere/penetration queries are private and/or MDK2-prohibited; ModAPI exposes no shape sweep.

Workshop-only possible:
YES — for a ray-envelope reimplementation, not for direct vanilla sphere-sweep reuse.

Needs client plugin:
NO — unless exact arbitrary-endpoint vanilla sphere-sweep behavior is mandatory.

Relevant callable method(s):
`MyAPIGateway.Physics.CastRay(...)`, `MyAPIGateway.Physics.GetCollisionLayer(...)`, `MyAPIGateway.Session.SetCameraController(...)`, `MySpectator.Static.Position`, and `MySpectator.Static.SetTarget(...)`.

Main limitation:
Workshop ModAPI has raycasts but no whitelisted sphere/shape sweep, penetration query, or access to `MyThirdPersonSpectator.m_positionSafe`.
