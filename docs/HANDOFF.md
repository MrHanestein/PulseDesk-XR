# Handoff log

Append newest entries at the bottom: date/time, who, task, what changed, what is untested, what is next.

## Sat 14 Nov 2026, 17:00 GMT — Claude (cloud session)
- Wrote all app code under `Assets/PulseDesk/` and `BUILD_GUIDE.md`. Visuals are built from code at runtime; no prefabs.
- **Untested:** everything. No Unity or Meta SDK was available; C# was syntax-checked only. Meta-specific files are behind scripting defines (`PULSEDESK_META`, `_MICROGESTURES`, `_MRUK`, `_DEPTH`), off by default.
- Least certain API calls: `MRUKRoom.Raycast(Ray, float, out RaycastHit, out MRUKAnchor)`, `OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonType()`, `OVRHand.GetMicrogestureType()`, TMP font-size scale in `Make.Text`.
- **Next:** BUILD_GUIDE Stop 0 onward. First human checks are Stop 1 (compiles, 8 tests pass) and Stop 2 (desk test with the mouse).

## Sun 15 Nov 2026, morning — Claude (desktop session, headless Unity)
- **Project home is `C:\Users\regen\PulseDesk-XR`** (the Unity project). Code and docs were copied in from `Documents\PulseDesk-XR`, which is now a stale copy. Unity `6000.6.9f1`, Meta XR All-in-One `209.0.0`.
- **Stops 0–3 and the build half of Stop 4 done, all from the command line** (`tools\unity.ps1`, entry points in `Assets/PulseDesk/Editor/PulseDeskAutomation.cs`):
  - Package name `com.pulsedesk.pulsedeskxr`, company `PulseDesk`, product `PulseDesk XR`. Change before the first upload if you want another; it cannot change afterwards.
  - Release keystore at `%USERPROFILE%\PulseDeskKeys\` (keystore + `keystore.properties` with alias and passwords). **Outside the repo. Back it up and give B a copy privately.** The build reads it from there (or from `PULSEDESK_KEYSTORE_DIR`).
  - TMP Essentials imported. Compiles with no errors with all four Meta switches on (`PULSEDESK_META`, `_MICROGESTURES`, `_MRUK`, `_DEPTH`). None of the Meta files needed changes.
  - 8/8 EditMode tests pass.
  - `Main.unity`: Camera Rig, Passthrough, Hand Tracking (left/right), MR Utility Kit (Device with prefab fallback, World Lock on), `PulseDesk` object. Scene permission requested on startup. `Dev_B.unity`: plain camera + `PulseDesk` object.
  - OVR project config: hands = Controllers and Hands, passthrough/scene = Supported, anchors on. Project Setup Tool: no Required items outstanding.
  - OpenXR validation needed the Hand Tracking Subsystem feature; `PulseDesk > Fix OpenXR validation issues` (also run by the build) applies it.
  - `Builds/PulseDeskXR.apk`: 67 MB, `arm64-v8a` only, release-signed, version code 3.
  - Play-mode smoke test (`PulseDeskAutomation.PlaySmoke`, Dev_B scene): dock, 3 lanes, 3 starter cards, move to Done, "Plan my day" adds 3 cards, re-place dock, no errors.
- **Untested:** anything on a headset (no device was connected): passthrough, pinch/ray/poke, microgestures, table finding, text size and dock facing. Nobody has looked at the Game view either; the smoke test checks objects and errors, not appearance.
- **Known small things:** TMP warns that LiberationSans has no ellipsis character (falls back to truncate). The APK requests `RECORD_AUDIO` (comes from the Meta SDK packages, not our code). `FindFirstObjectByType` deprecation warnings.
- **Later the same day:** the project is now solo. README, PLAN and SUBMISSION were rewritten for one person. Added a "New task" button that opens the system keyboard (PC keyboard in the Editor), with `requiresSystemKeyboard` set in the Meta project config. Banner text enlarged and card titles kept to one line after looking at screenshots from the smoke test (`Logs/shot-*.png`, taken from a seated eye position). The keyboard is untested on a headset; if it does not open, the app falls back to the goal picker.
- **After the first try in the Editor (user feedback):** presets looked like commands and only added cards, nothing could be deleted, the sprint was fixed at 5 minutes, alerts were too quiet, and "Move dock here" showed no change in the Editor (the camera does not move there, so the dock lands in the same place). Changes: "Break it down" is now "Routines" with noun names and a banner that says it adds three steps. "Remove card" (button, or drop a card on it) and "Clear done". Minus and plus beside the timer step the sprint through 1 to 60 minutes. Typed or dictated lines are parsed for "at 3pm", "in 20 min" and "for 25 min" (`Core/QuickAdd.cs`); due tasks get a soft tone five minutes before and an alert when due, and the banner shows the next due item. Sounds are louder, halfway has a tone, time-up repeats each minute. Re-placing the dock plays a sound and a short grow-in. 12 unit tests. Still nothing tested on a headset.
- **Next (needs a person):** Stop 2 look-check in the Editor; install the APK on the Quest and run Stop 4's checklist; create the `Competition` channel and upload; Stops 5–8 on device; video; Devpost.
