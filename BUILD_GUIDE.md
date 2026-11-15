# Build guide: from empty project to submitted entry

Follow the stops in order. Each stop says what is already built, what **you** do, and what to **verify** before moving on. If a verify step fails, see Troubleshooting at the end, or paste the exact Unity Console error to your coding agent.

This guide replaces the day-by-day coding tasks in PLAN.md (the code is now written). PLAN.md still holds the deadline, the gates, the eight test scripts and the working agreement.

## What is already built (and what is not proven)

All of the app's code is in `Assets/PulseDesk/`. The dock, lanes, cards, buttons, banner, timer, streak and sounds are **created from code at runtime**, so there are no prefabs to build and no Inspector references to drag. The scene needs only Meta's building blocks and one `PulseDesk` object.

| Part | State |
| --- | --- |
| Task logic, save/load, streak, sprint timer, goal splitter, 8 unit tests | Written. Plain Unity code. |
| Dock and card visuals, layout, banner, audio | Written. Plain Unity + TextMeshPro. |
| Hand interaction (near pinch-grab, far ray pinch, fingertip poke) | Written as our own small layer on top of Meta hand tracking data. |
| Mouse + keyboard test mode in the Editor | Written. No headset or Meta SDK needed. |
| Meta hand tracking, system pause, spatial anchor | Written behind the switch `PULSEDESK_META`. |
| Thumb microgestures | Written behind `PULSEDESK_MICROGESTURES`. |
| Table finding from a room scan / from live depth | Written behind `PULSEDESK_MRUK` / `PULSEDESK_DEPTH`. |
| PulseDesk menu: setup, feature detection, Android settings, APK build | Written. |

**Not proven:** none of this has been compiled or run. It was written without access to Unity or the Meta SDK and checked for syntax only. Expect a few compile fixes at Stops 1 and 3. That is why the Meta-specific files sit behind switches: everything else compiles without the SDK, and a Meta file that fails can be switched off on its own while you fix it.

**Changed from BLUEPRINT.md:** cards are moved by our own `HandInteractor` instead of Interaction SDK grab/snap prefabs, and there are no prefabs. This removes the wiring that could not be checked from here. Gaze (BLUEPRINT 9.2) and voice dictation (10, layer 2) are not built; Stop 9 says how to add them if time allows.

---

## Before Stop 0 — Accounts and tools (start now; some of this has a waiting time)

**A (you, Team Representative)**
- [ ] Apply for / confirm **Meta VR Start** membership on the Meta account that will submit. Approval is not instant and is required by submission time.
- [ ] Join the hackathon on Devpost and add B to the team.
- [ ] Meta developer dashboard: Developer Access is on; create an organisation if you have none; **create a new app** for Quest (this gives the App ID and is where the `Competition` release channel will live); invite B to the organisation.
- [ ] Unity Hub: Unity 6 (6000.0.66f2 or later) with *Android Build Support*, *OpenJDK*, *Android SDK & NDK Tools*. Tell B the exact version.
- [ ] Headset in developer mode, USB debugging allowed, Meta Quest Developer Hub installed.

**B (teammate, no headset)**
- [ ] Join the Devpost team; accept the Meta organisation invite; accept the GitHub invite.
- [ ] Install Git, Git LFS, and the **same Unity version** as A with *Android Build Support*.
- [ ] In the Unity Asset Store, signed in with **his own** Unity account, click "Add to My Assets" on **Meta XR All-in-One SDK** (free). Without this, the project A commits will fail to download the Meta packages on B's machine.
- [ ] A Meta account for testing the Invite URL later (a headset is not needed to accept it; installing needs one, so borrow A's headset signed in as B, or ask a friend with a Quest).

## Stop 0 — Put the code in the repo (10 min, A)

1. Unzip `pulsedesk-code.zip` into the repo root so that `Assets/PulseDesk/` and `BUILD_GUIDE.md` sit next to `README.md`. Let it replace `README.md`.
2. Commit and push: `git add . && git commit -m "Add PulseDesk code and build guide" && git push`.

**Verify:** `git status` is clean; GitHub shows `Assets/PulseDesk/Scripts/Core/TaskStore.cs`.

## Stop 1 — Create the Unity project (45 to 90 min, mostly waiting; A)

Do BLUEPRINT.md section 5.2 steps 1 to 6 (URP template, switch to Android, install Meta XR All-in-One SDK, OpenXR, Project Setup Tool, player settings). Create the project in a separate folder, then copy its `Assets/` contents, `Packages/` and `ProjectSettings/` into the repo folder, and open the repo folder in Unity Hub.

Then:
1. **Window > TextMeshPro > Import TMP Essential Resources.** Without this, all text is invisible.
2. Wait for the Console to finish compiling.

**Verify**
- [ ] Console has no red errors. The Meta switches are all off at this point, so any error here is in plain Unity code: paste it to your agent with the prompt in MASTERCLASS.md section 4.
- [ ] A **PulseDesk** menu appears in the top menu bar.
- [ ] Window > General > Test Runner > EditMode > Run All: 8 tests pass.

Commit (`Assets`, `Packages`, `ProjectSettings`, including the new `.meta` files) and push. Write the Unity version into README.md.

**B, once A has pushed:** `git pull`, open the folder in Unity Hub with the same version, wait for the import, import TMP Essentials if asked, and confirm the same three checks. If packages fail to resolve, the Asset Store step in "Before Stop 0" was missed.

## Stop 2 — Desk test without a headset (15 min; A creates the scene, then B owns this)

1. **A:** File > New Scene (Basic), save as `Assets/PulseDesk/Scenes/Main.unity`. **B:** do the same but save as `Dev_B.unity`; never edit `Main.unity`.
2. Menu **PulseDesk > 1. Set up project**. Save the scene.
3. Press Play. Look at the Game view.

Controls in the Editor: drag cards with the **mouse**; click buttons; **Left/Right arrow** = focused card to Urgent/Done; **Up/Down** = change focus; **Space** = pause; **B** = open the goal picker; **R** = re-place the dock.

**Verify**
- [ ] A dark dock with three lanes labelled NEXT, URGENT, DONE, each with an icon.
- [ ] Three starter cards. One is in URGENT and the timer above it counts down.
- [ ] Dragging a card onto DONE plays a chime and the streak text changes.
- [ ] Dragging a second card to URGENT sends the first back to NEXT.
- [ ] "Break it down" shows four goals; picking one drops three cards into NEXT.
- [ ] Stop and Play again: the board is as you left it. (PulseDesk > Reset saved tasks starts over.)

Things B can tune now without a headset: colours and labels in `UI/Theme.cs`; sizes and positions in `UI/DockView.cs`; card text sizes in `UI/TaskCardView.cs`; starter cards in `Core/TaskStore.cs`; preset goals in `Core/GoalDecomposer.cs`.

## Stop 3 — Add the Meta rig and switch on hands (30 min, A)

1. In `Main.unity`, open **Meta > Tools > Building Blocks** and add: **Camera Rig**, **Passthrough**, **Hand Tracking**. Delete the scene's original Main Camera.
2. Select the camera rig; on **OVRManager** set: Hand Tracking Support = *Controllers and Hands* or *Hands Only*; Passthrough Support = *Supported*; Scene Support = *Supported* (not Required); Anchor Support = on. Full table: BLUEPRINT 5.3.
3. **Meta > Tools > Project Setup Tool > Fix All** again.
4. Menu **PulseDesk > 2. Detect Meta SDK features**. Read the Console line that says which features were found. Unity recompiles.

**Verify**
- [ ] No red errors after the recompile. If there are, note the **file name** in the error:
  - `OvrHandPointerSource` or `OvrSystemHooks` → PulseDesk > Features > untick *Hands*
  - `OvrMicrogestures` → untick *Thumb microgestures*
  - `MrukTableFinder` → untick *Room scan placement*
  - `DepthTableFinder` → untick *Depth placement*
  
  Then give the error to your agent: "Fix this file against the installed Meta SDK; check the API with metavr docs search first." Tick the feature back on once fixed. The app still runs with a feature off.
- [ ] Press Play with Meta XR Simulator (or Quest Link): the dock appears, and the mouse still works in the Game view.

## Stop 4 — First build on the headset (45 min, A). Do this tonight.

1. **Edit > Project Settings > Player > Android**: set your package name (`com.<team>.pulsedeskxr`), and under Publishing Settings create or select the **release keystore** (kept outside the repo).
2. **File > Build Profiles**: add `Main.unity` to the scene list.
3. Menu **PulseDesk > 3. Apply Android player settings**, then **PulseDesk > 4. Build APK**. The APK lands in `Builds/PulseDeskXR.apk`. (Or use Build And Run with the headset plugged in.)
4. Install: `metavr device list`, then install the APK with Meta Quest Developer Hub or `adb install -r Builds/PulseDeskXR.apk`.

**Verify on the headset, controllers in another room**
- [ ] You see your real room (passthrough), not black.
- [ ] The dock appears in front of you within reach, facing you, text readable.
- [ ] **Near:** pinch a card between thumb and index, carry it to DONE, let go. Chime.
- [ ] **Far:** from 40 cm back, a thin line appears when your hand points at a card; pinch and drag it to URGENT.
- [ ] **Poke:** push "Break it down" with a fingertip; poke a goal; three cards arrive.
- [ ] Pinch the timer: "Paused". Pinch again: runs.
- [ ] Open the system menu and close it: the timer paused and resumed, and a pause you chose stays paused.
- [ ] "Move dock here" re-places the dock where you are now looking.

Write down anything that feels wrong (too far, too small, pinch unreliable). Sizes are constants at the top of `HandInteractor.cs` (pinch and poke radius) and `DockPlacer.cs` (reach).

**Then straight away (A, with B):** in the Meta developer dashboard open the app you created, go to Distribution > Release Channels, create a channel named exactly `Competition`, upload this APK, create the Invite URL and send it to B. B opens it signed in to his own Meta account and accepts. If a headset signed in as B is available, install and launch from there (SUBMISSION.md section 4). This proves the delivery route days before the deadline. If the upload is rejected, the message usually names the cause: debug-signed APK, a version code that is not higher than the last upload, or a manifest problem (run Meta > Tools > Project Setup Tool > Fix All and rebuild).

## Stop 5 — Thumb microgestures (20 min, A)

Already on if Stop 3 detected them. If your OVRManager has a microgestures toggle, enable it and re-run the Project Setup Tool.

Pose: rest your hand, thumb raised above the side of the index finger, other fingers loosely curled. Slide the thumb along the index finger.

**Verify**
- [ ] Point at a card (it brightens: that is the focused card). Swipe thumb **left**: it moves to URGENT. Swipe **right**: DONE.
- [ ] Swipe forward/backward: the highlight moves between cards.
- [ ] Thumb tap: pause/resume.

If nothing happens: run Meta's `HandMicrogestures` sample scene to confirm the headset recognises your gestures at all, then check `OvrMicrogestures.cs` with your agent. If it cannot be fixed in an hour, leave the feature off and change starter card 2's text in `TaskStore.SeedOnboarding` to drop the thumb hint. Every action still has a pinch route.

## Stop 6 — Dock lands on the real desk (45 min, A)

1. Building Blocks: add **MR Utility Kit**. On the MRUK component set the data source to *Device with prefab fallback* and enable World Lock.
2. On OVRManager, request the **Scene** permission on startup (BLUEPRINT 5.3).
3. Rebuild and install.
4. While testing, watch the log: `adb logcat -s Unity | findstr PulseDesk` (Windows) shows `dock placed by MrukTableFinder`, `DepthTableFinder` or `Fixed position`.

**Verify, in this order**
- [ ] Room with Space Setup done and a desk marked: log says `MrukTableFinder`; the dock sits on the desk surface.
- [ ] Settings > clear the room scan (or a different room): log says `DepthTableFinder`; the dock still sits on the desk.
- [ ] Sofa with nothing in front: log says `Fixed position`; the dock floats in reach.
- [ ] Deny the Scene permission: the app still gives you a dock.

If the dock sinks into or floats above the desk, adjust the `+ 0.005f` in `DockPlacer.Place`. If either finder misbehaves, switch it off (Stop 3) rather than ship it broken: the fixed position always works.

## Stop 7 — Robustness (30 min; A on the headset, B reading the script and writing down results)

Run the eight test scripts in PLAN.md section 5 (cold start, no controllers, unscanned room, permissions denied, offline, interruption, comfort, persistence). Fix or cut. **After Tuesday 18:00 GMT: fixes only.**

## Stop 8 — Performance and field of view (30 min, A)

BLUEPRINT section 11. The scene is light, so the check is mostly a formality:
- [ ] 72 fps in a 15-second trace with all cards on the dock.
- [ ] `metavr device fov-sim enable --perceived`: lanes, timer, banner and buttons all in view when seated. Disable it afterwards.

## Stop 9 — Optional extras, only if Stops 1 to 8 pass by Monday night

- **Voice:** add one of Meta's dictation building blocks and wire its transcript event (a `UnityEvent<string>`) to `PulseDeskApp.SubmitGoal`. That method already splits any sentence into cards. Add a "Speak" button in `DockView.BuildAll` that starts the dictation, with a 5-second timeout that falls back to the picker. Needs internet; must fail quietly without it.
- **Gaze (eye-tracked devices only):** not needed for judging on Quest 3/3S. Skip unless everything else is finished.
- **Polish B can do in the Editor:** rounder card shapes, a short scale-in when cards spawn, nicer sounds (keep the licence log in README).

Do not claim either extra on Devpost unless it is in the uploaded build.

## Stop 10 — Video (Tuesday, B leads, A records)

SUBMISSION.md section 3 has the shot list. Record in the headset (Quest capture, or `metavr capture`). Show hands with no controllers in the first 20 seconds. Under 3 minutes, public on YouTube or Vimeo.

## Stop 11 — Final build and submit (Wednesday, done by 15:00 GMT)

1. PulseDesk > 4. Build APK (the version code goes up by itself). Confirm it is release-signed and `lib/` contains only `arm64-v8a`.
2. Upload to the `Competition` channel; install from the Invite URL on the second account; run Stop 4's checklist one last time on **that** install.
3. Edit SUBMISSION.md section 2 so it describes what shipped (the hand layer is our own code on Meta hand tracking; say so plainly). Fill in the Devpost form. The other person reads every field.
4. Submit. Tag the commit `submission`. Touch nothing until winners are announced.

---

## Who does what, day by day

| | Person A (has the headset) | Person B (Editor only) |
| --- | --- | --- |
| Sat | Stops 0, 1, 3, 4 and the release-channel test | Pull after Stop 1; Stop 2; start tuning layout and text |
| Sun | Stops 5, 6 | Look and feel in `Theme`, `DockView`, `TaskCardView`; onboarding wording; sounds |
| Mon | Stops 7, 8; fix what the headset shows | Devpost drafts; video script; Stop 9 polish |
| Tue | Record footage; freeze at 18:00 | Edit video; finish the form text |
| Wed | Final build, upload, install test | Proof-read; submit together by 15:00 GMT |

Scene rule still applies: only A edits `Main.unity`. B works in code files or a `Dev_B.unity` scene with its own PulseDesk object.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| No text anywhere | TMP essentials not imported | Window > TextMeshPro > Import TMP Essential Resources |
| Everything pink, or invisible in the headset but fine in the Editor | Material/shader missing from the build | PulseDesk > 1. Set up project (creates `Resources/PulseDeskSurface.mat`), rebuild |
| Text far too big or too small | Text scale assumption is off for your TMP version | Change `TextScale` in `UI/Make.cs` (try 0.001 or 0.1) |
| Text mirrored or facing away | Dock facing is flipped | In `DockPlacer.Place`, use `Quaternion.LookRotation(-flatDir, Vector3.up)` |
| Black background on the headset | Passthrough not set up | Passthrough building block present; OVRManager Passthrough Support on; camera background transparent (Project Setup Tool) |
| Hands do nothing on the headset | Hands feature off, or no hand objects in the scene | PulseDesk > Features > Hands ticked; Hand Tracking building block present; OVRManager hand support not "Controllers Only" |
| Near pinch works, pointing does not | Pointer pose not valid or wrong space | Log `IsPointerPoseValid` in `OvrHandPointerSource.Get`; ask the agent to check `OVRHand.PointerPose` usage |
| Pointing works, near pinch does not | Fingertip bones not found | In `OvrHandPointerSource.CacheTips`, log the bone ids; adjust the `EndsWith("IndexTip")` match |
| A card is grabbed by pinches meant for something else | Near radius too generous | Lower `NearRadius` in `HandInteractor.cs` |
| Buttons fire twice | Should not happen (0.5 s debounce) | Check `DockButton.OnPress` |
| Compile error names an `Ovr…`, `Mruk…` or `Depth…` file | Meta API differs in your SDK version | Switch that feature off (Stop 3), fix with the agent, switch back on |
| Mouse does nothing in Play mode | Input handling setting | Project Settings > Player > Active Input Handling = Both |
| Test Runner shows no tests | Test Framework package missing | Package Manager > Unity Registry > Test Framework > Install |
