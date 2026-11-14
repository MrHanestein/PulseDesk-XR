# Submission pack

Everything the Devpost entry needs, in the order we will do it on Wednesday. Source: [competition page](https://start-developer-competition-26.devpost.com/), [rules](https://start-developer-competition-26.devpost.com/rules), [FAQ](https://start-developer-competition-26.devpost.com/details/faqs).

## 1. What must exist

| Item | Rule | Status |
| --- | --- | --- |
| Playable build | APK on a **new** release channel named `Competition`, shared by **Invite URL**. A raw APK file is not accepted. Must stay free and available until winners are announced (~11 Dec). | ☐ |
| Video | Under 3 minutes. Footage of the project on a Meta device or XR Simulator. YouTube or Vimeo, **public**. Judges may stop watching at 3:00. | ☐ |
| Form | All fields below, in English. | ☐ |
| Submitter | The Team Representative is a Meta VR Start member with Developer Access enabled. | ☐ |

## 2. Devpost form drafts

Edit these on Tuesday so they describe what actually shipped. Do not claim a feature that is not in the build.

**Submission name:** PulseDesk XR

**Tagline (max 140 characters):**
> Your real desk becomes a priority board. Pinch, thumb-swipe or speak tasks into Urgent, Next and Done. No controllers, no menus.

**Track:** Productivity  **Division:** New Experience

**Target launch date:** `________`

**Team:** name, email, role for each of us.

**Description (suggested: under 500 words) — skeleton:**

> **Inspiration.** A to-do list is easy to write and hard to face. We wanted the three things that matter — what is urgent, what is next, what is finished — to sit on the desk in front of you as objects you can pick up, so that triage takes seconds and finishing something feels like something.
>
> **What it does.** PulseDesk XR places a small dock on your real desk with three lanes. Task cards are physical: grab one and drop it, pinch it from a distance, or keep your hand in your lap and flick your thumb. Moving a card to Urgent starts a five-minute sprint with gentle alerts at the halfway and nearly-done points. Moving it to Done plays a chime and grows your daily streak. Say a goal — or pick one — and it is broken into three small cards. `[edit to match the build]`
>
> **How we built it.** Unity 6 with the Meta XR SDK. Placement tries the room scan first (MR Utility Kit table anchors), then live depth raycasting so it works in a room that was never scanned, then a fixed seated position. Interaction SDK provides grab, ray, poke and `[gaze, if shipped]`; thumb microgestures drive triage; `[voice path as shipped]`. All task logic lives in one store with no SDK dependencies, which let two of us work in parallel — one on the headset, one in the editor. Everything essential works offline and with every permission denied. We checked the layout against the narrower field of view of Meta VR Glasses with the Meta VR CLI's FoV simulation. `[Mention AI coding tools used, honestly.]`
>
> **Challenges.** `[Two real ones — e.g. placement in unscanned rooms; making scripted moves and physical snapping agree.]`
>
> **What's next.** `[Calendar import, shared boards, wall view, more languages — only what we would really do.]`

**Hand interactions (optional field, worth filling in):**
> Every action has a hands-only path and most have three: direct grab or poke within arm's reach, ray-and-pinch at a distance (look-and-pinch on eye-tracked devices), and thumb microgestures with the hand at rest. Nothing requires a controller to be paired at any point.

## 3. Video — shot list (target 2:15, hard cap 2:55)

Rules of thumb from the judging page: show real use that honestly represents the app, lead with the story, and do not lean on AI-generated video.

| Time | Shot | What is said or shown on screen |
| --- | --- | --- |
| 0:00–0:10 | Headset view: messy to-do list feeling → dock appears on the real desk | "Your desk already knows what matters." |
| 0:10–0:30 | Hands visible, no controllers. Onboarding card 1: grab and drop to Done | Caption: *No controllers.* |
| 0:30–0:55 | Thumb swipes with the hand resting on the leg; card slides to Urgent; timer starts | Caption: *Triage without lifting your arm.* |
| 0:55–1:20 | Sprint alert at 50%; pause with thumb tap; complete; streak ticks up | |
| 1:20–1:45 | "Break it down": speak or pick a goal → three cards land in Next | |
| 1:45–2:00 | Different room, no scan: dock still lands on the table | Caption: *Works in rooms it has never seen.* |
| 2:00–2:15 | Narrow-FoV view: everything still in frame. End card: name + tagline | |

Checks before upload: no other people's faces or voices, no brand logos on the desk or screen in passthrough, no copyrighted music, captions in English, visibility set to Public (open the link in a private window to confirm).

## 4. Release channel steps

1. Meta developer dashboard → the app → **Distribution → Release Channels** → create `Competition`.
2. Upload the signed ARM64 APK (dashboard upload, Meta Quest Developer Hub, or the CLI — see [Share a build with a link](https://developers.meta.com/vr/essentials/metavr-share-builds)).
3. Version code must be higher than the previous upload.
4. In the channel, create the **Invite URL** ([how](https://developers.meta.com/horizon/resources/publish-release-channels-add-users/#invite-users-via-url)).
5. Open the URL signed in as a *different* Meta account, accept, install on a headset, launch.

## 5. Final gate — both of us tick each line

- [ ] Start membership confirmed on the submitting account
- [ ] APK is IL2CPP, ARM64 only, release-signed; `Competition` channel shows the final version code
- [ ] Invite URL installs and launches from a second account
- [ ] Cold launch with controllers in another room: full loop completed
- [ ] Unscanned room: dock appears; "no table" case works
- [ ] Wi-Fi off: P0 works; voice shows a clear fallback
- [ ] Microphone and scene permission denied: no dead end
- [ ] System menu open/close mid-sprint: timer pauses and resumes
- [ ] 72 fps held in a 15-second trace; no visible hitching when cards spawn
- [ ] Narrow-FoV simulation: nothing essential clipped (then simulation disabled)
- [ ] No brand names, logos, real names, or placeholder text in the app
- [ ] Video public, under 3:00, real footage
- [ ] Every form field filled, in English, read by the other person
- [ ] Submitted before **16:00 GMT**; commit tagged `submission`
- [ ] Nothing touched afterwards until ~11 Dec
