# Submission pack

Everything the Devpost entry needs. The drafts below describe the build as it is today. Change them if the build changes, and never claim something that is not in the uploaded APK.

## 1. What must exist

| Item | Rule | Done |
| --- | --- | --- |
| Playable build | APK on a new release channel named `Competition`, shared by Invite URL. It must stay free and available until winners are announced around 11 December. | ☐ |
| Video | Under 3 minutes. Real footage from the headset or XR Simulator. YouTube or Vimeo, set to public. | ☐ |
| Form | Every field below, in English. | ☐ |
| Submitter | I am a Meta VR Start member with Developer Access switched on. | ☐ |

## 2. Devpost form drafts

**Submission name**

PulseDesk XR

**Tagline** (140 characters at most)

Your real desk becomes a task board. Pick cards up, pinch them from afar or flick your thumb. Urgent, Next, Done. No controllers.

**Track** Productivity

**Division** New Experience

**Target launch date** `fill in`

**Team** Solo. Name, email, role (developer).

**Description**

Inspiration

A to-do list is easy to write and hard to face. I wanted the three things that matter, what is urgent, what is next and what is finished, to sit on my desk as objects I can pick up. Sorting should take seconds, and finishing something should feel like something.

What it does

PulseDesk XR places a small dock on your real desk with three lanes. Tasks are cards. You can grab a card and drop it in a lane, pinch it from a distance, or keep your hand on your leg and flick your thumb. A card in Urgent starts a five minute sprint with an alert at the halfway point and another near the end. A card in Done plays a chime and adds to your streak for the day. You can type your own task with the headset keyboard, or pick a goal and have it split into three small steps. The board is saved, so tomorrow your open tasks are still there and yesterday's finished pile is cleared.

How I built it

Unity 6 with the Meta XR All-in-One SDK. To place the dock, the app first looks for a table in the room scan using MR Utility Kit. If there is no scan it reads live depth data and looks for a flat surface below eye level. If that also fails it uses a fixed seated position, so there is always a dock within reach. Pinch, pointing and poking are handled by a small interaction layer I wrote on top of Meta's hand tracking data. Thumb microgestures sort cards with the hand at rest. The dock, cards and buttons are built from code when the app starts. All task logic sits in one store with no SDK dependencies and has unit tests. Everything works offline and with every permission denied. I used an AI coding assistant to help write and test the code.

Challenges

`Fill in two real ones after headset testing. For example placement in unscanned rooms, or making pinch reliable.`

What's next

`Fill in only what you would really do. For example voice entry, a wall view, calendar import.`

**Hand interactions**

Every action has a hands-only route and most have three. You can grab or poke within arm's reach, point and pinch from a distance, or use thumb microgestures with your hand at rest. Text entry uses the system keyboard with hands. A controller is never needed.

## 3. Video shot list

Aim for 2 minutes 15 seconds. Never go past 2 minutes 55. Show real use. Do not use AI generated video.

| Time | Shot | Caption |
| --- | --- | --- |
| 0:00 to 0:10 | Headset view of the desk. The dock appears on it. | Your desk already knows what matters. |
| 0:10 to 0:30 | Bare hands in view. Grab the first starter card and drop it on Done. | No controllers. |
| 0:30 to 0:55 | Hand resting on the leg. Thumb swipe sends a card to Urgent and the timer starts. | Sort tasks without lifting your arm. |
| 0:55 to 1:20 | Halfway alert. Pause with a thumb tap. Finish the card. The streak goes up. | |
| 1:20 to 1:45 | Poke "New task" and type one. Then "Break it down" and three cards land in Next. | |
| 1:45 to 2:00 | A different room with no scan. The dock still lands on the table. | Works in rooms it has never seen. |
| 2:00 to 2:15 | Narrow field of view. Everything still in frame. End card with name and tagline. | |

Before uploading, check that there are no other people's faces or voices, no brand logos visible on the desk in passthrough, no copyrighted music, captions are in English, and the link opens in a private browser window.

## 4. Release channel steps

1. In the Meta developer dashboard open the app, then Distribution, then Release Channels. Create `Competition`.
2. Upload the signed APK. The package name is `com.pulsedesk.pulsedeskxr`.
3. Each upload needs a higher version code than the last. The build raises it by itself.
4. In the channel, create the Invite URL.
5. Open the URL signed in to a different Meta account, accept, install on a headset and launch.

## 5. Final checks

- [ ] Start membership confirmed on the submitting account
- [ ] APK is release signed and arm64 only, and the `Competition` channel shows the final version code
- [ ] Invite URL installs and launches from a second account
- [ ] Cold launch with controllers in another room, full loop completed
- [ ] Unscanned room, dock appears
- [ ] Wi-Fi off, everything works
- [ ] Scene permission denied, no dead end
- [ ] System menu opened and closed during a sprint, timer pauses and resumes
- [ ] 72 frames per second held in a 15 second trace
- [ ] Narrow field of view simulation shows nothing essential cut off, then the simulation is switched off
- [ ] No brand names, logos, real names or placeholder text in the app
- [ ] Video is public, under 3 minutes, real footage
- [ ] Every form field filled in and read through once more after a break
- [ ] Submitted before 16:00 GMT and the commit tagged `submission`
- [ ] Nothing touched afterwards until around 11 December
