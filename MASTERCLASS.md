# Masterclass: how to use this repo and the AI agents to build PulseDesk XR

Read this once, then work from PLAN.md every morning.

## 1. Who does what

| Who | Good at | Not able to |
| --- | --- | --- |
| You and your teammate | Wiring things in the Unity editor, wearing the headset, judging if it feels right, signing in to Meta, recording the video | Typing every script by hand in four days |
| Coding agent (Antigravity, Claude Code) | Writing and fixing C# scripts, docs, build scripts, Devpost text, reading compile errors | Clicking around the Unity editor (unless you give it computer use), wearing the headset, anything on your Meta account |

Rule: **the agent writes code, you wire and test it.** Every task ends with a human putting the headset on.

## 2. The files and when to use each

| File | Open it when | Edit it when |
| --- | --- | --- |
| README.md | You are new, or setting up a new machine | Setup steps change, a third-party asset is added |
| PLAN.md | Every morning, and before asking an agent for anything | A task is finished or the schedule slips. Tick things off. |
| BLUEPRINT.md | You need the reason for a design rule, a setup step, a link, or the reference C# | Rarely. Add notes if reality differs from it. |
| AGENTS.md | Never during work. The agent reads it. | A new rule is learned the hard way (e.g. "never edit Main.unity") |
| SUBMISSION.md | Tuesday evening and Wednesday | Tuesday: make the drafts match what really shipped |
| docs/HANDOFF.md | Start of every agent session, and before switching accounts | End of every session. Append, do not rewrite. |
| .gitignore / .gitattributes | Almost never | Only if Git tracks something it should not |

Make sure AGENTS.md sits in the repo root. Antigravity, Claude Code and most agents look for it there automatically.

## 3. The loop for every task

1. Pick a task ID from PLAN.md (yours are A-numbers or B-numbers).
2. Start a **fresh agent chat**. One task per chat keeps context small and quality high.
3. Give it this prompt (fill the brackets):

```
Read AGENTS.md, then docs/HANDOFF.md, then PLAN.md task [ID].
Do only that task. Edit only files I own in PLAN.md section 3.
Before using any Meta SDK API, check it with the metavr MCP / docs search.
When done, list: files changed, what I must do by hand in the Unity editor,
and how to test it on the headset or simulator.
```

4. Read the diff. Do not accept code you have not skimmed.
5. Back in Unity: let it compile. If red errors appear, paste the **exact console text** back into the same chat.
6. Do the manual editor steps the agent listed (adding a component, dragging a reference, setting a layer).
7. Test in the simulator, then on the headset if you own one.
8. Commit small and often: `git add -p`, `git commit -m "A5: spatial mapper placement"`, `git pull --rebase`, `git push`.
9. Append 3 to 5 lines to docs/HANDOFF.md: what is done, what is broken, what is next.

## 4. Prompts worth keeping

**Fix a compile error**
```
Unity 6 shows this error in [file]. Fix it without changing behaviour.
Check any Meta API against the metavr docs before changing it.
[paste the console text, including the file and line]
```

**Review before merging**
```
Review my last commit for: Unity API misuse, null references when permissions are
denied, anything that blocks the main thread, and anything that breaks the offline
fallback. List problems only; do not rewrite.
```

**Stuck on Meta SDK behaviour**
```
Search the Meta docs with metavr for [topic]. Summarise what applies to Quest 3/3S
in Unity 6 and give me the exact component names and menu paths. Do not guess.
```

**Keep Devpost honest (Tuesday)**
```
Compare SUBMISSION.md section 2 against the code in Assets/PulseDesk. Mark every
claim as built, partly built, or not built.
```

## 5. Setting up Antigravity

1. Install Antigravity from Google's site and open the repo folder in it.
2. Install the Meta tooling: `npx metavr@latest init`, then `metavr mcp install antigravity` (BLUEPRINT section 2 has the exact steps and the skills install).
3. Open an agent chat and ask "which tools do you have?". You should see the metavr tools. If not, restart Antigravity and re-run the install.
4. Point it at AGENTS.md by saying so in your first prompt of each session.

### Rotating your three accounts safely
- Switch **by hand**: sign out, sign in with the next account, in the official app. Do not use rotator tools, proxies or shared tokens. People were banned for those.
- Switch when you hit a limit, not before. Run `/usage` if the app offers it.
- Before every switch, ask the agent to update docs/HANDOFF.md, commit, then switch. The new account starts from the file, not from chat memory.
- Stagger: use account 1 for the heavy code task of the day, 2 for fixes and reviews, 3 for docs and Devpost text.
- This is staff guidance from a forum, not the written terms. If an account is flagged, stop and use another agent (Claude Code, Cursor, anything).

## 6. What the agent cannot do for you (manual checklist)

- Create the Unity project and apply Meta > Tools > Project Setup Tool fixes.
- Add components to GameObjects and drag references in the Inspector.
- Build prefabs (DeskDock, TaskCard) and set layers and colliders.
- Put on the headset: placement, reach, readability, comfort.
- Meta dashboard: Start membership, Developer Access, the `Competition` release channel, Invite URL.
- Record and upload the video. Submit the form.

Tip: ask the agent to write the manual steps as a numbered checklist with exact menu names. That is much quicker than working them out.

## 7. Traps

- **Scenes and prefabs do not merge.** Only the owner edits Main.unity and each shared prefab. The other person works in their own Dev_ scene.
- **Agents invent SDK calls.** If a method name looks too convenient, check it with metavr docs search before trusting it.
- **Do not let an agent "tidy" the repo.** Large refactors on day 3 are how builds break.
- **Never paste keystore passwords or API keys into any chat.**
- **Do a device build early** (the smoke build tonight) and again at every gate. Late first builds are where projects die.
- **Feature freeze means freeze.** After Tuesday 18:00 GMT only fixes, video and form.

## 8. The shape of the four days

| Day | Goal | Proof |
| --- | --- | --- |
| Sat (tonight) | Repo, Unity project, smoke build installed by the teammate through the Invite URL | Teammate sees the app in their headset or can install it |
| Sun | Dock placement and TaskStore working | Cards move between lanes |
| Mon | Inputs, timer, streak, goal splitter | Full loop without controllers |
| Tue | Polish, performance trace, video footage, form drafts | Feature freeze 18:00 GMT |
| Wed | Final build, video upload, submit by 15:00 GMT | Submission confirmation email |

PLAN.md has the task-level version with owners and gates.
