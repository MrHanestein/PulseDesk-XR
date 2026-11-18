# PulseDesk XR

PulseDesk XR is a task board that sits on your real desk and keeps your day moving. Built for Meta Quest 3 and 3S, used with bare hands or Touch controllers.

PulseDesk XR places a small dock on your desk with three lanes: **Next**, **Urgent** and **Done**. Each task is a card. The card in Urgent is a timed sprint. When you finish it, the next card starts by itself. You type a task once, the app reads any time out of what you wrote, and the dock reminds you when it is due.

Entry for the Meta VR Start Developer Competition 2026, Productivity track, New Experience division. Built by David Anthony.

Demo video:https://youtu.be/aloayrz1JLY

## What it does

**Finds your desk.** The dock lands on your table without any setup. It uses your room scan if you have one, live depth data if you do not, and a seated position in front of you if neither finds a surface.

**Reads times out of what you type.**

| You type | You get |
| --- | --- |
| `Call the bank at 3pm` | A card due at 15:00 |
| `Stretch in 20 min` | A card due 20 minutes from now |
| `Gym 1h 30m` | A card with a 90 minute sprint |
| `Report for 25 min by 4pm` | A 25 minute sprint, due at 16:00 |

**Reminds you.** A soft tone plays five minutes before a task is due. At the due time an alert plays and the card is highlighted. If nothing else is running, the due task starts by itself. A running sprint is never interrupted.

**Runs the sprint for you.**
- A bar drains as time runs out, a tone marks the halfway point, and the timer pulses in the last minute.
- When time is up, the alert repeats every minute until you finish or add time.
- One pinch on Done finishes the card. Three seconds later the next card starts, earliest due time first.
- One pinch on +5 adds five minutes.
- With nothing running, the minus and plus buttons set the sprint length for new cards, from 1 minute to 2 hours.

**Dims the room while you focus.** During a sprint the real room fades to grey and darkens slightly, so the dock and your desk stand out. Pausing or finishing brings the colour back. The Dim button turns this off.

**Routines.** One pinch adds three timed steps and starts the first one.

| Routine | Steps |
| --- | --- |
| Deep work 25m | 2 min clear the desk, 20 min heads down, 3 min save and note the next step |
| Rapid triage | 2 min quick replies, 5 min turn emails into cards, 2 min archive |
| Plan my day | 5 min list everything, 3 min pick three, 2 min put a time on each |
| Meeting prep | 5 min read the agenda, 5 min write your points, 2 min check audio and camera |

**Clears without losing anything.** One button clears the Done lane, or the whole board when Done is empty. For 12 seconds afterwards it becomes Undo and puts everything back. Dropping a card on it deletes that card, also with Undo.

**Shrinks while you work.** Compact mode hides everything except the running sprint and one line saying what comes next, so the dock can sit beside a laptop. It opens back up when time runs out.

**Shows your day.** The header shows open tasks, tasks done today and minutes spent in sprints. A row of pips in Done lights up as you finish tasks. The first launch of a new day clears yesterday's finished cards and shows what the last day came to.

**Remembers.** The board is saved on the headset. Closing the app, opening the system menu or taking the headset off pauses the sprint, and a pause you chose stays paused.

## Controls

Every action works with bare hands. Controllers are optional.

| Action | Hands, up close | Hands, from a distance | Hand at rest | Touch controllers |
| --- | --- | --- | --- | --- |
| Move a card | Pinch it and carry it to a lane | Point, pinch and drag | Thumb swipe left to Urgent, right to Done | Point and hold the trigger |
| Choose a card | Touch it | Point at it | Thumb swipe forward or back | Point at it |
| Finish the sprint | Poke Done | Pinch Done | Highlight it, thumb swipe right | A |
| Start the next card | Poke Start | Pinch Start | | A |
| Pause or resume | Poke the timer | Pinch the timer | Thumb tap | Y |
| Add a task | Poke Type | Pinch Type | | X |
| Compact mode | Poke Compact | Pinch Compact | | B |

Typing uses the Meta system keyboard. Where it is not available, such as in the Unity editor or over Quest Link, a keyboard built into the dock opens instead. It has shortcut keys for "in 15 min" and "for 25 min".

## Try it

The build is shared through a Meta release channel. Open the Invite URL from the Devpost page on a phone or computer signed in to your Meta account, accept it, and install PulseDesk XR from the headset's library.

On first launch, allow the spatial data permission so the app can find your desk. The app still works if you decline.

## Build from source

Requirements:
- Unity 6000.6.9f1 with Android Build Support
- Meta XR All-in-One SDK 209, added to your Unity account from the Asset Store

Steps:
1. Open this folder in Unity Hub.
2. Open `Assets/PulseDesk/Scenes/Dev_B.unity` and press Play to try the app with a mouse and keyboard. Drag cards with the mouse. Arrow keys move the highlighted card, Space pauses, B opens routines, R moves the dock.
3. Open `Assets/PulseDesk/Scenes/Main.unity` for the headset version.
4. Build with the menu **PulseDesk > 4. Build APK**. The APK is written to `Builds/PulseDeskXR.apk`.

Release builds are signed with a keystore kept outside this repository. Without it, the build is debug signed, which is fine for installing on your own headset.

With the Unity editor closed, the same steps run from a terminal:

```
powershell -File tools\unity.ps1 -Tests
powershell -File tools\unity.ps1 -Method PulseDesk.EditorTools.PulseDeskAutomation.PlaySmoke -Name smoke
powershell -File tools\unity.ps1 -Method PulseDesk.EditorTools.PulseDeskAutomation.Build -Name build
```

## How it is built

- **Engine:** Unity 6 with the Universal Render Pipeline and OpenXR.
- **Meta SDKs:** Meta XR All-in-One SDK 209 for hand tracking, thumb microgestures, passthrough and Touch controllers. Mixed Reality Utility Kit for room scan tables and live depth raycasts.
- **Desk placement:** a downward ray from the room scan, then depth rays at several angles below eye level, then a fixed seated position. If a room scan arrives late, the dock moves onto the desk. The dock is held in place by MRUK world lock or a spatial anchor.
- **Interaction:** a small layer of its own on top of Meta's hand tracking data handles pinch, ray and poke the same way for hands, controllers and the mouse in the editor.
- **Room dimming:** the passthrough layer's brightness and saturation controls, eased in and out.
- **Visuals:** the dock, cards and buttons are generated in code when the app starts, including the rounded shapes. There are no prefabs and no image assets of its own.
- **Sound:** every tone is synthesised in code. There are no audio files.
- **Logic:** all task logic lives in one class with no SDK dependencies. Reading times out of text is rule based and runs offline.
- **Tests:** 18 unit tests cover the task logic, the time reading, undo and the day rollover. A scripted play test runs the app in the editor, adds and removes tasks, checks the counts and saves screenshots.

Everything works offline and with every permission denied.

## Project layout

```
Assets/PulseDesk/
  Scripts/Core         tasks, saving, sprint timer, due times, reading typed lines
  Scripts/App          the scene component, sounds, speech check
  Scripts/Interaction  pinch, ray and poke handling
  Scripts/Input        hand tracking, thumb gestures, controllers, mouse and keyboard
  Scripts/Spatial      desk placement, room dimming
  Scripts/UI           dock, lanes, cards, buttons, keyboard
  Editor               PulseDesk menu and command line build
  Tests/Editor         unit tests
  Scenes               Main (headset) and Dev_B (editor)
tools/unity.ps1        runs Unity without opening the editor
docs/TESTING.md        how the app was tested
```

## Third party software

| Software | Licence |
| --- | --- |
| Meta XR All-in-One SDK 209 | Oculus SDK License Agreement |
| Unity, URP, Input System, TextMesh Pro | Unity Companion License |
| Liberation Sans font (via TextMesh Pro) | SIL Open Font License |

The project contains no other fonts, images, models, audio files or brand assets.
