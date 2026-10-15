# SECinematicCamera V0 in-game test checklist

Use a disposable local test world. Start with a simple rover or small ship that has a cockpit, working propulsion, and enough open space to move safely.

Record failures with the test number, what happened, the expected result, and the relevant lines from `SpaceEngineers.log`.

## 1. Load and basic readiness

- [ ] Add `SECinematicCamera` to the test world's active mod list.
- [ ] Load the world without a mod compile error or crash.
- [ ] Confirm there are no `SECinematicCamera` script errors in `SpaceEngineers.log`.
- [ ] While on foot, press T and F9. Expected: CineCam does not start or reserve vanilla controls; F9 retains normal spectator behavior when available.
- [ ] Enter a cockpit or remote control attached to a valid grid.
- [ ] Run `/cine status`. Expected: `OFF | next start: AUTO`.
- [ ] Run `/cine get`. Expected: the compiled runtime settings are shown.

## 2. First milestone: activation and restoration

- [ ] Begin in first-person cockpit view and note the exact view.
- [ ] Press F9. Expected: cinematic mode turns ON with a motion-weighted opening shot and a `Cine Cam - Auto` HUD notification.
- [ ] While cinematic mode is active, steer, accelerate, brake, roll, and use normal ship/rover controls. Expected: the vehicle remains fully controllable.
- [ ] Confirm the camera follows smoothly while staying in the selected opening shot.
- [ ] Press F9 again. Expected: cinematic mode turns OFF and restores first-person cockpit view.
- [ ] Press T while OFF. Expected: AUTO starts and the hint shows T for manual cameras and V to exit. Press T again several times: each tap hard-cuts to the next manual profile, switches to MANUAL, and never cycles into OFF.
- [ ] Repeat from third-person view at two different zoom distances. Expected: F9 exit restores third-person view and the entry zoom distance.
- [ ] After exit, press V twice. Expected: the same controlled cockpit switches first/third normally; it must not remain in SpectatorFixed or jump to another cockpit.
- [ ] Activate from first person and press V. Expected: cinematic mode exits to the same first-person cockpit view without an additional vanilla V toggle.
- [ ] Repeat from third person and press V. Expected: the saved third-person mode and zoom are restored; subsequent V presses behave normally.
- [ ] While active, press Ctrl+F9 repeatedly. Expected: each press hard-cuts to the next manual profile and automatic selection turns OFF; no Blueprint screen opens.
- [ ] Press Ctrl+F9 while OFF: CineCam stays OFF. Press Ctrl+T, Alt+T, and Shift+T: no CineCam action. Confirm B/Ctrl+B retain vanilla behavior.
- [ ] Open cockpit building mode and press T: vanilla orientation reset works and CineCam does not activate/cycle. Repeat with chat, inventory, terminal, and menus open: no CineCam hotkey is handled. Close them and confirm T/F9 work again.
- [ ] With spectators enabled, enter and exit using F9 from both view modes, including rapid toggles. Expected: vanilla static spectator does not override CineCam or the restored cockpit camera.
- [ ] If a saved world loads into spectator while a cockpit is still controlled, expected: startup recovery immediately selects that cockpit in first person.
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
- [ ] Turn cinematic mode OFF. Expected: the current controlled entity has its saved first/third-person mode and spectator movement no longer affects it.

## 4. Chat command handling

- [ ] Run `/cine`. Expected: it toggles cinematic mode.
- [ ] Run `/cine on` twice. Expected: cinematic mode remains ON without duplicating state.
- [ ] Run `/cine off` twice. Expected: cinematic mode remains safely OFF.
- [ ] Confirm handled `/cine` commands do not appear in multiplayer chat and are not sent to the server/other players; responses appear as temporary HUD notifications.
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
- [ ] `/cine cam chase` and `/cine cam 10`: Chase Rear or Chase Offset; the selected variant is named in the HUD notification/status.
- [ ] Confirm every manual selection immediately hard-cuts to the new pose—there must be no travel/interpolation between shots.
- [ ] Confirm position and aim follow smoothly after the cut for all tracking shots.
- [ ] For WorldLockedFlyBy, confirm the camera is initially placed approximately 60–100 m away, stays fixed in world space, and pans smoothly toward the moving vehicle. Small left/right course corrections must not flip the composition or jerk the pan.
- [ ] With auto ON, fly-by should cut at five seconds or when the receding grid reaches approximately 700 m, whichever happens first. Manual fly-by should hard-cut to a newly placed fixed camera at the same limits.
- [ ] Confirm DistantTopSide lasts about 5 seconds in automatic mode.
- [ ] Confirm both mounted cameras last about 2.5 seconds in automatic mode.
- [ ] Confirm OverheadDrift lasts about 3 seconds in automatic mode.
- [ ] Confirm Chase lasts about 6 seconds in automatic mode.
- [ ] Confirm selecting a manual shot turns automatic changes OFF.
- [ ] Switch cinematic mode OFF and ON after a manual shot. Expected: the new activation reports `Cine Cam - Auto`; manual state does not silently carry over.
- [ ] Switch cinematic mode OFF and ON several times. Expected: consecutive activations never reuse the same opening shot, and the opening varies with vehicle motion.
- [ ] Confirm orbit/overhead shots remain high relative to natural gravity. The two close mounted shots may sit close to the hull, but there should be no wheel-height or ground-level view.

### Composition checks

- [ ] Run `/cine reset` and `/cine get`. Expected: the default distance is `0.75`.
- [ ] In RearHighThreeQuarter, confirm the ship is intentionally offset from exact screen center and the view favors its forward/upper detail.
- [ ] In FrontHighThreeQuarter, confirm the focus favors the nose/cockpit region rather than the geometric center.
- [ ] In SideHighLeft and SideHighRight, fly forward and backward. Expected: the camera leaves visible lead room in the direction of screen-space travel.
- [ ] Confirm the ten profiles do not all place the ship at the same screen coordinate.
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

### Chase checks

- [ ] Select Chase while stationary. Expected: the camera begins behind and slightly above the vehicle, with no cut between its subsequent follow movements.
- [ ] Accelerate sharply. Expected: the vehicle moves forward in frame before the spring catches up, and camera distance briefly increases with acceleration.
- [ ] Compare low and high constant speeds. Expected: Chase runs farther from the vehicle at higher speed.
- [ ] Turn sharply at speed. Expected: the camera initially preserves its old world-space trajectory, then smoothly settles behind the new trajectory instead of snapping behind grid Forward.
- [ ] Fly with strong lateral slip. Expected: Chase follows velocity direction progressively above roughly 4 m/s and is primarily trajectory-aligned by roughly 30 m/s, with heading changes limited to 30 degrees per second.
- [ ] For Chase Rear, confirm the view remains nearly centered behind the trajectory with only a subtle lateral drift.
- [ ] For Chase Offset, remain in the shot long enough to see a continuous rear-left/center/rear-right wander with no internal cut.
- [ ] Roll rapidly on a planet. Expected: the horizon carries about 30% of vehicle roll instead of matching it fully.
- [ ] Roll rapidly in zero gravity. Expected: camera Up follows through a slow, restrained response without snapping one-for-one to cockpit roll.

## 7. Planet and zero-gravity orientation

- [ ] On a planet, drive across slopes and uneven ground. Expected: camera Up remains aligned with natural gravity rather than rolling with the rover, except Chase's deliberately restrained 30% roll response.
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
- [ ] Select `/cine cam frontmount`. Expected: a close camera sits just outside the rightmost hull surface at the cockpit's longitudinal/vertical station and looks straight forward; vibration is small, fast, and local to the camera.
- [ ] Select `/cine cam rearmount`. Expected: a close camera sits outside the rear-left corner, slightly raised, and looks straight forward along the ship; vibration is small, fast, and local to the camera.
- [ ] Select `/cine cam distant`. Expected: an extremely wide view about five times farther than before makes the ship very small against the world, permits broad movement within frame, barely reacts to brief mouse turns, and has no shake.
- [ ] Test both mounted cameras on unusually shaped grids. They intentionally do not force the whole bounding box onscreen; note any placement inside blocks for later mount-clearance tuning.

### Thrust taps and reversal regression

- [ ] In rear, front, side-left, side-right, overhead, distant, and Chase, tap W/A/S/D individually at low speed. Expected: ship motion remains visible, but there is no separate camera pan or jerk on every key transition. Mounted cameras intentionally remain rigid.
- [ ] In both side shots and distant, drift forward at 0.5–3 m/s, tap S through a complete stop into reverse, then repeat in the other direction. Expected: lead room crosses center gradually without a fast aim flip or a special jump near 1 m/s.
- [ ] In Chase, reverse through 0, 4, 12, 17, and 30 m/s with no ship rotation, then repeat with slight lateral thrust. Expected: the heading remains finite and continuous, low-speed taps do not swing the camera around, and sustained high-speed reversal produces a gradual response instead of a flip.
- [ ] Repeat with dampeners ON and OFF, and in gravity/zero gravity. Confirm manual/automatic shot changes still hard-cut without an interpolated travel path.

### Alt + mouse orbit

- [ ] Select Chase, rear, front, both side shots, and distant. Hold Alt and move the mouse slowly in both axes. Expected: the camera orbits the ship with low sensitivity and eased angular motion, while keeping it framed; simply holding Alt without moving does not change the angle.
- [ ] While holding Alt, use W/A/S/D, Space, C, and roll controls. Expected: translation and keyboard roll remain available, and mouse movement does not also yaw/pitch the ship. Release Alt and confirm normal mouse steering returns.
- [ ] Release Alt after a small drag, a large drag, and multiple complete horizontal orbits. Expected: a gentle return by the shortest horizontal arc, without a snap or prolonged spinning. Vertical motion stays away from the pole.
- [ ] In AUTO, orbit and hold the view longer than the shot's normal duration. Expected: automatic cuts pause while freelooking/returning and resume afterward, retaining AUTO mode. T/Ctrl+F9 and manual chat selection still hard-cut immediately and discard the previous orbit offset.
- [ ] In both mounted shots, hold Alt and pan/tilt the mouse. Expected: smooth aim rotation from the same hull anchor, no orbit or extra positional lag, and no mouse steering of the ship. Release Alt: aim eases back parallel to cockpit Forward. Repeat while the ship translates, pitches, yaws, and rolls, in gravity and zero gravity.
- [ ] In both mounted shots, drag far upward/downward and through a full horizontal turn. Expected: pitch stays within ±65 degrees of the mount's local frame, the camera remains attached, and return/cut/exit behavior works as in the tracking shots.
- [ ] Try Alt + mouse in fly-by and overhead shots. Expected: their existing camera rules remain unchanged and their timers are not paused by Alt.
- [ ] Open chat, inventory, terminal, building mode, or a menu while freelooking. Expected: mouse movement in those interfaces does not accumulate as camera input; after closing, there is no queued jump.
- [ ] Compare slow and fast drags at low/high rendering frame rates. Expected: each mouse displacement is consumed once, fast swipes accelerate smoothly, and slow drags remain usable with game mouse sensitivity below 1.
- [ ] Change the native freelook binding and X/Y inversion in the game's options. Expected: CineCam follows the native binding and inversion. Restore the options afterward.
- [ ] Try `/cine set orbitsensitivity 0.015` and `0.06`, then `/cine reset`. Expected: lower/higher mouse sensitivity for tracking orbit and mounted aim, and the restored `0.03` default; values outside `0.005`–`0.5` are rejected.
- [ ] Run `/cine return off`, adjust a tracking shot and both mounted shots, then release Alt. Expected: smoothed mouse motion finishes and the adjusted angle stays relative to that shot; AUTO timing resumes after settling. Camera cuts reset the angle, and cinematic OFF/ON preserves the return setting.
- [ ] Run `/cine return on` while an angle is retained: it eases back. Switch return OFF during that return: it keeps the current angle without jumping. `/cine get` reports the setting; `/cine reset` restores return ON. Invalid `/cine return` arguments show usage without changing the setting or sending the command to multiplayer chat.
- [ ] Exit with V/F9, switch seats, or trigger a camera takeover during orbit. Expected: the usual safe shutdown/view restoration applies and the next activation has no leftover orbit input.

## 10. Debug output

- [ ] Run `/cine debug on`. Expected: confirmation is shown.
- [ ] Confirm debug output identifies cinematic state, target, shot, shot age/duration, speed, and shot-change reason.
- [ ] Let cinematic mode run for at least 15 seconds. Expected: status appears approximately every five seconds, not every simulation tick.
- [ ] Trigger several shot cuts. Expected: each cut logs the selected shot and reason once.
- [ ] Run `/cine debug off`. Expected: periodic status stops.

## 11. Mandatory emergency-stop cases

Perform these one at a time, re-entering cinematic mode between tests:

- [ ] Leave the cockpit/control seat. Expected: one EmergencyStop reason, cinematic mode OFF, and no repeated camera fighting.
- [ ] After leaving the cockpit, press V twice. Expected: only the character view changes; the camera never jumps back into the cockpit that was left.
- [ ] Transfer control to a different cockpit or remote control. Expected: controlled-entity-change EmergencyStop.
- [ ] Close/remove/destroy the controlled cockpit. Expected: invalid controller/target EmergencyStop.
- [ ] Split, close, remove, or destroy the target grid. Expected: invalid/changed grid EmergencyStop.
- [ ] Use another game camera action that takes camera ownership. Expected: camera-takeover EmergencyStop; the mod does not reclaim the camera every tick.
- [ ] Exit to the main menu while cinematic mode is ON. Expected: unload uses the same safe shutdown path and does not leave errors repeating in the log.
- [ ] Close the game with Alt+F4 while cinematic mode is ON. Expected: no “mod crashed” report; `SpaceEngineers.log` contains no `CineLogger.Info`/`ShowNotification` exception during `UnloadData`.
- [ ] Reload the world after exiting with cinematic mode active. Expected: the controlled cockpit starts in first person, never persisted SpectatorFixed.
- [ ] Save, reload, and re-enter the world. Expected: the mod initializes cleanly and F9 still works.
- [ ] After every emergency case, run `/cine status` when possible. Expected: cinematic mode is OFF and a later activation works normally.

## 12. Clipping expectations for V0

- [ ] Test near a hangar wall, large grid, cliff, and terrain. Record any clipping, but do not treat ordinary obstruction avoidance as implemented behavior.
- [ ] Confirm there is no visible low-camera or collision-avoidance behavior.
- [ ] Report cases where a nominally high shot clips excessively under ordinary open-world driving; these may justify profile tuning without adding collision logic.

## Completion criteria

- [ ] No game crash, repeating exception, or permanently stuck camera.
- [ ] Vehicle controls remain active for the entire cinematic session.
- [ ] Normal exit restores the entry first/third-person mode and zoom for the same controlled entity; safe shutdown never restores a stale cockpit or steals a camera after takeover.
- [ ] All ten profiles work in gravity and zero gravity.
- [ ] All shot changes are hard cuts.
- [ ] Automatic selection is varied and obeys side continuity.
- [ ] All handled commands remain local.
- [ ] The post-test `SpaceEngineers.log` contains no recurring `SECinematicCamera` errors.
