# PulseDesk XR

A hands-first mixed reality task board for Meta Quest 3 / 3S. Your real desk becomes three lanes — **Urgent**, **Next** and **Done** — and you move task cards between them by grabbing, pinching, thumb-swiping or speaking. No controllers.

Entry for the [Meta VR Start Developer Competition 2026](https://start-developer-competition-26.devpost.com/) — **Productivity track, New Experience division**.

> **Deadline: Wed 18 Nov 2026, 12:00 PT = 20:00 GMT.** Our own cut-off is 16:00 GMT. See [PLAN.md](PLAN.md).

## Documents

| File | What it is for |
| --- | --- |
| [BUILD_GUIDE.md](BUILD_GUIDE.md) | Step-by-step stops from empty project to submission, with what to verify at each. |
| [PLAN.md](PLAN.md) | Who does what, day by day, with gates. Start here every morning. |
| [BLUEPRINT.md](BLUEPRINT.md) | The full build guide: design rules, setup steps, code, links. |
| [SUBMISSION.md](SUBMISSION.md) | Devpost form drafts, video shot list, final checklist. |
| [AGENTS.md](AGENTS.md) | Rules the AI coding agent (Antigravity or any other) must follow in this repo. |
| `docs/HANDOFF.md` | Create this. A running log both of us (and our agents) append to. |

## The core loop (what a judge does in the first two minutes)

1. Puts the headset on while seated. The dock appears on the desk in front of them (no room scan required).
2. Three starter cards teach the three inputs: grab a card, thumb-swipe a card, pinch a button.
3. Moves a card to **Urgent** — a 5-minute sprint timer starts. Moves it to **Done** — chime, streak goes up.
4. Says or picks a goal ("prepare Friday's demo") and gets three small cards back.

## Controls

| Action | Direct | At a distance | Resting hand |
| --- | --- | --- | --- |
| Move a card | Grab it, drop it on a lane | Point (or look, on eye-tracked devices) + pinch-drag | Thumb swipe left = Urgent, right = Done |
| Change focused card | Touch it | Point / look at it | Thumb swipe forward / backward |
| Pause / resume sprint | Poke the timer | Pinch the timer | Thumb tap |
| Break a goal into steps | Poke "Break it down" | Pinch "Break it down" | — (then speak, or pick a preset) |
| Re-place the dock | Poke "Move dock here" | Pinch it | — |

Quest 3 and 3S have no eye tracking, so "look" becomes "point with the hand ray" there. That switch is automatic (see BLUEPRINT §4).

## Requirements

- **Unity 6**, `6000.0.66f2` or later. We both use the *exact same* editor version: `6000.6.9f1` (Unity 6.6; and keep `ProjectSettings/ProjectVersion.txt` committed).
- **Meta XR All-in-One SDK**, latest release (Interaction SDK v207 or newer is needed for gaze).
- **Node.js 18+** and the **Meta VR CLI** (`metavr`).
- **Git** with **Git LFS**.
- A Quest 3 or 3S in developer mode for device testing. Without one, use Meta XR Simulator.

No particular PC is required. Nothing in the shipped app depends on a local AI server.

## Getting started

```bash
# 1. Clone
git lfs install
git clone <REPO_URL> pulsedesk-xr
cd pulsedesk-xr

# 2. Meta VR CLI (pick one)
npx metavr@latest --version                                  # no install
# Windows:  iwr -useb https://developers.meta.com/horizon/install-cli/windows/ | iex
# mac/Linux: curl -fsSL https://developers.meta.com/horizon/install-cli/ | sh

metavr doctor          # lists what dev software is present / missing
metavr init            # guided setup: skills, MCP server, optional sign-in
```

3. Open the folder in Unity Hub with the pinned editor version. Let it import (first import is slow).
4. Open **Meta > Tools > Project Setup Tool** and apply every fix.
5. Open `Assets/PulseDesk/Scenes/Main.unity` (integration scene) or your own `Dev_<Name>.unity`.
6. Press Play with Meta XR Simulator, or **Build And Run** to a connected headset.

## Repo layout

```
Assets/PulseDesk/
  Scripts/
    Core/         TaskStore, TaskModels, GoalDecomposer   (no Meta SDK; own assembly, unit-tested)
    App/          PulseDeskApp (the one scene component), AudioFeedback
    Interaction/  HandInteractor, PinchTarget, PointerSource
    Input/        Meta hand tracking, microgestures, mouse/keyboard for Editor testing
    Spatial/      DockPlacer and the table finders (room scan, depth)
    UI/           DockView, DockLane, DockButton, TaskCardView, CardManager, Theme, Make
  Editor/         PulseDesk menu: setup, feature switches, build
  Tests/Editor/   TaskStore tests (Window > General > Test Runner)
  Resources/      PulseDeskSurface.mat (created by the menu)
docs/             HANDOFF.md
```

The dock, cards and buttons are built from code at runtime, so there are no prefabs to wire or merge. The scene holds only Meta's building blocks and one `PulseDesk` object. See [BUILD_GUIDE.md](BUILD_GUIDE.md).

## Building for submission

- Scripting backend **IL2CPP**, architecture **ARM64** only.
- Signed with our release keystore (not the debug key). Keystore lives outside the repo.
- Uploaded to a release channel named exactly **`Competition`**; the channel's Invite URL goes on the Devpost form.

Full steps: BLUEPRINT §12 and SUBMISSION.md.

## Team

| | Name | Role | Hardware |
| --- | --- | --- | --- |
| A | `________` | Spatial, interaction, device builds, release channel. Team Representative. | Desktop + Quest |
| B | `________` | Task logic, UI, audio, onboarding, video, Devpost write-up. | Any machine; XR Simulator |

## Third-party assets

List every asset, font, sound and package here with its licence as it is added. The rules require that we are authorised to use everything in the build, and forbid brand names, logos and identifiable people other than us.

| Asset | Source | Licence |
| --- | --- | --- |
| | | |
