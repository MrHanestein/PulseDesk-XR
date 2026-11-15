# Submission pack

Everything the Devpost entry needs. The text below describes the build as it is today. Change it if the build changes, and never claim something that is not in the uploaded APK.

## 1. What must exist

| Item | Rule | Done |
| --- | --- | --- |
| Playable build | APK on a new release channel named `Competition`, shared by Invite URL. It must stay free and available until winners are announced around 11 December. | ☐ |
| Video | Under 3 minutes. Real footage from the headset. YouTube or Vimeo, set to public. | ☐ |
| Form | Every field below, in English. | ☐ |
| Submitter | I am a Meta VR Start member with Developer Access switched on. | ☐ |

## 2. Devpost form text

**Submission name**

PulseDesk XR

**Tagline** (140 characters at most)

A task board on your real desk that runs itself. Say a task once, it comes back on time. Pinch, poke or flick a thumb. No controllers.

**Track** Productivity

**Division** New Experience

**Target launch date** `fill in`

**Team** Solo. Name, email, role (developer).

**Description**

Inspiration

A to-do list is easy to write and hard to face. I wanted the three things that matter, what I am doing now, what is next and what is finished, to sit on my desk as objects I can pick up. I also wanted to stop managing the list. I say a task once, and the desk brings it back at the right time.

What it does

PulseDesk XR places a small dock on your real desk with three lanes called Next, Urgent and Done. Tasks are cards. You can grab a card and drop it in a lane, pinch it from a distance, or keep your hand on your leg and flick your thumb.

You add a task by typing or dictating one line with the headset keyboard. The app reads times out of the line. "Call the bank at 3pm" becomes a card that is due at 15:00. "Gym 1h 30m" gets a ninety minute sprint. Five minutes before a task is due the dock plays a soft tone. When it is due the dock plays an alert, and if nothing else is running the task starts by itself.

The card in Urgent is a timed sprint. A bar drains as the time runs out, a tone marks the halfway point and the timer pulses in the last minute. One tap on Done finishes it and three seconds later the next card starts by itself, with due tasks first. One tap on +5 gives it five more minutes.

Routines add three timed steps for common jobs and start the first one. Deleting a card or clearing the board can be undone with one tap. A compact mode hides everything except the running sprint, so the dock can sit beside a laptop, and it opens again when time runs out. The header shows open tasks, tasks done today and minutes of focus today. The board is saved, so tomorrow the open tasks are still there, yesterday's finished pile is gone, and the first line you see is what the last day came to.

How I built it

Unity 6 with the Meta XR All-in-One SDK. To place the dock, the app first looks for a table in the room scan using MR Utility Kit. If there is no scan it reads live depth data and looks for a flat surface below eye level. If that also fails it uses a fixed seated position, so there is always a dock within reach. Pinch, pointing and poking are handled by a small interaction layer I wrote on top of Meta's hand tracking data. Thumb microgestures sort cards with the hand at rest. The dock, cards and buttons are built from code when the app starts, and every sound is generated in code. All task logic sits in one store with no SDK dependencies and has unit tests. Reading times out of a typed line is done with rules on the device. Everything works offline and with every permission denied. I used an AI coding assistant to help write and test the code.

Challenges

`Fill in two real ones after headset testing. For example placement in unscanned rooms, or making pinch reliable.`

What's next

`Fill in only what you would really do. For example calendar import, a wall view, shared boards.`

**Hand interactions**

Every action has a hands-only route and most have three. You can grab or poke within arm's reach, point and pinch from a distance, or use thumb microgestures with your hand at rest. The buttons you use most, Done, +5 and Start, sit beside the timer and take one tap. Text entry uses the system keyboard with hands. A controller is never needed.

## 3. Features, as a list for the form or the video description

- A dock that lands on your real desk, in a scanned room, an unscanned room or with no desk at all
- Three ways to move a card with bare hands (grab, point and pinch, thumb swipe)
- One line of text or dictation becomes a card with a due time and a sprint length
- Alerts five minutes before and at the due time, and the due task starts by itself if you are free
- A sprint timer with a draining bar, a halfway tone, a last minute pulse and a repeating time-up alert
- One tap for Done, +5 minutes, Start, pause
- The next card starts by itself when a sprint is finished
- Routines of three timed steps that start straight away
- One button for clear done, clear all and undo
- Compact mode that leaves only the sprint in view
- Open tasks, done today and focus minutes in the header, a streak, and a recap on the first launch of a new day
- Works offline, with every permission denied, and saves the board between sessions

## 4. Video script

Aim for 2 minutes 20 seconds. Never go past 2 minutes 55. Record in the headset with bare hands and a tidy desk. No AI generated video. Speak the lines or put them on screen as captions.

| Time | What to show | Line |
| --- | --- | --- |
| 0:00 to 0:08 | Your real desk through the headset. The dock grows in on the desk. | "This is my desk. PulseDesk turns it into the one place my day lives." |
| 0:08 to 0:25 | Bare hands in view. Pinch the first card, carry it to Done, chime. | "No controllers. I pick a task up and put it where it belongs." |
| 0:25 to 0:45 | Poke New task. Dictate "call the bank at 3 pm". The card lands with 15:00 on it. Then "gym 1 hour 30". | "I say it once. It reads the time out of what I said." |
| 0:45 to 1:05 | Poke Routines, then Deep work. The first step starts and the bar begins to drain. | "A routine is three timed steps. The first one is already running." |
| 1:05 to 1:25 | Poke Done. Three seconds later the next card slides into Urgent and starts. Poke +5. | "When I finish, the next task starts by itself. I never manage the list." |
| 1:25 to 1:45 | Hand resting on the leg. Thumb swipe right finishes a card, thumb tap pauses. | "With my hand at rest, a flick of the thumb is enough." |
| 1:45 to 2:00 | Poke Compact. Type on a real keyboard with the small timer beside the laptop. Show the last minute pulse and a due alert. | "While I work it shrinks to a timer, and it tells me when something is due." |
| 2:00 to 2:12 | A different room with no scan, or a sofa. The dock still lands in reach. | "It works in rooms it has never seen." |
| 2:12 to 2:20 | The header with done and focus minutes. End card with the name and tagline. | "PulseDesk XR. Your day, on your desk." |

To record the due alert without waiting, add a task with "in 6 minutes" before you start recording, so the five minute tone lands during the take. To record the last minute pulse, start a 1 minute sprint.

Before uploading, check that there are no other people's faces or voices, no brand logos visible on the desk in passthrough, no copyrighted music, captions are in English, and the link opens in a private browser window.

## 5. Release channel steps

1. In the Meta developer dashboard open the app, then Distribution, then Release Channels. Create `Competition`.
2. Upload the signed APK. The package name is `com.pulsedesk.pulsedeskxr`.
3. Each upload needs a higher version code than the last. The build raises it by itself.
4. In the channel, create the Invite URL.
5. Open the URL signed in to a different Meta account, accept, install on a headset and launch.

## 6. Final checks

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
