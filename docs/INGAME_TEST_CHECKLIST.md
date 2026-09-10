# SECinematicCamera V0 in-game test checklist

Use a disposable local test world. Start with a simple rover or small ship that has a cockpit, working propulsion, and enough open space to move safely.

Record failures with the test number, what happened, the expected result, and the relevant lines from `SpaceEngineers.log`.

## 1. Load and basic readiness

- [ ] Add `SECinematicCamera` to the test world's active mod list.
- [ ] Load the world without a mod compile error or crash.
- [ ] Confirm there are no `SECinematicCamera` script errors in `SpaceEngineers.log`.
- [ ] While on foot, press F9. Expected: cinematic mode does not start and a concise message says to control a ship or rover first.
- [ ] Enter a cockpit or remote control attached to a valid grid.
- [ ] Run `/cine status`. Expected: `OFF`, with the current automatic-mode state.
- [ ] Run `/cine get`. Expected: the compiled runtime settings are shown.

## 2. First milestone: activation and restoration

- [ ] Begin in first-person cockpit view and note the exact view.
- [ ] Press F9. Expected: cinematic mode turns ON in `RearHighThreeQuarter` and names the controlled grid.
- [ ] While cinematic mode is active, steer, accelerate, brake, roll, and use normal ship/rover controls. Expected: the vehicle remains fully controllable.
- [ ] Confirm the camera follows smoothly while staying in the same rear-high shot.
- [ ] Press F9 again. Expected: cinematic mode turns OFF and restores first-person cockpit view.
- [ ] Repeat from third-person view. Expected: F9 exit restores third-person view.
- [ ] Activate from first person and press V. Expected: cinematic mode exits to a normal controlled-ship view, not SpectatorFixed.
- [ ] Repeat from third person and press V. Expected: a normal first/third-person ship camera is active and V continues behaving like the vanilla view key.
- [ ] While active, press Ctrl+F9 repeatedly. Expected: each press hard-cuts to the next manual profile and automatic selection turns OFF; no Blueprint screen opens.
- [ ] If spectator mode is available, activate from a spectator view, then exit. Expected: prior spectator position, aim/roll, and movement mode are restored as closely as possible.
- [ ] Toggle ON/OFF rapidly several times. Expected: no stuck camera, repeated exception, or loss of vehicle control.

## 3. Spectator input-isolation regression

- [ ] Run `/cine debug on`, enter a ship/rover controller, and activate cinematic mode.
- [ ] Confirm the activation log shows the spectator movement mode before activation and `None` while active.
- [ ] Hold Space continuously for 3 seconds. Expected: the ship rises, the camera maintains its intended relation to the ship, and there is no additional spectator-camera drift.
- [ ] Inspect the temporary Space diagnostics. Expected: they show camera position before/after the authoritative write, controlled-grid position, spectator movement `None`, and `pose rewritten True`.
- [ ] Repeat with C. Expected: the ship moves downward while the camera only follows the solved cinematic pose.
- [ ] Repeat with W, A, S, and D individually. Expected: ship input works and does not independently translate the spectator.
- [ ] Move the mouse enough to command ship gyros. Expected: the ship rotates; mouse input does not leave the cinematic camera in a user-rotated pose.
- [ ] Test both roll directions. Expected: the ship rolls; the camera continues using its gravity-relative or grid-relative solved orientation.
- [ ] Combine translation, mouse/gyro, and roll controls for at least 15 seconds. Expected: no accumulating camera offset or rotation.
- [ ] Accelerate from 0 to maximum speed in a straight line. Expected: the camera does not spring behind and the ship remains framed near its intended screen position.
- [ ] Release thrust at maximum speed and decelerate. Expected: there is no large catch-up movement from the camera.
- [ ] Turn cinematic mode OFF. Expected: the prior camera controller and the spectator movement mode captured at activation are restored.
- [ ] Repeat activation from each available spectator movement mode. Expected: cinematic mode always uses `None`, and shutdown restores the prior mode.

## 4. Chat command handling

- [ ] Run `/cine`. Expected: it toggles cinematic mode.
- [ ] Run `/cine on` twice. Expected: cinematic mode remains ON without duplicating state.
- [ ] Run `/cine off` twice. Expected: cinematic mode remains safely OFF.
- [ ] Confirm handled `/cine` commands do not appear in multiplayer chat and are not sent to the server/other players.
- [ ] Run `/cine help`. Expected: concise command usage appears.
- [ ] Run an invalid command such as `/cine banana`. Expected: concise error and `/cine help` guidance, with no exception.
- [ ] Run malformed commands such as `/cine cam`, `/cine auto maybe`, and `/cine set distance nope`. Expected: concise usage messages and no state corruption.

## 5. Manual shot profiles

Test these while stationary, moving forward, reversing, and turning:

- [ ] `/cine cam rear` and `/cine cam 1`: RearHighThreeQuarter.
- [ ] `/cine cam sideleft` and `/cine cam 2`: SideHighLeft.
- [ ] `/cine cam sideright` and `/cine cam 3`: SideHighRight.
- [ ] `/cine cam front` and `/cine cam 4`: FrontHighThreeQuarter.
- [ ] `/cine cam overhead` and `/cine cam 5`: OverheadDrift.
- [ ] `/cine cam flyby` and `/cine cam 6`: WorldLockedFlyBy.
- [ ] `/cine cam frontmount` and `/cine cam 7`: FrontMountedClose.
- [ ] `/cine cam rearmount` and `/cine cam 8`: RearMountedForward.
- [ ] `/cine cam distant` and `/cine cam 9`: DistantTopSide.
- [ ] Confirm every manual selection immediately hard-cuts to the new pose—there must be no travel/interpolation between shots.
- [ ] Confirm position and aim follow smoothly after the cut for all tracking shots.
- [ ] For WorldLockedFlyBy, confirm the camera is initially placed approximately 60–100 m away, stays fixed in world space, and pans smoothly toward the moving vehicle. Small left/right course corrections must not flip the composition or jerk the pan.
- [ ] With auto ON, fly-by should cut after the receding grid reaches approximately 700 m. With manual fly-by, it should hard-cut to a newly placed fixed camera instead.
- [ ] Confirm front mount lasts about 2 seconds and rear mount about 2.5 seconds in automatic mode.
- [ ] Confirm OverheadDrift lasts about 2.5 seconds in automatic mode.
- [ ] Confirm selecting a manual shot turns automatic changes OFF.
- [ ] Confirm orbit/overhead shots remain high relative to natural gravity. The two close mounted shots may sit close to the hull, but there should be no wheel-height or ground-level view.

### Composition checks

- [ ] Run `/cine reset` and `/cine get`. Expected: the default distance is `0.75`.
- [ ] In RearHighThreeQuarter, confirm the ship is intentionally offset from exact screen center and the view favors its forward/upper detail.
- [ ] In FrontHighThreeQuarter, confirm the focus favors the nose/cockpit region rather than the geometric center.
- [ ] In SideHighLeft and SideHighRight, fly forward and backward. Expected: the camera leaves visible lead room in the direction of screen-space travel.
- [ ] Confirm the nine profiles do not all place the ship at the same screen coordinate.
- [ ] Rotate a long, wide, and tall grid. Expected: ordinary tracking shots keep the oriented grid bounds inside approximately 5% screen margins by moving outward when necessary.
- [ ] Confirm bounds correction never produces a smooth travel path during a shot cut; manual and automatic shot changes remain hard cuts.

## 6. Automatic director and hard cuts

- [ ] After `/cine reset`, confirm the ordinary base interval is about 10 seconds, not 5 seconds. Individual profile durations intentionally vary.
- [ ] Run `/cine auto on` and `/cine set duration 3` only when shortening the test cycle is useful.
- [ ] Observe at least ten automatic cuts at speed. Expected: rear/left/right/front orbit views never follow one another as a four-sided rotation.
- [ ] Confirm a rear orbit view does not cut directly to the rear-mounted view, and a front orbit view does not cut directly to the front-mounted view.
- [ ] Confirm fly-by, mounted, overhead, and distant establishing coverage appears between orbit views.
- [ ] Drive continuously for at least two minutes. Expected: multiple shot types are selected and the order is not a fixed repeating sequence.
- [ ] Confirm every automatic shot change is a hard cut with no interpolated path.
- [ ] Confirm the camera still follows smoothly between cuts.
- [ ] Confirm the director does not repeatedly select the same shot.
- [ ] Confirm SideHighLeft never cuts directly to SideHighRight, and SideHighRight never cuts directly to SideHighLeft.
- [ ] Drive quickly in a straight line. Expected: side and fly-by shots occur occasionally.
- [ ] Turn or rotate strongly. Expected: unsuitable side shots are less common and OverheadDrift becomes more likely.
- [ ] Stop or move very slowly. Expected: WorldLockedFlyBy becomes uncommon; RearHighThreeQuarter remains a safe option.
- [ ] Run `/cine next` repeatedly. Expected: each command immediately hard-cuts to another eligible shot while preserving the side-continuity rule.

## 7. Planet and zero-gravity orientation

- [ ] On a planet, drive across slopes and uneven ground. Expected: camera Up remains aligned with natural gravity rather than rolling with the rover.
- [ ] On a planet, point a ship steeply upward/downward. Expected: camera orientation remains stable and does not flip or produce an invalid view.
- [ ] In zero gravity, rotate the ship through several orientations. Expected: the occupied cockpit's orientation defines forward/up, even when the cockpit is mounted sideways relative to the grid; the camera does not flip or disappear.
- [ ] Repeat every manual profile in both natural gravity and zero gravity.

## 8. Runtime tuning

- [ ] Test `/cine set duration 5.0`; verify `/cine get` reports the change and automatic timing follows it.
- [ ] Test `/cine set distance 0.75`, then `0.5`; expected: the entire camera rig moves closer, including its height and side offsets, without a shot transition.
- [ ] Test `/cine set height 1.2`; expected: camera height changes.
- [ ] Test `/cine set side 1.1`; expected: three-quarter/side bias changes.
- [ ] Test `/cine set smoothing 0.05`, then `0.5`; expected: within-shot following changes from slower to faster without smoothing between cuts.
- [ ] Test `/cine set lookahead 0`, then `1`; expected: aim lead visibly changes while moving but remains capped so the ship stays safely framed at high speed.
- [ ] Test values just inside and outside every documented safe range. Expected: valid values apply; invalid values are rejected cleanly.
- [ ] Run `/cine reset`. Expected: all values return to compiled defaults, automatic mode turns ON, and debug mode turns OFF.
- [ ] Reload the world after changing values. Expected: V0 runtime tuning is not persisted.

## 9. Camera weight, acceleration lag, and shake

- [ ] Select `/cine cam rear`, accelerate hard forward/backward and laterally, then stop thrust without braking. Expected: clearly visible but bounded camera yield during acceleration, followed by recovery; constant speed alone must not keep increasing the offset.
- [ ] In `/cine cam rear`, yaw/pitch/roll the ship with the mouse and roll controls. Expected: the camera orientation follows with visible weight and catches up without losing the ship.
- [ ] At 0–10 m/s, confirm there is no procedural camera shake.
- [ ] Above 10 m/s, confirm shake fades in progressively and does not look like spectator input drift.
- [ ] Select `/cine cam flyby`. Expected: the camera position stays world-locked and has no procedural shake.
- [ ] Select `/cine cam frontmount`. Expected: a close camera sits just beyond the nose and looks back across the forward structure; vibration is small and faster than on tracking shots.
- [ ] Select `/cine cam rearmount`. Expected: a close camera sits just behind/above the tail and looks forward along the ship; vibration is small and fast.
- [ ] Select `/cine cam distant`. Expected: a very wide top-side view makes the ship small against the world and follows position/rotation with substantially more weight; it has no shake.
- [ ] Test both mounted cameras on unusually shaped grids. They intentionally do not force the whole bounding box onscreen; note any placement inside blocks for later mount-clearance tuning.

## 10. Debug output

- [ ] Run `/cine debug on`. Expected: confirmation is shown.
- [ ] Confirm debug output identifies cinematic state, target, shot, shot age/duration, speed, and shot-change reason.
- [ ] Let cinematic mode run for at least 15 seconds. Expected: status appears approximately every five seconds, not every simulation tick.
- [ ] Trigger several shot cuts. Expected: each cut logs the selected shot and reason once.
- [ ] Run `/cine debug off`. Expected: periodic status stops.

## 11. Mandatory emergency-stop cases

Perform these one at a time, re-entering cinematic mode between tests:

- [ ] Leave the cockpit/control seat. Expected: one EmergencyStop reason, cinematic mode OFF, and no repeated camera fighting.
- [ ] Transfer control to a different cockpit or remote control. Expected: controlled-entity-change EmergencyStop.
- [ ] Close/remove/destroy the controlled cockpit. Expected: invalid controller/target EmergencyStop.
- [ ] Split, close, remove, or destroy the target grid. Expected: invalid/changed grid EmergencyStop.
- [ ] Use another game camera action that takes camera ownership. Expected: camera-takeover EmergencyStop; the mod does not reclaim the camera every tick.
- [ ] Exit to the main menu while cinematic mode is ON. Expected: unload uses the same safe shutdown path and does not leave errors repeating in the log.
- [ ] Save, reload, and re-enter the world. Expected: the mod initializes cleanly and F9 still works.
- [ ] After every emergency case, run `/cine status` when possible. Expected: cinematic mode is OFF and a later activation works normally.

## 12. Clipping expectations for V0

- [ ] Test near a hangar wall, large grid, cliff, and terrain. Record any clipping, but do not treat ordinary obstruction avoidance as implemented behavior.
- [ ] Confirm there is no visible low-camera or collision-avoidance behavior.
- [ ] Report cases where a nominally high shot clips excessively under ordinary open-world driving; these may justify profile tuning without adding collision logic.

## Completion criteria

- [ ] No game crash, repeating exception, or permanently stuck camera.
- [ ] Vehicle controls remain active for the entire cinematic session.
- [ ] Original camera state restores reliably after normal exit and safe shutdown.
- [ ] All nine profiles work in gravity and zero gravity.
- [ ] All shot changes are hard cuts.
- [ ] Automatic selection is varied and obeys side continuity.
- [ ] All handled commands remain local.
- [ ] The post-test `SpaceEngineers.log` contains no recurring `SECinematicCamera` errors.
