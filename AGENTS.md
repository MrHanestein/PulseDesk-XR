# Agent rules for this repository

You are helping two people ship **PulseDesk XR**, a hands-first mixed reality task board for Meta Quest 3/3S (Unity 6, Meta XR SDK), before **Wed 18 Nov 2026 16:00 GMT**. Read `PLAN.md` for who owns what and `BLUEPRINT.md` for how it is built. Read `docs/HANDOFF.md` for the latest state before doing anything.

(Antigravity also reads `GEMINI.md`; if this file is not being picked up, copy it to that name.)

## Before you write Quest-specific code

- Meta's SDK changes quickly and your training data is probably out of date. **Look it up first**: use the `metavr` MCP server (`vr_docs_search`) or `metavr docs search "<topic>"`, and the skills in `.agents/skills/` (start with `hz-quest-verify-first`).
- If a class, method or menu path in `BLUEPRINT.md` does not match the installed SDK, say so and propose the fix. Do not invent an API to make code compile.
- State which SDK version you checked against.

## Hard product rules (competition requirements)

- **Hands only.** No feature may require a controller. Do not add `OVRInput` controller button reads. Every action needs at least one hand path that works on Quest 3/3S, which has **no eye tracking**.
- **Seated.** Everything interactive spawns within 0.60 m of the user. No locomotion.
- **Works offline and with permissions denied.** Network, microphone and room-scan access are optional. Every use of them needs a fallback and a timeout.
- **No API keys or secrets in the repo or in the APK.**
- **No brand names, logos, or real people** in content, sample tasks, or assets.
- Interactive targets are at least 3.5 cm at desk distance (5.2 cm at 1 m). Hover brightens the fill; never outlines, never reflows layout.
- Colour is never the only signal: lanes also carry an icon and a text label.

## Files you must not edit

- `*.unity`, `*.prefab`, `*.asset`, `*.meta`, `ProjectSettings/*`, `Packages/manifest.json` — describe the Editor steps for a human instead.
- Anything outside the current person's area in `PLAN.md` unless asked.

## Code conventions

- C# in `Assets/PulseDesk/Scripts/<Core|Spatial|Input|UI>/`. One class per file, file name = class name.
- `Core/` has **no** Meta SDK references, so it compiles and tests without a headset.
- All task state changes go through `TaskStore`. UI listens to its events; input code calls its methods. Do not keep a second copy of task state anywhere.
- `[SerializeField] private` fields for Inspector references; null-check them in `Awake` with a clear `Debug.LogError`.
- No per-frame allocations in `Update` (no LINQ, no string building every frame).
- No new packages without asking.

## How to work

- One small task at a time, named by its ID in `PLAN.md` (for example `B2`).
- Show the plan in two or three lines, then make the change.
- After changing code, list the exact Editor steps needed to wire it (which GameObject, which field).
- When you stop, append to `docs/HANDOFF.md`: date/time, task ID, what changed, what is untested, what is next.
- Commit messages: `<ID>: <what changed>`.

## Useful commands

```bash
metavr doctor                         # dev environment report
metavr device list                    # connected headsets
metavr docs search "<topic>"          # Meta documentation search
metavr asset search "<thing>"         # Meta 3D asset catalog
metavr capture screenshot             # screenshot from the headset
metavr device fov-sim enable --perceived   # narrow-FoV test; disable afterwards
metavr --help                         # everything else; check before guessing a subcommand
```
