# PulseDesk XR — Build Blueprint

Rewritten 14 Nov 2026 for a two-person team with **no fixed hardware requirement**. The earlier version assumed a BosGame M5 plus an RTX 4080 running a local AI pipeline; that is gone. Anything that needs a particular PC is now optional and dev-only (Appendix A).

How to read this: sections 1–4 are decisions, 5–12 are build steps in order, 13 is the link index. Task IDs such as `A3` refer to [PLAN.md](PLAN.md).

**A note on the code.** The scripts here were written against Meta's current documentation but have not been compiled against your installed SDK. Where a call could not be confirmed it is marked *check*. Have your agent confirm with `vr_docs_search` before relying on it.

---

## 0. What changed from the original brief

| Original | Now | Why |
| --- | --- | --- |
| Two-machine split (BosGame M5 LLM server + RTX 4080 voice server) | Removed. Ships with an offline rule-based splitter; voice and LLM are optional layers that run on the headset or in the cloud | Teammate has neither machine. More importantly, **judges' headsets cannot reach a PC on our LAN**, so that pipeline would never have run for them |
| "100% on-premise AI voice pipeline" | Presets + offline splitter always work; dictation is a bonus | Same reason; also the "take it away" guideline |
| Eyes + pinch as the primary input | Hand ray + pinch, grab, poke and microgestures are primary; gaze is an enhancement | Judges use **Quest 3 / 3S, which have no eye tracking**. Meta's Gaze Interaction block is also labelled *Experimental* |
| Enable Eye-Tracked Foveated Rendering | Fixed foveated rendering + dynamic resolution | No eye tracking on the judging hardware |
| Up to three Urgent cards in a row | Urgent holds **one** card (the active sprint) | Three 14 cm cards do not fit a lane spaced 18 cm from its neighbours |
| `Vector3 p = anchor.GetClosestSurfacePosition(pos)` | Downward physics ray against the room mesh | That method returns a `float` distance with an `out` position; the original line does not compile |
| Placement ray pitched with `Quaternion.Euler(25,0,0) * dir` | Pitched about the user's own right axis | The Euler version tilts around the world X axis, so it only works when facing world +Z |
| Wait for MRUK's scene-loaded callback, else nothing | Timeout, then depth, then fixed position | With no room scan the callback may never arrive |
| Resume timer on every `InputFocusAcquired` | Separate user-pause and system-pause flags | The original un-paused a sprint the user had paused on purpose |
| "Gaze supplies the target" for microgestures | A focused card that swipes cycle through; hover sets focus when available | Needed for Quest 3/3S |
| 32 skills incl. `hz-unity-device-readiness`, `hz-store-publish` | 29 skills are listed in the repo today; those two names are **not** among them. Use `hz-store-submit`, `hz-immersive-designer`, `hz-unity-code-review` | Checked against the repo README |
| "Judging 23 Nov – 9 Dec"; "AI video explicitly forbidden"; "five special awards" | Judging from ~18 Nov to ~9 Dec; guidance is *don't lean on* AI-generated video; six special awards | Checked against the rules page |
| Healthcare / circuit-sim rationale (DCB0129 etc.) | Dropped | Not relevant to building this |
| Red / Yellow / Green lane names | Urgent / Next / Done, each with colour **and** icon **and** label | Colour alone fails colour-blind users |

---

## 1. Competition facts (checked 14 Nov against the Devpost pages)

- **Deadline:** Wed 18 Nov 2026, 12:00 PT (20:00 GMT). No changes of any kind afterwards; modified entries may be disqualified. Winners ~11 Dec.
- **Who can enter:** 18+, eligible country, Meta account with Developer Access. The person who submits (the Representative) must be a **Meta VR Start** member by submission time. No team-size limit. One entry per team.
- **Division — New Experience:** conceived *and* built on or after 24 Sep 2026, no pre-existing codebase. Anything older belongs in *Adapted / Significantly Updated*.
- **Track — Productivity:** "task management" and "habits tied to a recurring context" are named explicitly. Prizes: $70,000 winner, $35,000 runner-up.
- **Special awards ($25,000 each):** Social & Multiplayer, Agentic Interaction, Reason to Come Back, First Five Minutes, Accessibility Forward, Boldest Original Concept. Plus two Judge's Choice at $20,000. One prize per entry (the highest it qualifies for).
- **Hard requirement:** fully usable with hands, end to end — "can someone complete the entire experience without ever pairing a controller?"
- **Guidelines (scored under Experience Design, not pass/fail):** seated, everything within a 2-foot radius; fast in and out, satisfying within 10 minutes; original, not a thin wrapper around a third-party service.
- **Judging:** Stage 1 pass/fail viability. Stage 2 four criteria at 25% each — Innovation & Creativity, Experience Design, Technical Implementation, Polish & Presentation. Called out by name: gaze interactions (ISDK v207+), hand tracking, passthrough that actually uses the room, spatial anchoring, FoV-aware design, MRUK scene understanding, "holding up in rooms the developer never tested", minimum 60 fps on Quest hardware.
- **Judges' hardware:** Quest 3 or 3S. Builds must be playable there. A headset is not required to enter; XR Simulator footage is accepted for the video.
- **Build delivery:** APK on a new release channel named `Competition`, shared by Invite URL. Free, and available until winners are announced.
- **Content rules worth remembering:** English (or subtitles); no brand names, logos or recognisable branded products; no identifiable people other than the entrants; only publicly available tools and SDKs; you must hold a licence for every third-party asset.
- **Suggested SDK:** Meta XR All-in-One SDK (the page says "v81+ (v207)").

---

## 2. Product in one page

PulseDesk XR puts a small dock on your real desk. It has three lanes. Tasks are cards you can pick up.

- **Urgent** (centre): one card. Putting a card here starts its sprint timer (default 5 minutes). Alerts at 50% and 90%.
- **Next** (left): a short stack of what is coming up, including cards made by "Break it down".
- **Done** (right): finished today. Each card dropped here plays a chime and bumps the streak.

Why this can score: the room matters (the desk *is* the interface, which is what "purposeful passthrough" asks for); the loop completes in under a minute; there is a reason to return tomorrow (streak, yesterday's Done cleared, Next still waiting); and every action has more than one hands-only route, including one with the hand at rest.

Lessons carried over from last year's winners (from the original brief; see the [winners post](https://developers.meta.com/horizon/blog/meta-horizon-start-developer-competition-meet-the-winners/)): tangible tokens plus timers anchored to real surfaces went down well; start from Meta's sample prefabs instead of writing interaction from scratch; one very reliable pinch beats many clever gestures; and do not ask an AI model to decide *where* things go in 3D — use the device's own geometry for placement and keep AI for words.

---

## 3. Design rules

**Size.** Meta's eye-targeting guidance asks for at least 3° of visual angle: size = 2 × distance × tan(1.5°). That is 5.2 cm at 1 m and 3.1 cm at 0.60 m. We use **3.5 cm minimum** for anything on the desk. Cards are 14 × 8 × 1.5 cm.

**Layout.** Lane centres on the dock's local X axis: Next −0.18 m, Urgent 0, Done +0.18 m. The dock is about 0.50 m wide, which is roughly 49° at 0.55 m — inside the 58° × 54° "perceived" view of Meta VR Glasses. Buttons sit in a single row, never a grid, so a slightly-off aim lands on nothing.

**Tilt.** Cards lying flat on a desk are read at a glancing angle. Tilt each lane's card rest about 35–45° toward the user, like a small lectern.

**Reach.** Everything spawns 0.35–0.60 m from the user horizontally.

**Colour.** Dark neutral card bodies so hover has room to brighten; never pure black or pure white.

| Lane | Body | Accent | Icon + label |
| --- | --- | --- | --- |
| Urgent | `#24272B` | crimson, about `#F2383F` | flame / "URGENT" |
| Next | `#2A2D32` | amber `#FCC934` | arrow / "NEXT" |
| Done | `#202A25` | mint `#81C995` | tick / "DONE" |

**Hover and press.** Brighten the fill and scale the *contents* slightly in place. No outline strokes (they pull the eye to the edge). Nothing appears, moves or reflows on hover — only on select. Timings from Meta's eyes guidance as quoted in the original brief (confirm on the [Eyes best practices](https://developers.meta.com/vr/design/eyes-best-practices/) page): colour on 300 ms / off 400 ms; motion on 475 ms after a 150 ms delay / off 625 ms; press 80 ms; release 100 ms.

**No head-locked HUD.** Status lives on the dock (banner along its back edge, streak beside Done). Head-locked panels fight with real hands and desks for depth.

**Text.** One line per onboarding card. Title ≤ 40 characters, micro-step ≤ 60.

---

## 4. Interaction map

| Action | Quest 3 / 3S (what judges use) | Eye-tracked devices (VR Glasses, Quest Pro) | Component |
| --- | --- | --- | --- |
| Target a card | Hand ray hover, or reach for it | Look at it | Ray interactor / `HandGazeInteractor` |
| Move a card | Grab and drop, or ray-pinch and drag | Same, plus look-and-pinch | `Grabbable` + hand grab + ray/gaze grab |
| Triage at rest | Thumb swipe left → Urgent, right → Done (acts on the **focused** card) | Same; looking at a card sets focus | `OVRMicrogestureEventSource` |
| Change focus | Thumb swipe forward / backward | Same, or just look | `TaskStore.CycleFocus` |
| Pause / resume | Thumb tap, or poke/pinch the timer | Same | |
| Press buttons | Poke, or ray-pinch | Look-and-pinch | Poke + ray/gaze on canvas |
| New tasks | Poke/pinch a preset, or dictate | Same | §10 |

How the gaze fallback works (from Meta's Gaze Interaction doc): the block tags the target `ISDK_Gaze_Fallback` and filters the ray interactors on that tag while eye tracking is active. When eye tracking is not active — always, on Quest 3/3S — the rays take over. So on the judging hardware the same objects are simply ray-interactable.

Microgesture hand pose, worth teaching in onboarding: thumb raised above the side of the index finger, other fingers loosely curled; tap or slide the thumb on the index finger's middle segment.

---

## 5. Project setup (A1, A2)

### 5.1 Tools

```bash
metavr doctor            # shows Unity editors, Android SDK, JDK, Meta tools found
metavr init              # skills + MCP + optional sign-in
metavr device list       # headset connected?
```

Unity Hub: install **Unity 6 (6000.0.66f2 or later)** with *Android Build Support*, *OpenJDK* and *Android SDK & NDK Tools*. Both teammates install the identical version.

### 5.2 Create the project

Either ask your agent to use the `hz-new-project-creation` skill, or do it by hand:

1. New project, **Universal 3D (URP)** template.
2. **File → Build Profiles → Android → Switch Platform**.
3. Install **Meta XR All-in-One SDK** (Unity Asset Store → "Add to My Assets" → Package Manager → My Assets → Install). Take the latest; Interaction SDK must be v207 or newer.
4. **Edit → Project Settings → XR Plug-in Management → Android tab → OpenXR**, and enable the Meta Quest feature group.
5. **Meta → Tools → Project Setup Tool → Fix All / Apply All** for Android. Re-run it whenever you add a feature.
6. **Player settings:** Scripting Backend **IL2CPP**; Target Architectures **ARM64** only; Color Space Linear; a package name of your own such as `com.<team>.pulsedeskxr`; version code 1.
7. **Publishing Settings:** create a release keystore. Store it and its passwords outside the repo, and give B a copy by a private channel.

### 5.3 Rig and feature settings

Open **Meta → Tools → Building Blocks** and add, in this order: **Camera Rig**, **Passthrough**, **Interactions Rig**, **MR Utility Kit**, **Effect Mesh**. Delete the default Main Camera.

On `OVRManager` (on the camera rig) — names as in recent SDKs; the Project Setup Tool will flag anything missing:

| Setting | Value | Note |
| --- | --- | --- |
| Hand Tracking Support | Controllers and Hands, or Hands Only | Never Controllers Only |
| Passthrough Support | Supported or Required | |
| Scene Support | **Supported** | Not Required — we must run without a scan |
| Anchor Support | Enabled | |
| Eye Tracking Support | **Supported** (only if using gaze) | *Required* would block Quest 3/3S |
| Microgestures | Enabled if there is a toggle in your SDK version | *check* |
| Permission requests on startup | Scene; Eye tracking if used | Microphone is requested later, when voice is first used |

On the **MRUK** component: Scene data source **Device with prefab fallback** (real room on the headset, sample rooms in the Editor); **Enable World Lock** on.
On **Effect Mesh**: colliders on; hide the mesh; leave the global mesh label unticked.

### 5.4 Editor testing

- **Meta XR Simulator** for B and for quick logic checks (`hz-xr-simulator-setup` skill).
- **Quest Link** on A's PC for Play-mode on the headset. Passthrough over Link must be switched on in the Link app's developer/beta settings.
- Depth raycasting does not behave the same in the simulator; placement must be verified on the device.

### 5.5 Scene skeleton

```
Main.unity
├─ [BuildingBlock] Camera Rig            (OVRManager; CenterEyeAnchor inside)
├─ [BuildingBlock] Passthrough
├─ [BuildingBlock] OVRInteractionComprehensive
├─ [BuildingBlock] MR Utility Kit
├─ [BuildingBlock] Effect Mesh
├─ Systems
│   ├─ TaskStore                         (B)
│   ├─ SpatialMapper                     (PulseDeskSpatialMapper + EnvironmentRaycastManager)
│   ├─ LeftHandGestures / RightHandGestures   (OVRMicrogestureEventSource)
│   └─ MicrogestureTriage
└─ (DeskDock is spawned at runtime by SpatialMapper)
```

### 5.6 One-time Git setup per machine

```bash
git lfs install
# Unity's YAML merge tool; adjust the path to your editor install
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver \
  "'C:/Program Files/Unity/Hub/Editor/<VERSION>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

It helps, but the real protection is the ownership rule in PLAN §3.

---

## 6. Placing the dock (A3)

Three attempts, in order, each with a time limit:

1. **Room scan present:** cast straight down from points 35–60 cm in front of the user and accept the first upward-facing hit on a collider that belongs to a `TABLE` anchor.
2. **No scan:** cast into the live depth data at a few downward pitches and accept a roughly horizontal hit below eye level.
3. **Nothing found** (simulator, depth not ready, no table): 50 cm forward, 35 cm below the eyes.

Then clamp to seated reach, face the dock away from the user, and anchor it. A public `Recenter()` re-runs the whole thing for the "Move dock here" button.

Create an empty `SpatialMapper` object, add **EnvironmentRaycastManager** (from MRUK v81 no separate depth manager is needed) and this script.

```csharp
// Assets/PulseDesk/Scripts/Spatial/PulseDeskSpatialMapper.cs
using System;
using System.Collections;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class PulseDeskSpatialMapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private EnvironmentRaycastManager raycastManager;
    [SerializeField] private GameObject deskDockPrefab;

    [Header("Seated reach (metres)")]
    [SerializeField] private float minReach = 0.35f;
    [SerializeField] private float maxReach = 0.60f;
    [SerializeField] private float fallbackForward = 0.50f;
    [SerializeField] private float fallbackDrop = 0.35f;

    [Header("Time limits (seconds)")]
    [SerializeField] private float sceneWait = 2.5f;
    [SerializeField] private float depthWait = 2.0f;

    public Transform Dock { get; private set; }
    public event Action<Transform> DockPlaced;

    private static readonly float[] SampleDistances = { 0.50f, 0.45f, 0.55f, 0.40f, 0.60f, 0.35f };
    private static readonly float[] SamplePitches = { 38f, 30f, 48f };

    private bool _sceneLoaded;
    private GameObject _anchorRoot;
    private Coroutine _routine;

    private void Start()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RegisterSceneLoadedCallback(() => _sceneLoaded = true);
        }
        _routine = StartCoroutine(PlaceRoutine());
    }

    /// <summary>Hook this to the "Move dock here" button.</summary>
    public void Recenter()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlaceRoutine());
    }

    private IEnumerator PlaceRoutine()
    {
        // 1. Give the room scan a moment to load. It may never load; that is fine.
        float waited = 0f;
        while (!_sceneLoaded && waited < sceneWait)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        yield return null; // one frame so Effect Mesh colliders exist

        Vector3 point;
        if (_sceneLoaded && TryTableFromScene(out point))
        {
            Place(point);
            yield break;
        }

        // 2. Live depth. It can take a second to become ready, so retry.
        waited = 0f;
        while (waited < depthWait)
        {
            if (TryTableFromDepth(out point))
            {
                Place(point);
                yield break;
            }
            waited += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }

        // 3. Fixed seated position.
        Place(centerEyeAnchor.position + ForwardFlat() * fallbackForward + Vector3.down * fallbackDrop);
    }

    private Vector3 ForwardFlat()
    {
        Vector3 f = Vector3.ProjectOnPlane(centerEyeAnchor.forward, Vector3.up);
        if (f.sqrMagnitude < 0.01f)
        {
            // Looking straight down: the head's "up" now points forward.
            f = Vector3.ProjectOnPlane(centerEyeAnchor.up, Vector3.up);
        }
        return f.normalized;
    }

    private bool TryTableFromScene(out Vector3 point)
    {
        point = default;
        Vector3 eye = centerEyeAnchor.position;
        Vector3 fwd = ForwardFlat();

        foreach (float d in SampleDistances)
        {
            Vector3 origin = eye + fwd * d;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            foreach (RaycastHit h in hits)
            {
                if (h.normal.y < 0.9f || h.distance < 0.15f || h.distance >= best) continue;
                MRUKAnchor anchor = h.collider.GetComponentInParent<MRUKAnchor>();
                if (anchor == null || !anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE)) continue;
                best = h.distance;
                point = h.point;
                found = true;
            }
            if (found) return true;
        }
        return false;
    }

    private bool TryTableFromDepth(out Vector3 point)
    {
        point = default;
        if (raycastManager == null || !EnvironmentRaycastManager.IsSupported) return false;

        Vector3 eye = centerEyeAnchor.position;
        Vector3 fwd = ForwardFlat();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        foreach (float pitch in SamplePitches)
        {
            Vector3 dir = Quaternion.AngleAxis(pitch, right) * fwd; // tilt down about the user's own right axis
            if (!raycastManager.Raycast(new Ray(eye, dir), out EnvironmentRaycastHit hit, 1.5f)) continue;
            if (hit.normal.y < 0.8f) continue;                      // not a horizontal surface
            float drop = eye.y - hit.point.y;
            if (drop < 0.15f || drop > 1.0f) continue;              // not plausibly a desk or lap tray
            point = hit.point;
            return true;
        }
        return false;
    }

    private void Place(Vector3 surfacePoint)
    {
        Vector3 eye = centerEyeAnchor.position;

        // Keep the dock inside seated reach, at the height we found.
        Vector3 flat = new Vector3(surfacePoint.x - eye.x, 0f, surfacePoint.z - eye.z);
        Vector3 flatDir = flat.sqrMagnitude > 0.0001f ? flat.normalized : ForwardFlat();
        float dist = Mathf.Clamp(flat.magnitude, minReach, maxReach);
        Vector3 pos = new Vector3(eye.x + flatDir.x * dist, surfacePoint.y + 0.01f, eye.z + flatDir.z * dist);
        Quaternion rot = Quaternion.LookRotation(flatDir, Vector3.up); // dock +Z points away from the user

        if (Dock == null) Dock = Instantiate(deskDockPrefab).transform;

        // A fresh anchor object each time: an OVRSpatialAnchor owns its transform once created.
        Dock.SetParent(null, true);
        if (_anchorRoot != null) Destroy(_anchorRoot);
        _anchorRoot = new GameObject("DockAnchor");
        _anchorRoot.transform.SetPositionAndRotation(pos, rot);
        Dock.SetParent(_anchorRoot.transform, false);
        Dock.localPosition = Vector3.zero;
        Dock.localRotation = Quaternion.identity;

        // With MRUK world lock active the room keeps content stable; otherwise anchor it ourselves.
        if (MRUK.Instance == null || !MRUK.Instance.IsWorldLockActive)
        {
            _anchorRoot.AddComponent<OVRSpatialAnchor>();
        }

        DockPlaced?.Invoke(Dock);
    }
}
```

Notes:
- Confirmed against Meta's docs today: `EnvironmentRaycastManager.Raycast(Ray, out EnvironmentRaycastHit, float)`, `IsSupported`, hit fields `point` / `normal`, `MRUK.Instance.IsWorldLockActive`, `MRUKAnchor.HasAnyLabel(SceneLabels)`. Rays outside the depth camera's view return no hit, which is why several pitches are tried.
- `PlaceBox(ray, size, upwards, out hit)` also exists and checks that a box fits. Meta's examples use it for upright panels; for a flat dock a plain raycast is the safer start. Try `PlaceBox` later if the dock ever lands half-off a table edge.
- If the clamp pulls the dock in from a far table edge it may float just in front of the table at table height. That reads fine in practice and keeps it reachable.
- Cards must be children of the dock (or follow it) so they move when it is re-placed.
- Optional wall board (P2): `room.GetKeyWall(out Vector2 size)` gives the longest unobstructed wall; place a world-space canvas at `wall.GetAnchorCenter()` and check on the device which way it faces.

**Test on the headset before moving on:** scanned room; Space Setup cleared; coffee table; sofa with nothing in front.

---

## 7. Dock, lanes and cards (A4, A5)

### 7.1 `DeskDock` prefab

```
DeskDock                         (root; +Z away from user)
├─ Lane_Next    (x −0.18)        DockLane(lane = Next)    + ring/plate visual + icon + label
├─ Lane_Urgent  (x  0.00)        DockLane(lane = Urgent)
├─ Lane_Done    (x +0.18)        DockLane(lane = Done)
├─ Banner                        (B's prefab; back edge, tilted up)
├─ Streak                        (B's prefab; beside Done)
└─ Buttons row                   "Break it down" · "Move dock here"   (each ≥ 3.5 cm)
```

Each lane has a child `Slot` transform tilted 35–45° toward the user; that is where cards rest. Next and Done stack cards 1.8 cm apart along the slot's away-axis so the title strip of each card peeks out; show the top four and a "+N" badge.

### 7.2 `TaskCard` prefab

Root: `BoxCollider` (0.14 × 0.08 × 0.015), kinematic `Rigidbody` with gravity off, `Grabbable`, `TaskCardView` (B), `CardMover` (below).

Add interactions with Meta's wizards (right-click the object → **Interaction SDK** quick actions, or the Building Blocks window):
- **Add Grab Interaction** → hand grab.
- **Add Ray Grab Interaction** → pinch from a distance. This is the main distance path on Quest 3/3S.
- P1: **Gaze Interaction** block, *Grab Object* variant, with ray fallback left on (§9.2).

### 7.3 Dropping a card on a lane — try ISDK snap first, for two hours at most

Meta's snap system is "inside out" compared with what you might expect: the **`SnapInteractor` goes on the thing that moves** (a child of the card, with its *Pointable Element* set to the card's `Grabbable`), and the **`SnapInteractable` goes on each lane**. Both need a rigidbody to detect each other. Follow Meta's [snap tutorial](https://developers.meta.com/vr/documentation/unity/unity-isdk-create-snap-interactions/) for the exact collider and rigidbody settings (the original brief quotes a shared kinematic `SnapLocationRigidBody` volume and a `ButtonRing` visual from that page).

Tell the store when a card snaps:

```csharp
// Assets/PulseDesk/Scripts/Spatial/DockLane.cs
using Oculus.Interaction;
using UnityEngine;

public class DockLane : MonoBehaviour
{
    public TaskLane lane;
    public Transform slot;
    [SerializeField] private SnapInteractable snap;   // leave empty if using the proximity fallback
    [SerializeField] private TaskStore store;

    private void OnEnable()
    {
        if (snap != null) snap.WhenSelectingInteractorViewAdded += OnSnapped;
    }

    private void OnDisable()
    {
        if (snap != null) snap.WhenSelectingInteractorViewAdded -= OnSnapped;
    }

    private void OnSnapped(IInteractorView view)
    {
        Component c = view as Component;
        TaskCardView card = c != null ? c.GetComponentInParent<TaskCardView>() : null;
        if (card != null) store.Move(card.TaskId, lane);
    }

    public void Accept(TaskCardView card) => store.Move(card.TaskId, lane);
}
```

*Check* `WhenSelectingInteractorViewAdded` against your ISDK version.

**The known trouble spot:** microgestures move cards from code, while snap wants to own a snapped card's pose. If cards jitter, jump back, or refuse to leave a lane, stop and use the fallback below. It is twenty lines, it cannot fight anything, and to a judge it looks identical.

### 7.4 Proximity fallback (no snap components)

```csharp
// Assets/PulseDesk/Scripts/Spatial/CardDrop.cs
using System.Collections;
using Oculus.Interaction;
using UnityEngine;

public class CardDrop : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private TaskCardView card;
    [SerializeField] private CardMover mover;
    [SerializeField] private float snapRadius = 0.12f;

    private void OnEnable()  { grabbable.WhenPointerEventRaised += OnPointer; }
    private void OnDisable() { grabbable.WhenPointerEventRaised -= OnPointer; }

    private void OnPointer(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Unselect) StartCoroutine(CheckDropNextFrame());
    }

    private IEnumerator CheckDropNextFrame()
    {
        yield return null;                                  // let the grab state settle
        if (grabbable.SelectingPointsCount > 0) yield break; // still held by the other hand

        DockLane nearest = null;
        float best = snapRadius;
        foreach (DockLane l in FindObjectsByType<DockLane>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(transform.position, l.slot.position);
            if (d < best) { best = d; nearest = l; }
        }

        if (nearest != null) nearest.Accept(card);   // store.Move → TaskMoved → CardMover.GoTo
        else mover.ReturnHome();                     // dropped in mid-air: glide back
    }
}
```

### 7.5 Moving cards from code

Whatever caused a move — a drop, a swipe, a voice command — the store raises `TaskMoved`, and each card glides to its lane's slot unless it is being held.

```csharp
// Assets/PulseDesk/Scripts/Spatial/CardMover.cs
using Oculus.Interaction;
using UnityEngine;

public class CardMover : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private float sharpness = 12f;

    private Transform _slot;
    private Vector3 _localOffset;

    public void GoTo(Transform slot, Vector3 localOffset)
    {
        _slot = slot;
        _localOffset = localOffset;   // stack offset within the lane
    }

    public void ReturnHome() { /* nothing to do: Update keeps pulling toward the current slot */ }

    private void Update()
    {
        if (_slot == null || grabbable.SelectingPointsCount > 0) return;
        float k = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, _slot.TransformPoint(_localOffset), k);
        transform.rotation = Quaternion.Slerp(transform.rotation, _slot.rotation, k);
    }
}
```

A small `CardSpawner` (A) owns the glue: on `Start` it creates a card for every item in `store.Tasks`; on `TaskAdded` it creates one more; on `TaskMoved` it recomputes stack offsets for the affected lanes and calls `GoTo`.

**Gate 1 check:** grab → drop on each lane → banner and streak react; drop in mid-air → card glides home.

---

## 8. Task logic (B1, B2)

No Meta SDK types in here, so it compiles and tests on any machine.

```csharp
// Assets/PulseDesk/Scripts/Core/TaskStore.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum TaskLane { Next = 0, Urgent = 1, Done = 2 }

[Serializable]
public class TaskItem
{
    public string id;
    public string title;
    public string microStep;
    public TaskLane lane = TaskLane.Next;
    public float durationSeconds = 300f;
    public float remainingSeconds = 300f;
    public bool alert50;
    public bool alert90;
}

[Serializable]
public class PulseDeskSave
{
    public List<TaskItem> tasks = new List<TaskItem>();
    public int dayStreak;
    public int doneToday;
    public string lastDoneDate = "";
    public bool onboardingDone;
}

public class TaskStore : MonoBehaviour
{
    public event Action<TaskItem> TaskAdded;
    public event Action<TaskItem, TaskLane> TaskMoved;   // item, lane it came from
    public event Action<TaskItem> FocusChanged;          // may be null
    public event Action<string, bool> Alert;             // message, urgent
    public event Action StatsChanged;

    public IReadOnlyList<TaskItem> Tasks => _save.tasks;
    public TaskItem Focused { get; private set; }
    public int DayStreak => _save.dayStreak;
    public int DoneToday => _save.doneToday;
    public bool OnboardingDone => _save.onboardingDone;
    public bool IsPaused => _userPaused || _systemPaused;

    private const string DateFormat = "yyyy-MM-dd";
    private PulseDeskSave _save = new PulseDeskSave();
    private bool _userPaused;
    private bool _systemPaused;

    private string SavePath => Path.Combine(Application.persistentDataPath, "pulsedesk.json");

    // Loads in Awake so every other script can read Tasks in its Start.
    private void Awake()
    {
        Load();
        if (_save.tasks.Count == 0 && !_save.onboardingDone) SeedOnboarding();
        Focused = FirstOpen();
    }

    private void Update()
    {
        if (IsPaused) return;
        TaskItem t = ActiveSprint();
        if (t == null || t.remainingSeconds <= 0f) return;

        t.remainingSeconds = Mathf.Max(0f, t.remainingSeconds - Time.deltaTime);
        float progress = 1f - t.remainingSeconds / Mathf.Max(1f, t.durationSeconds);

        if (!t.alert50 && progress >= 0.5f)
        {
            t.alert50 = true;
            Alert?.Invoke("Halfway: " + t.title, false);
        }
        if (!t.alert90 && progress >= 0.9f)
        {
            t.alert90 = true;
            Alert?.Invoke("Nearly out of time: " + t.title, true);
        }
    }

    // ---------- queries ----------

    public TaskItem Get(string id)
    {
        for (int i = 0; i < _save.tasks.Count; i++)
            if (_save.tasks[i].id == id) return _save.tasks[i];
        return null;
    }

    public TaskItem ActiveSprint()
    {
        for (int i = 0; i < _save.tasks.Count; i++)
            if (_save.tasks[i].lane == TaskLane.Urgent) return _save.tasks[i];
        return null;
    }

    private TaskItem FirstOpen()
    {
        for (int i = 0; i < _save.tasks.Count; i++)
            if (_save.tasks[i].lane != TaskLane.Done) return _save.tasks[i];
        return null;
    }

    // ---------- commands ----------

    public TaskItem Add(string title, string microStep = "", float minutes = 5f, TaskLane lane = TaskLane.Next)
    {
        var t = new TaskItem
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 8),
            title = title,
            microStep = microStep,
            lane = TaskLane.Next,
            durationSeconds = minutes * 60f,
            remainingSeconds = minutes * 60f
        };
        _save.tasks.Add(t);
        TaskAdded?.Invoke(t);
        if (lane != TaskLane.Next) Move(t.id, lane);
        if (Focused == null) SetFocus(t.id);
        Save();
        return t;
    }

    public void Move(string id, TaskLane to)
    {
        TaskItem t = Get(id);
        if (t == null || t.lane == to) return;

        if (to == TaskLane.Urgent)
        {
            // Only one sprint at a time: the current one steps back to Next.
            TaskItem current = ActiveSprint();
            if (current != null)
            {
                current.lane = TaskLane.Next;
                TaskMoved?.Invoke(current, TaskLane.Urgent);
            }
            if (t.remainingSeconds <= 0f)
            {
                t.remainingSeconds = t.durationSeconds;
                t.alert50 = false;
                t.alert90 = false;
            }
            _userPaused = false;
        }

        TaskLane from = t.lane;
        t.lane = to;
        TaskMoved?.Invoke(t, from);

        if (to == TaskLane.Done)
        {
            RegisterCompletion();
            if (Focused == t) CycleFocus(1);
        }
        else if (to == TaskLane.Urgent)
        {
            Alert?.Invoke("Sprint started: " + t.title, false);
        }
        Save();
    }

    public void MoveFocused(TaskLane to)
    {
        if (Focused != null) Move(Focused.id, to);
    }

    public void SetFocus(string id)
    {
        TaskItem t = Get(id);
        if (t == Focused) return;
        Focused = t;
        FocusChanged?.Invoke(Focused);
    }

    /// <summary>dir = +1 next, -1 previous, among tasks that are not Done.</summary>
    public void CycleFocus(int dir)
    {
        List<TaskItem> open = _save.tasks.FindAll(x => x.lane != TaskLane.Done);
        if (open.Count == 0)
        {
            Focused = null;
            FocusChanged?.Invoke(null);
            return;
        }
        int i = Focused == null ? -1 : open.IndexOf(Focused);
        i = i < 0 ? 0 : ((i + dir) % open.Count + open.Count) % open.Count;
        Focused = open[i];
        FocusChanged?.Invoke(Focused);
    }

    public void ToggleUserPause()
    {
        _userPaused = !_userPaused;
        Alert?.Invoke(_userPaused ? "Sprint paused" : "Sprint running", false);
    }

    /// <summary>Called when the system menu opens/closes or the headset comes off.</summary>
    public void SetSystemPaused(bool paused)
    {
        _systemPaused = paused;
        if (paused) Save();
    }

    public void MarkOnboardingDone()
    {
        _save.onboardingDone = true;
        Save();
    }

    // ---------- streak ----------

    private void RegisterCompletion()
    {
        string today = DateTime.Now.ToString(DateFormat);
        string yesterday = DateTime.Now.AddDays(-1).ToString(DateFormat);

        if (_save.lastDoneDate == today)
        {
            _save.doneToday++;
        }
        else
        {
            _save.dayStreak = _save.lastDoneDate == yesterday ? _save.dayStreak + 1 : 1;
            _save.doneToday = 1;
        }
        _save.lastDoneDate = today;
        StatsChanged?.Invoke();
        Alert?.Invoke("Done. " + _save.doneToday + " today, day " + _save.dayStreak + " of your streak", false);
    }

    // ---------- persistence ----------

    private void Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                PulseDeskSave loaded = JsonUtility.FromJson<PulseDeskSave>(File.ReadAllText(SavePath));
                if (loaded != null) _save = loaded;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("PulseDesk: could not read save, starting fresh. " + e.Message);
            _save = new PulseDeskSave();
        }

        // New day: clear yesterday's Done pile, reset today's count, break the streak if a day was missed.
        string today = DateTime.Now.ToString(DateFormat);
        string yesterday = DateTime.Now.AddDays(-1).ToString(DateFormat);
        if (_save.lastDoneDate != today)
        {
            _save.tasks.RemoveAll(x => x.lane == TaskLane.Done);
            _save.doneToday = 0;
            if (_save.lastDoneDate != yesterday) _save.dayStreak = 0;
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_save));
        }
        catch (Exception e)
        {
            Debug.LogWarning("PulseDesk: could not save. " + e.Message);
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Save();
    }

    private void SeedOnboarding()
    {
        _save.tasks.Add(new TaskItem { id = "onb1", title = "Pick me up", microStep = "Grab this card and drop it on DONE", lane = TaskLane.Urgent, durationSeconds = 180f, remainingSeconds = 180f });
        _save.tasks.Add(new TaskItem { id = "onb2", title = "Flick your thumb", microStep = "Swipe your thumb left along your finger", lane = TaskLane.Next });
        _save.tasks.Add(new TaskItem { id = "onb3", title = "Make it small", microStep = "Pinch \"Break it down\" and pick a goal", lane = TaskLane.Next });
    }
}
```

Tests B should write (`B2`, Unity Test Runner, Edit Mode): only one Urgent at a time; moving to Done increments `DoneToday`; a completion the day after continues the streak and two days after resets it (inject the date or temporarily make the date source overridable); focus skips Done cards; save → load round-trips.

UI pieces that listen to the store (B3, B4): `TaskCardView` (exposes `string TaskId`, shows title / micro-step / timer bar / lane colour-icon-label, highlights when `FocusChanged` names it), `AlertBanner`, `StreakCounter`.

---

## 9. Microgestures, gaze, scrolling (A6–A9)

### 9.1 Microgesture triage

Create `LeftHandGestures` and `RightHandGestures`, add **OVRMicrogestureEventSource** to each, and assign the matching **OVRHand** from the camera rig to its *Hand* field. An empty *Hand* field throws every frame. Needs Meta XR Core SDK v74+. Meta ships a `HandMicrogestures` sample scene in the Core SDK samples — run it once on the headset to learn the motion.

```csharp
// Assets/PulseDesk/Scripts/Input/MicrogestureTriage.cs
using UnityEngine;

public class MicrogestureTriage : MonoBehaviour
{
    [SerializeField] private OVRMicrogestureEventSource leftHand;
    [SerializeField] private OVRMicrogestureEventSource rightHand;
    [SerializeField] private TaskStore store;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tick;

    private void OnEnable()
    {
        if (leftHand != null) leftHand.GestureRecognizedEvent.AddListener(OnGesture);
        if (rightHand != null) rightHand.GestureRecognizedEvent.AddListener(OnGesture);
        OVRManager.InputFocusLost += OnFocusLost;
        OVRManager.InputFocusAcquired += OnFocusAcquired;
    }

    private void OnDisable()
    {
        if (leftHand != null) leftHand.GestureRecognizedEvent.RemoveListener(OnGesture);
        if (rightHand != null) rightHand.GestureRecognizedEvent.RemoveListener(OnGesture);
        OVRManager.InputFocusLost -= OnFocusLost;
        OVRManager.InputFocusAcquired -= OnFocusAcquired;
    }

    private void OnGesture(OVRHand.MicrogestureType gesture)
    {
        switch (gesture)
        {
            case OVRHand.MicrogestureType.SwipeLeft:     store.MoveFocused(TaskLane.Urgent); break;
            case OVRHand.MicrogestureType.SwipeRight:    store.MoveFocused(TaskLane.Done);   break;
            case OVRHand.MicrogestureType.SwipeForward:  store.CycleFocus(-1);               break;
            case OVRHand.MicrogestureType.SwipeBackward: store.CycleFocus(1);                break;
            case OVRHand.MicrogestureType.ThumbTap:      store.ToggleUserPause();            break;
            default: return;
        }
        if (audioSource != null && tick != null) audioSource.PlayOneShot(tick);
    }

    // System menu opened / headset removed: pause. Coming back does not override a pause the user chose.
    private void OnFocusLost()     { store.SetSystemPaused(true); }
    private void OnFocusAcquired() { store.SetSystemPaused(false); }
}
```

Left and right swipes are defined by the system per hand (the physical direction is mirrored between hands); the events arrive already labelled, so no per-hand logic is needed.

Hover → focus: on the card, when a ray or gaze interactor starts hovering, call `store.SetFocus(card.TaskId)`. An `InteractableUnityEventWrapper` on the card's ray/gaze interactable with *When Hover* wired to a small method on `TaskCardView` does it without code in the interaction layer.

### 9.2 Gaze (P1)

Per Meta's [Gaze Interaction block doc](https://developers.meta.com/vr/documentation/unity/unity-isdk-gaze-building-blocks/) (ISDK v207+, labelled Experimental):

1. Select the `TaskCard` prefab root → Building Blocks → **Gaze Interaction** → variant **Grab Object** → keep *ray fallback* on.
2. Select each world-space canvas (buttons, Next queue) → **Gaze Interaction** → variant **UI Canvas**.
3. Check each `HandGazeInteractor` (one per hand, under the interaction rig): its *Candidate Provider* must reference the `GazeConecaster`. If the scene already had an eye-gaze object without a conecaster child, this field is empty and the interactor fails at start.
4. On Quest 3/3S confirm that pinch-from-a-distance still works on the same objects. That is the fallback doing its job.

Canvas rules from Meta's gaze-UI page as quoted in the original brief: use `RectMask2D` (not `Mask`) for clipped areas and keep rects unrotated relative to the canvas; turn on the canvas's debug visuals and make sure targets draw as valid. Confirm on the [Gaze UI page](https://developers.meta.com/vr/documentation/unity/unity-isdk-gaze-ui/).

### 9.3 Scrolling the Next queue (P1)

Meta provides a `MicrogestureScroll` prefab (Interaction SDK → Runtime → Prefabs → Microgestures) that scrolls a `ScrollRect` you are pointing or looking at. From the original brief and Meta's [how-to](https://developers.meta.com/vr/documentation/unity/unity-isdk-gaze-microgesture-scroll/): drag it **directly onto** the interactor in the Hierarchy so it auto-wires (re-parenting later leaves its interactor field empty), then set its `OVRMicrogestureEventSource` *Hand*. If the stack-with-badge layout from §7.1 is enough, skip this.

---

## 10. "Break it down" and voice (B8, A11)

The feature has three layers. Each one works without the one above it.

**Layer 1 — presets + offline splitter (P1, always available).** The "Break it down" button opens a row of four preset goals ("Plan my day", "Clear my inbox", "Prepare a presentation", "Tidy my workspace") with hand-written three-step breakdowns. Free-text goals go through this:

```csharp
// Assets/PulseDesk/Scripts/Core/GoalDecomposer.cs
using System;
using System.Collections.Generic;

public static class GoalDecomposer
{
    private static readonly string[] Separators =
        { " and then ", " then ", ", and ", " and ", ", ", "; " };

    public static List<string> Split(string goal, int max = 3)
    {
        var parts = new List<string>();
        if (string.IsNullOrWhiteSpace(goal)) return parts;

        goal = goal.Trim().TrimEnd('.', '!', '?');
        foreach (string piece in goal.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            string s = piece.Trim();
            if (s.Length > 2) parts.Add(char.ToUpper(s[0]) + s.Substring(1));
        }

        if (parts.Count < 2)
        {
            parts.Clear();
            parts.Add("Write down what done looks like for: " + goal);
            parts.Add("Do the first five minutes of: " + goal);
            parts.Add("Check it and close out: " + goal);
        }

        if (parts.Count > max) parts.RemoveRange(max, parts.Count - max);
        return parts;
    }
}
```

Each result becomes `store.Add(text, "", 5f, TaskLane.Next)`; the cards fly out of the button into the Next lane one after another (150 ms apart) — that animation is the "agent placed things on my desk" moment.

**Layer 2 — dictation (P1).** Meta's Building Blocks include voice blocks from the Voice SDK (dictation and text-to-speech, listed since v65) and newer **AI Building Blocks** (v83+: Speech to Text, Text to Speech, LLM, Object Detection). Both speech routes use cloud services and need internet, and the AI blocks' speech providers need an API key. Wire whichever you choose so that its transcript string goes to `GoalDecomposer.Split`. Rules:
- Ask for the microphone permission only when the user first presses the mic.
- Show state on the button: listening / thinking / done.
- 5-second timeout, permission denied, or no network → show "Voice unavailable — pick a goal" and open the presets. Never a spinner that does not end.
- Do not put a private API key in the APK. A key in an APK can be extracted by anyone who downloads it.

**Layer 3 — LLM breakdown (P2).** Put the splitter behind an interface (`IGoalDecomposer { Task<List<string>> Split(string goal); }`) so an LLM version can be swapped in. Options:
- **On-device small model** through the AI Building Blocks' Unity Inference Engine provider (v85+ lists SmolLM, Qwen and Phi). No server, no key, works for judges. Costs APK size, memory and a noticeable pause; measure frame rate while it runs.
- **Cloud model** through a provider, called via your own small relay endpoint that holds the key and rate-limits. Works for judges while it stays up; must stay up until ~11 Dec.

Prompt shape: ask for exactly three steps, each under 60 characters, as a JSON array of strings; validate; on any failure or after 4 seconds fall back to Layer 1.

For the **Agentic Interaction** award, be honest in the write-up about which layer is doing the work. A rule-based splitter is not an AI agent; an on-device model that reads the goal plus the current board and decides lane and duration for each new card is much closer.

---

## 11. Performance, field of view, testing (A12–A14)

**Performance.** Target 72 fps on Quest 3S; the judging floor is 60 fps ([VRC](https://developers.meta.com/horizon/resources/vrc-quest-performance-1/#criteria)). This scene is light — passthrough, a dozen cards, a few canvases — so the usual culprits are: too many separate world-space canvases (merge static ones), TextMeshPro rebuilding every frame (update the timer text once a second, not every frame), transparent overdraw, realtime shadows (turn them off), and MSAA above 4×. Use fixed foveated rendering and dynamic resolution if needed.

```bash
metavr perf --help                 # confirm the capture subcommand and flags for your CLI version
# the original brief used:
metavr perf capture --mode full --duration 15000 --app <your.package.name> --output pulsedesk_trace
```

Ask the agent to analyse the trace with the `hz-perfetto-debug` skill. See [Profile and debug performance](https://developers.meta.com/vr/essentials/metavr-performance).

**Field of view.** Meta VR Glasses: 70° × 66° nominal, 58° × 54° "perceived" (p90). Simulate on a Quest:

```bash
metavr device fov-sim enable --perceived     # then relaunch the app and look
metavr capture screenshot
metavr device fov-sim disable                # do not forget; a reboot also clears it
```

If nothing changes, take the headset off and on. Judge the result *in the headset*; screenshots do not reproduce the lens view. Horizon OS v209+ is needed for compositor layers to be clipped.

**Reviews by agent.** `hz-immersive-designer` (comfort, layout, accessibility), `hz-unity-code-review` (allocations, input, VR pitfalls), `hz-unity-meta-quest-ui` and `hz-unity-tmp-resources` (canvas and font setup), `hz-vr-debug` (device logs).

**Manual test scripts** are in PLAN §5. Run all eight at every gate.

---

## 12. Build and submit

1. **Build:** IL2CPP, ARM64 only, release keystore, development build **off**, version code incremented.
2. **Verify the APK:** open it as a zip (or Android Studio → Analyze APK). `lib/` must contain only `arm64-v8a`.
3. **Upload** to the `Competition` channel — dashboard, Meta Quest Developer Hub, or CLI ([Share a build with a link](https://developers.meta.com/vr/essentials/metavr-share-builds)). The `hz-store-submit` skill walks through build validation.
4. **Invite URL:** create it in the channel ([instructions](https://developers.meta.com/horizon/resources/publish-release-channels-add-users/#invite-users-via-url)); install from a second account.
5. **Video + form:** [SUBMISSION.md](SUBMISSION.md).
6. **Then leave it alone** until ~11 Dec.

---

## 13. Links

### Checked today (14 Nov 2026)

| Topic | Link | Find on page |
| --- | --- | --- |
| Competition overview | https://start-developer-competition-26.devpost.com/ | `Hard Requirements`, `WHAT TO SUBMIT` |
| Official rules | https://start-developer-competition-26.devpost.com/rules | `ENTRY REQUIREMENTS`, `JUDGING CRITERIA` |
| Judging criteria | https://start-developer-competition-26.devpost.com/details/judging | `rooms the developer never tested` |
| FAQ | https://start-developer-competition-26.devpost.com/details/faqs | `Quest 3 or Quest 3S`, `Start Member` |
| Install Meta VR CLI | https://developers.meta.com/vr/essentials/metavr-install/ | `metavr mcp install`, `antigravity`, `vr_docs_search` |
| Meta agentic tools (skills) | https://github.com/meta-quest/agentic-tools | `Skills`, `npx ctx7`, `Google Antigravity` |
| FoV values and simulation | https://developers.meta.com/vr/essentials/field-of-view | `fov-sim`, `--perceived` |
| Environment raycasting | https://developers.meta.com/vr/documentation/unity/unity-mr-utility-kit-environment-raycast/ | `Raycast`, `PlaceBox`, `IsWorldLockActive` |
| Gaze Interaction block | https://developers.meta.com/vr/documentation/unity/unity-isdk-gaze-building-blocks/ | `Ray fallback`, `Candidate Provider` |
| Microgestures | https://developers.meta.com/vr/documentation/unity/unity-microgestures/ | `OVRMicrogestureEventSource`, `GestureRecognizedEvent` |
| AI Building Blocks | https://developers.meta.com/vr/documentation/unity/unity-ai-building-blocks-overview/ | `Provider`, `On-Device LLM Support` |
| AI agents (STT / LLM / TTS) | https://developers.meta.com/vr/documentation/unity/unity-ai-agents/ | `SpeechToTextAgent`, `onTranscript` |
| MRUK anchor API | https://developers.meta.com/horizon/reference/mruk/v77/class_meta_x_r_m_r_utility_kit_m_r_u_k_anchor/ | `GetClosestSurfacePosition`, `HasAnyLabel` |
| MRUK room API | https://developers.meta.com/horizon/reference/mruk/v77/class_meta_x_r_m_r_utility_kit_m_r_u_k_room/ | `FindLargestSurface`, `GetKeyWall` |
| Antigravity account switching (Google staff reply) | https://discuss.ai.google.dev/t/is-switching-google-antigravity-accounts-a-tos-violation/174378 | `personally own` |
| Antigravity ban notice (third-party tools) | https://github.com/google-gemini/gemini-cli/discussions/20632 | `circumventing usage limits` |

### Linked from the official pages above (not opened individually)

| Topic | Link |
| --- | --- |
| Apply to Meta VR Start | https://developers.meta.com/vr/discover/programs/start/ |
| Developer Forum (check your Start flair) | https://communityforums.atmeta.com/category/horizon-developer-forum |
| Getting started with hands and eyes | https://developers.meta.com/vr/essentials/getting-started-with-hands-and-eyes/ |
| Test your app | https://developers.meta.com/vr/essentials/test-your-app/ |
| Bring your app to Meta VR Glasses | https://developers.meta.com/vr/essentials/bring-your-app-glasses |
| Eyes best practices | https://developers.meta.com/vr/design/eyes-best-practices/ |
| Meta VR CLI overview | https://developers.meta.com/vr/essentials/metavr-overview |
| CLI: devices and apps | https://developers.meta.com/vr/essentials/metavr-devices-and-apps |
| CLI: share a build | https://developers.meta.com/vr/essentials/metavr-share-builds |
| CLI: performance | https://developers.meta.com/vr/essentials/metavr-performance |
| Skills catalog | https://developers.meta.com/vr/essentials/agent-skills |
| LLM-ready docs | https://developers.meta.com/vr/essentials/ai-solutions |
| Unity project setup | https://developers.meta.com/vr/documentation/unity/unity-project-setup/ |
| Building Blocks overview | https://developers.meta.com/vr/documentation/unity/bb-overview/ |
| MRUK getting started | https://developers.meta.com/vr/documentation/unity/unity-mr-utility-kit-gs/ |
| MRUK scene data, world lock | https://developers.meta.com/vr/documentation/unity/unity-mr-utility-kit-manage-scene-data |
| MRUK content placement | https://developers.meta.com/vr/documentation/unity/unity-mr-utility-kit-content-placement |
| Instant placement motif | https://developers.meta.com/vr/documentation/unity/unity-mrmotifs-instant-content-placement |
| MRUK samples (GitHub) | https://github.com/oculus-samples/Unity-MRUtilityKitSample |
| Quick Actions (interaction wizards) | https://developers.meta.com/vr/documentation/unity/unity-isdk-quick-actions/ |
| Ray interactions | https://developers.meta.com/vr/documentation/unity/unity-isdk-ray-interaction/ |
| Microgesture + gaze scrolling | https://developers.meta.com/vr/documentation/unity/unity-isdk-gaze-microgesture-scroll/ |
| Unity FoV simulation alternatives | https://developers.meta.com/vr/documentation/unity/in-headset-fov-simulation |
| Invite URL for release channels | https://developers.meta.com/horizon/resources/publish-release-channels-add-users/#invite-users-via-url |
| Performance requirement | https://developers.meta.com/horizon/resources/vrc-quest-performance-1/#criteria |
| Last year's winners | https://developers.meta.com/horizon/blog/meta-horizon-start-developer-competition-meet-the-winners/ |
| Official quickstart: VR 101 / VR 102 | https://www.youtube.com/watch?v=QXiR_GHQ5U0 · https://www.youtube.com/watch?v=V7VGbt55VBE |

### Carried over from the original brief (not re-checked — open before relying on them)

| Topic | Link |
| --- | --- |
| Snap interactions tutorial | https://developers.meta.com/vr/documentation/unity/unity-isdk-create-snap-interactions/ |
| Poke interactions tutorial | https://developers.meta.com/vr/documentation/unity/unity-isdk-create-poke-interactions/ |
| Gaze UI / ConecastableCanvas | https://developers.meta.com/vr/documentation/unity/unity-isdk-gaze-ui/ |
| Microgestures design guidance | https://developers.meta.com/vr/design/design-microgestures/ |
| Display and colour guidance | https://developers.meta.com/vr/design/display/ |
| Microgestures walkthrough (Dilmer Valecillos) | https://www.youtube.com/watch?v=fJgvIxkFABU · https://github.com/dilmerv/MicrogesturesDemos |
| MRUK overview (Roberto Coviello) | https://www.youtube.com/watch?v=n6YZlp4yMwM |
| Snap interaction setup (Immersive Insiders) | https://www.youtube.com/watch?v=9Oe9_Im331k |
| Interaction SDK basics (Valem) | https://www.youtube.com/watch?v=4kGD8q5kEx8 |
| Environment raycast tutorial (Valem) | https://www.youtube.com/watch?v=r9gedHRY0rc |
| Past winners AMA (XR Bootcamp) | https://www.youtube.com/watch?v=BQWHNSeeC8c |
| Unity CLI + metavr tips | https://unity.com/blog/5-tips-for-speeding-up-your-xr-dev-workflows-with-unity-cli |

Also from the original brief and **not confirmed**: the `metavr_unity_setup` MCP tool, the `unity command import_asset` CLI, and `metavr vrc-local …` subcommands. Run `metavr --help` and ask the agent to list the MCP server's tools before depending on any of them.

---

## Appendix A — optional local AI for development only

Nothing here ships. It is only a way to iterate on LLM prompts without spending cloud credits, for whoever has a capable GPU.

- The AI Building Blocks include an **Ollama provider**. Run Ollama on the desktop, pull a small instruct model, and point the provider at `http://<desktop-ip>:11434` while testing over Link or on the same Wi-Fi.
- Before any build that goes to the `Competition` channel, switch the provider back to the on-device or relay option, or to none. A build that points at a home IP address will simply time out for judges — which is exactly why Layer 1 must always be there underneath.
- The original brief's faster-whisper and Kokoro servers are not needed.
