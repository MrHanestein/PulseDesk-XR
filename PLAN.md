# PulseDesk XR — Team Plan

Two people, about four days. All times are **GMT (UK)**. The deadline is **Wed 18 Nov, 20:00 GMT** (12:00 PT). We stop touching the build at **16:00 GMT**.

- **A is David** = has the desktop and the Quest. Owns everything that needs a headset.
- **B is teammate** = teammate. Owns everything that can be built and tested in the Unity Editor or XR Simulator.

If B also has a Quest 3/3S, B takes over "untested room" checks (§5) — a second room and a second pair of hands is the best test we have.

---

## 1. Do tonight (Sat 14 Nov) — nothing else matters until these are done

These are the items that can sink the entry regardless of how good the app is. Some depend on other people's approval queues.

| # | Task | Who | Why it is urgent |
| --- | --- | --- | --- |
| 0.1 | Apply to / confirm **Meta VR Start** membership. Check for the "Start Member" flair on the [Developer Forum](https://communityforums.atmeta.com/category/horizon-developer-forum). | A (Representative) | Rules: the person who submits must be a member *by submission time*. Approval is not instant; the FAQ talks about waiting 24 h before raising a ticket. |
| 0.2 | Both click **Join hackathon** on [Devpost](https://start-developer-competition-26.devpost.com/). | A + B | Needed to be listed on the project. |
| 0.3 | Both: Meta account with **Developer Access** enabled. A creates the developer organisation and adds B to it. | A + B | Eligibility rule 2(c); B needs dashboard access to help upload. |
| 0.4 | Create the app in the Meta developer dashboard. Create a release channel named **`Competition`**. | A | The channel name is a rule. |
| 0.5 | Create the Git repo, add `.gitignore` + `.gitattributes`, enable LFS, agree the exact Unity version, commit an empty URP project. | A | B cannot start until this exists. |
| 0.6 | **Smoke build**: passthrough + one cube, IL2CPP/ARM64, signed, uploaded to `Competition`. B opens the Invite URL from B's account and installs it. | A, then B | Proves the whole submission pipeline on day 0 instead of hour 95. |
| 0.7 | Clone, open the project, press Play in Meta XR Simulator. | B | Proves B's machine works. |
| 0.8 | Both: `metavr init`, connect the MCP server to our agents, drop the Meta skills into the repo (§6). | A + B | |
| 0.9 | Confirm with each other: was any of this code or design started **before 24 Sep 2026**? If yes we must enter *Adapted*, not *New Experience*. | A + B | Division rule. |

**Gate 0 (tonight, before sleep):** B has installed A's smoke build through the Invite URL. If this fails, fixing it is tomorrow's first job.

---

## 2. Scope — decided now so we don't argue on Tuesday

**P0 — must ship (the entry is these):**
- Passthrough scene; dock placed on the desk with three fallbacks (room scan → live depth → fixed seated position)
- Task cards: grab or ray-pinch, drop on Urgent / Next / Done
- One active sprint timer, 50% and 90% alerts, pause on headset menu
- Thumb microgestures for triage and focus
- Three-card onboarding, no walls of text
- Daily streak + saved state between sessions
- Sound for grab, snap, alert, done
- 72 fps on Quest 3S; runs with controllers never paired
- Signed ARM64 APK on `Competition`; video under 3 min; Devpost form

**P1 — ship if Gate 2 passes on time:**
- Gaze Interaction block (benefits eye-tracked devices; falls back to rays on Quest 3/3S)
- "Break it down": pick a preset goal or dictate one → three cards, using the offline rule-based splitter
- Scrollable Next queue with microgesture scroll
- Narrow-FoV pass with `metavr device fov-sim`
- "Move dock here" button

**P2 — only if everything above is solid by Tue 12:00:**
- LLM-written breakdowns (on-device small model or a cloud provider) behind the same interface
- Spoken alerts (text to speech)
- Wall summary board, high-contrast toggle, wrist badge

Cut from the original brief: the dual-machine local AI pipeline as a shipped feature (judges' headsets cannot reach our PCs), eye-tracked foveated rendering (Quest 3/3S have no eye tracking), three Urgent cards side by side (they do not fit the dock geometry — Urgent holds one card).

---

## 3. Working agreement

**Scenes and prefabs (the merge-conflict rules):**
- `Main.unity` is A's. B never saves it.
- Each of us has `Dev_A.unity` / `Dev_B.unity` for experiments.
- Everything is a prefab. B builds UI as prefabs; A drops them into `Main`.
- One owner per prefab (see README layout). To change someone else's prefab, ask, or make a prefab variant.
- Never hand-edit `.unity`, `.prefab`, `.asset` or `.meta` files, and never let an agent do it.

**Git:**
- `main` always builds. Work on `a/<topic>` and `b/<topic>` branches, small PRs, merge at least twice a day.
- Pull before opening Unity; close Unity before switching branches.
- Commit `.meta` files with their assets, always.

**Sync:** 10:00 and 21:00 GMT, 10 minutes, voice. Each gate below is checked at the 21:00 call.

**Handoff log:** append to `docs/HANDOFF.md` whenever you stop: what changed, what is broken, what is next. Our agents read this file, so it also carries context across Antigravity accounts and sessions.

**The contract between A's side and B's side** — B's `TaskStore` is the single source of truth. A's interaction code only calls it; B's UI only listens to it.

```csharp
// calls (A → store)
store.Move(taskId, TaskLane.Urgent | Next | Done);
store.MoveFocused(lane);      store.CycleFocus(+1 | -1);     store.SetFocus(taskId);
store.ToggleUserPause();      store.SetSystemPaused(bool);
store.Add(title, microStep, minutes, lane);

// events (store → everyone)
TaskAdded(TaskItem)   TaskMoved(TaskItem, previousLane)   FocusChanged(TaskItem)
Alert(message, urgent)   StatsChanged()
```

If either of us needs to change this, say so in the sync before changing it.

---

## 4. Day by day

### Sun 15 Nov — the core loop exists

| ID | A (headset side) | ID | B (editor side) |
| --- | --- | --- | --- |
| A1 | Project settings: IL2CPP, ARM64, OpenXR, hand tracking, passthrough, scene + anchor support (BLUEPRINT §5) | B1 | `TaskItem`, `TaskStore`, save/load, streak logic (BLUEPRINT §8) |
| A2 | Building Blocks: Camera Rig, Passthrough, Interactions Rig, MRUK, Effect Mesh | B2 | Edit-mode tests for `TaskStore`: move rules, one-Urgent rule, streak across days |
| A3 | `PulseDeskSpatialMapper`: table → depth → fallback placement (§6) | B3 | `TaskCard` face: title, micro-step, timer bar, lane colour + icon + label |
| A4 | `DeskDock` prefab with three lanes; `TaskCard` shell that can be grabbed and ray-pinched | B4 | Alert banner + streak counter prefabs, driven by store events |
| A5 | Card drop → `store.Move`. Timebox the ISDK snap setup to 2 h, then use the proximity fallback (§7) | B5 | Source or make 5 sounds: grab, snap, tick, alert, done. Log licences in README |

**Gate 1 (21:00):** on the headset, seated at a desk A has *not* scanned: the dock appears, a card can be grabbed and dropped on each lane, the banner and streak react. Build #2 is on `Competition`.

### Mon 16 Nov — it feels good and teaches itself

| ID | A | ID | B |
| --- | --- | --- | --- |
| A6 | `MicrogestureTriage` on both hands (§9) | B6 | Onboarding: three seeded cards, each completes when its gesture is done; one line of text each |
| A7 | Focus highlight on the focused card; store-driven moves animate the card to its lane | B7 | Sprint timer visuals, 50/90% states, paused state |
| A8 | Pause on `InputFocusLost`, resume on `InputFocusAcquired` | B8 | `GoalDecomposer` (offline) + "Break it down" panel with 4 preset goals |
| A9 | P1: Gaze Interaction block on the card and on canvases; confirm ray fallback on Quest | B9 | Hover / press feedback per design rules (§3): brighten fill, no outlines |
| A10 | "Move dock here" button → `Recenter()` | B10 | Empty states: no tasks, all done ("clear desk" moment) |

**Gate 2 (21:00):** A hands the headset to someone who has never seen the app (or records themselves cold). With controllers in another room, they finish the three onboarding cards and complete one task without being told anything. Write down every place they hesitated. **After this gate no new P1 feature is started.**

### Tue 17 Nov — voice, polish, proof

| ID | A | ID | B |
| --- | --- | --- | --- |
| A11 | P1: dictation → `GoalDecomposer` → cards. Mic denied or offline must still work via presets (§10) | B11 | Devpost description, tagline, hand-interaction answer (SUBMISSION.md) |
| A12 | FoV pass: `metavr device fov-sim enable --perceived`, fix anything clipped | B12 | Video script and shot list; title cards; music (licensed) |
| A13 | Performance trace; hold 72 fps; fix hitches | B13 | Fix list from Gate 2, UI side |
| A14 | Three-room test: different desk, coffee table, no table at all | B14 | Install each new build via Invite URL and re-run the cold test in B's own room |
| A15 | Record raw headset footage per shot list (evening) | B15 | Edit the video; subtitles; export under 3:00 |

**12:00 — P2 decision.** Only start a P2 item if Gate 2 issues are closed.
**18:00 — FEATURE FREEZE.** Bug fixes only from here.
**Gate 3 (23:00):** release-candidate APK on `Competition`, installed by B via Invite URL; video rough cut exists; Devpost draft saved (not yet submitted is fine).

### Wed 18 Nov — submit early

| GMT | What | Who |
| --- | --- | --- |
| 09:00–12:00 | Fixes for crashes and blockers only. No refactors, no visuals. | A + B |
| 12:00 | Final APK uploaded to `Competition`. Tag the commit `submission`. | A |
| 12:30 | B installs from the Invite URL on a headset or account that never had the app. Full run, controllers away. | B (or A on a second account) |
| 13:30 | Video uploaded to YouTube/Vimeo, **Public**, checked in a private window. | B |
| 14:30 | Devpost form complete; every field re-read by the other person. | A submits, B reviews |
| 15:00 | **Submitted.** | A |
| 16:00 | Hard stop. Nothing is uploaded after this. | — |
| 20:00 | Official deadline. | — |

**After submitting:** do not upload to the `Competition` channel, edit the Devpost entry, or change the video until winners are announced (around 11 Dec). Modified entries can be disqualified. The app must stay installable and free that whole time.

---

## 5. Test scripts we run at every gate

1. **Controllers away.** Controllers in another room, headset rebooted, cold launch, complete the whole loop.
2. **Unscanned room.** Clear Space Setup (or go to a room never scanned). Dock must still appear on or near the desk within ~3 s.
3. **No table.** Sit on a sofa with nothing in front. Dock appears at the fixed seated position.
4. **Airplane seat.** Elbows at your sides. Can you reach or ray everything without leaning?
5. **Interrupt.** Open the system menu mid-sprint, come back. Timer paused and resumed. Take the headset off for a minute, put it back on.
6. **Offline.** Wi-Fi off. Everything in P0 still works; voice degrades to presets with a clear message.
7. **Permissions denied.** Deny microphone and scene permission on first launch. No dead ends.
8. **Narrow view.** `metavr device fov-sim enable --perceived`, relaunch. Nothing essential is clipped. Disable afterwards.

---

## 6. Using Google Antigravity (and switching accounts)

**Can we build this in Antigravity?** Yes for the code side. Meta's CLI lists Antigravity as a supported agent target:

```bash
metavr mcp install antigravity        # adds the metavr MCP server to Antigravity
# Meta's Unity/Quest skills, written to .agents/skills/ in this repo:
npx ctx7@latest skills install /meta-quest/agentic-tools --all --universal
# alternative installer:  npx skills add meta-quest/agentic-tools
```

Then in Antigravity open **Settings → Customizations** and confirm `metavr` shows under MCP servers. Ask the agent: *"Use the metavr server's vr_docs_search tool to search for passthrough and show me the pages it returned."* If it returns doc pages, it is wired.

What the agent is good for here: C# scripts, tests, reading Meta docs through the MCP server, checking logs and traces from the headset. What it cannot do: click through Unity's Building Blocks windows or wire Inspector references. Scene and prefab work stays manual.

**What the Google AI Plus plan gets you in Antigravity.** As of this week, the pricing trackers that quote Google's plan pages all say the same thing: Plus has the *same* Antigravity quota as the free tier — a pool that refreshes **weekly** — and cannot buy top-up credits. The faster 5-hour refresh starts at Google AI Pro. So three Plus accounts amount to three free-tier weekly pools. Check the real numbers with the `/usage` command in Antigravity before planning around them.

**Switching accounts.** A Google staff member answered this on the official forum in July 2026: manually switching between paid accounts *that you personally own* to manage usage is not treated as a terms violation. What has got accounts banned (a wave in February 2026) is third-party rotators, proxies and tools that reuse Antigravity's sign-in tokens. So:

- Do it by hand: sign out of the account in Antigravity, sign in with the next one. Use only the official app.
- Do **not** install "rotator" or "multi-account" plugins or proxies, and do not automate failover. Second offences were described as permanent bans, and a ban also hit other Google developer tools on the same account.
- Use only accounts that are yours, not ones from shared or discounted pools.
- That forum reply is guidance from staff, not the written terms; the decision and the risk are yours.

**Making a switch painless** — chat history does not follow you to another account, so the repo has to carry the context:

1. Before switching, ask the agent to append a summary to `docs/HANDOFF.md` and commit.
2. After switching, start with: *"Read AGENTS.md, PLAN.md and docs/HANDOFF.md, then continue with task <ID>."*
3. Keep tasks small (one script, one test file) so a session rarely dies mid-change.

**Stretching the quota:** tab completions are unlimited on every plan; spend agent requests on the hard parts (placement logic, store rules, debugging device logs) and not on boilerplate. B using B's own account on B's tasks is the cleanest way to double capacity. If the weekly pools run dry mid-sprint, one month of Pro on a single account buys the 5-hour refresh.

---

## 7. Risk list

| Risk | Sign | Response |
| --- | --- | --- |
| Start membership not approved in time | No flair by Mon 10:00 | File a support case immediately; ask in the competition Discussions tab |
| Upload rejected (signing, manifest, package name) | Smoke build fails tonight | Fix first thing Sunday; this is why 0.6 exists |
| Dock lands in the wrong place in a new room | Fails test 2 or 3 | Ship the "Move dock here" button; bias toward the fixed seated fallback |
| ISDK snap fights scripted card moves | Cards jitter or jump back | Switch to the proximity-drop fallback (BLUEPRINT §7) |
| Voice unreliable | Dictation wrong or slow in a noisy room | Presets are the primary path; voice is a bonus |
| Scene merge conflict | Git reports a conflict in `.unity` | Owner's version wins; the other person redoes their change in a prefab |
| One of us is blocked | Stuck > 45 min | Say so in chat, swap tasks, do not sit on it |
