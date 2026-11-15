# PulseDesk XR

A task board that sits on your real desk. Built for Meta Quest 3 and 3S, used with bare hands.

The app puts a small dock in front of you with three lanes called Next, Urgent and Done. Tasks are cards. You pick a card up and drop it in a lane, or pinch it from a distance, or flick your thumb while your hand rests on your leg. A card in Urgent starts a five minute sprint. A card in Done plays a chime and adds to your streak for the day.

This is my entry for the Meta VR Start Developer Competition 2026, Productivity track, New Experience division. I am building it alone.

## What you can do in it

| Action | Up close | From a distance | Hand at rest |
| --- | --- | --- | --- |
| Move a card | Pinch it and carry it to a lane | Point, pinch and drag | Thumb swipe left for Urgent, right for Done |
| Choose a different card | Touch it | Point at it | Thumb swipe forward or back |
| Pause or resume the sprint | Poke the timer | Pinch the timer | Thumb tap |
| Add your own task | Poke "New task" and type | Pinch "New task" | |
| Split a goal into three steps | Poke "Break it down" | Pinch "Break it down" | |
| Put the dock somewhere else | Poke "Move dock here" | Pinch it | |

No controller is needed at any point. Typing uses the headset's own keyboard, which works with hands and without internet.

## How it finds your desk

The app tries three things in order and always ends with a dock you can reach.

1. If you have scanned your room, it looks for a table in the scan.
2. If not, it reads the headset's live depth data and looks for a flat surface below eye level.
3. If that fails too, it places the dock half a metre in front of you at lap height.

## Running it

You need Unity 6000.6.9f1 with Android Build Support, and the Meta XR All-in-One SDK 209 added to your Unity account.

1. Open this folder in Unity Hub.
2. Open `Assets/PulseDesk/Scenes/Dev_B.unity` and press Play to try it with a mouse and keyboard. Drag cards with the mouse. The arrow keys move the highlighted card, Space pauses, B opens the goal picker, R moves the dock.
3. Open `Assets/PulseDesk/Scenes/Main.unity` for the headset version.

To build the APK, use the menu PulseDesk, then "4. Build APK". The file lands in `Builds/PulseDeskXR.apk`. The release keystore is kept outside this repository in a folder called `PulseDeskKeys` in the user profile.

With the Unity editor closed, the same steps run from a terminal.

```
powershell -File tools\unity.ps1 -Tests
powershell -File tools\unity.ps1 -Method PulseDesk.EditorTools.PulseDeskAutomation.PlaySmoke -Name smoke
powershell -File tools\unity.ps1 -Method PulseDesk.EditorTools.PulseDeskAutomation.Build -Name build
```

## Where things are

```
Assets/PulseDesk/
  Scripts/Core         tasks, saving, streak, sprint timer, goal splitting (unit tested)
  Scripts/App          the one scene component, sounds, typed text
  Scripts/Interaction  pinch, ray and poke handling
  Scripts/Input        Meta hand tracking, thumb gestures, mouse and keyboard
  Scripts/Spatial      dock placement and the two table finders
  Scripts/UI           dock, lanes, buttons and cards, all built from code
  Editor               the PulseDesk menu and command line entry points
  Tests/Editor         unit tests
tools/unity.ps1        runs Unity without opening the editor
```

There are no prefabs. The scene holds Meta's camera rig, passthrough, hand tracking and MR Utility Kit blocks, plus one object named PulseDesk that builds everything else when the app starts.

## Other documents

| File | What it is |
| --- | --- |
| [PLAN.md](PLAN.md) | What is left to do before the deadline |
| [SUBMISSION.md](SUBMISSION.md) | Text for the Devpost form, the video shot list and the final checks |
| [docs/HANDOFF.md](docs/HANDOFF.md) | Log of what was done and what is still untested |
| [BUILD_GUIDE.md](BUILD_GUIDE.md), [BLUEPRINT.md](BLUEPRINT.md), [MASTERCLASS.md](MASTERCLASS.md), [AGENTS.md](AGENTS.md) | Earlier planning notes, written when this was a two person project. Kept for reference. PLAN.md replaces their schedules. |

## Third party assets

| Asset | Source | Licence |
| --- | --- | --- |
| Meta XR All-in-One SDK 209 | Meta | Meta SDK licence |
| TextMesh Pro and the Liberation Sans font | Unity package | Unity Companion Licence, SIL Open Font Licence |

All sounds are generated in code. There are no other fonts, images, models or audio files.
