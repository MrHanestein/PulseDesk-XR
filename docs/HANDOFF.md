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
- **Next (needs a person):** Stop 2 look-check in the Editor; install the APK on the Quest and run Stop 4's checklist; create the `Competition` channel and upload; Stops 5–8 on device; video; Devpost.
