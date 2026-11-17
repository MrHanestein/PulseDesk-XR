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

You add a task by pinching Speak and saying it, using the headset's own speech recognition, or by typing one line on the Meta keyboard. If it is not available, a keyboard on the dock opens instead, poked like any other button, with shortcut keys that write "in 15 min" and "for 25 min" in one tap. The app reads times out of the line. "Call the bank at 3pm" becomes a card that is due at 15:00. "Gym 1h 30m" gets a ninety minute sprint. Five minutes before a task is due the dock plays a soft tone. When it is due the dock plays an alert, and if nothing else is running the task starts by itself.

The card in Urgent is a timed sprint. A bar drains as the time runs out, a tone marks the halfway point and the timer pulses in the last minute. One tap on Done finishes it and three seconds later the next card starts by itself, with due tasks first. One tap on +5 gives it five more minutes.

Routines add three timed steps for common jobs and start the first one. Deleting a card or clearing the board can be undone with one tap. While a sprint runs the real room fades to grey and dims a little, so the task stands out, and the colour returns when you finish or pause. A compact mode hides everything except the running sprint, so the dock can sit beside a laptop, and it opens again when time runs out. The header shows open tasks, tasks done today and minutes of focus today. The board is saved, so tomorrow the open tasks are still there, yesterday's finished pile is gone, and the first line you see is what the last day came to.

How I built it

Unity 6 with the Meta XR All-in-One SDK. To place the dock, the app first looks for a table in the room scan using MR Utility Kit. If there is no scan it reads live depth data and looks for a flat surface below eye level. If that also fails it uses a fixed seated position, so there is always a dock within reach. Pinch, pointing and poking are handled by a small interaction layer I wrote on top of Meta's hand tracking data. Thumb microgestures sort cards with the hand at rest. The dock, cards and buttons are built from code when the app starts, and every sound is generated in code. All task logic sits in one store with no SDK dependencies and has unit tests. Reading times out of a typed line is done with rules on the device. Everything works offline and with every permission denied. I used an AI coding assistant to help write and test the code.

Challenges

Finding the desk in a room that was never scanned. A room scan gives a labelled table, but many people never make one, and asking for it at launch sends them into a setup flow before they have seen the app. So the app tries the scan quietly, then reads live depth data for a flat surface below eye level, then falls back to a seated position, and re-places the dock if a scan arrives late.

Typing in a headset is slow. The first version had only a keyboard, and testing it on the headset made it obvious that nobody wants to poke out a sentence. Speaking a task with one pinch, through the headset's own speech recognition, turned adding a task from the slowest thing in the app into the fastest.

What's next

Pinning a task to a place in the room, so "water the plants" waits by the window. Importing today's calendar so meetings arrive as cards with their times. A shared board for two people working in the same room.

**Hand interactions**

Every action has a hands-only route and most have three. You can grab or poke within arm's reach, point and pinch from a distance, or use thumb microgestures with your hand at rest. The buttons you use most, Done, +5 and Start, sit beside the timer and take one tap. Tasks can be spoken with one pinch, typed on the Meta keyboard, or typed on a keyboard on the dock as a fallback. Touch controllers also work for anyone who prefers them. A controller is never needed.

## 3. Features, as a list for the form or the video description

- A dock that lands on your real desk, in a scanned room, an unscanned room or with no desk at all
- Three ways to move a card with bare hands (grab, point and pinch, thumb swipe)
- Speak or type one line and it becomes a card with a due time and a sprint length
- Hands first, and Touch controllers work too: point and pull the trigger, A to finish, B to speak, X to type, Y to pause, with a short buzz on each press
- Alerts five minutes before and at the due time, and the due task starts by itself if you are free
- A sprint timer with a draining bar, a halfway tone, a last minute pulse and a repeating time-up alert
- One tap for Done, +5 minutes, Start, pause
- The next card starts by itself when a sprint is finished
- Routines of three timed steps that start straight away
- One button for clear done, clear all and undo
- The room fades to grey while a sprint runs and the colour comes back when it ends, using passthrough colour controls
- Compact mode that leaves only the sprint in view
- Open tasks, done today and focus minutes in the header, a streak, and a recap on the first launch of a new day
- Works offline, with every permission denied, and saves the board between sessions

## 4. Video script

Aim for 2 minutes 40 seconds. Never go past 2 minutes 55, because judges may stop at 3:00. Lead with your best moment. Real footage only, no AI generated video.

**How to film it**

- Most of the video is what you see in the headset. Record it with the headset's own camera menu (press the Meta button, open Camera, choose Record video). It records passthrough and the app together, with your voice from the headset microphone.
- Two short shots from your phone on a tripod or a shelf, showing you at your desk wearing the headset. These give the judges the real-world picture in two seconds. You are the entrant, so your face is allowed. Nobody else may be in frame.
- Record the headset part in short takes, one row of the table at a time, and join them in any video editor. Speak the line during the take, or record it afterwards as a voiceover.
- Put the captions in the right-hand column on screen. Many judges watch with the sound off.

**Before you press record**

- Clear the desk of logos and brand names. Have a laptop or a notebook on the desk, so the room looks like real work.
- Use bright, even light. Hand tracking and the room scan both like it.
- In the app, poke Clear until the board is empty, then add three real tasks you would actually do today, for example "Email Sam the report", "Book dentist", "Gym 1h".
- Start the Deep work routine once and let its 2 minute step run out before filming, so you know what the timer, the pulse and the alert look and sound like.

**Shots**

| Time | What to show | Line (speak it) | Caption |
| --- | --- | --- | --- |
| 0:00 to 0:06 | Phone shot: you sit down at your desk and put on the headset. | "Every morning my to-do list lives in five different places." | |
| 0:06 to 0:16 | Headset view: your real desk. The dock grows in on it with Next, Urgent and Done. | "PulseDesk puts my whole day on my real desk." | Finds your desk by itself |
| 0:16 to 0:34 | Bare hands. Pinch "Speak" and say "call the bank at 3pm". The words appear, then a card lands in Next with 15:00 on it. | "I say a task once. It hears the time and remembers it for me." | Speak a task. It reads the time |
| 0:34 to 0:54 | Pinch Routines, then "+ Deep work 25m". Three timed cards land, the first starts, the bar drains and the room fades to grey around the desk. | "When I start, the room steps back. Only my task is in colour." | The room dims while you focus |
| 0:54 to 1:14 | Pinch Done. Chime, green burst, a pip lights, and three seconds later the next card slides into Urgent and starts. Pinch +5. | "When I finish, the next task starts by itself. I never manage the list." | One pinch. The next task starts itself |
| 1:14 to 1:30 | Hand resting on your leg. Thumb swipe right finishes a card. Thumb tap pauses, and the room's colour comes back. | "Hand at rest, a flick of the thumb is enough." | Thumb gestures, hand at rest |
| 1:30 to 1:50 | Pinch Compact. Type on your real laptop with the small timer beside it. Show the "Then: 15:00 Call the bank" line, then the five minute tone. | "While I work it shrinks to a timer, and it tells me what is coming." | Compact mode beside real work |
| 1:50 to 2:02 | Pick up a controller. Point and pull the trigger to move a card, press A to finish. Put it down and carry on with hands. | "Hands first, but controllers work too." | Hands or controllers |
| 2:02 to 2:14 | Drop a card on Clear, then pinch Undo and it comes back. | "Nothing is ever lost by accident." | One tap undo |
| 2:14 to 2:28 | Phone shot or headset view in a second room with no room scan. The dock still lands in reach. | "It works in rooms it has never seen." | No room scan needed |
| 2:28 to 2:40 | The header: tasks done and focus minutes, the lit pips. End card with the name and tagline. | "PulseDesk XR. My day, on my desk." | PulseDesk XR |

**If something will not work on the day**

- If Speak does not hear you, use Type for that shot and change its line to "I type it once. It reads the time out of what I wrote." Do not show or say Speak in the video unless it worked in the uploaded build.
- If the room does not visibly fade to grey in the recording, drop that line and caption and keep the rest of the shot.
- Every line in this script must be true of the build the judges install.

**Practice**

Run the whole sequence three times without recording. Time it with your phone. If it runs long, cut the controller shot first, then the undo shot.
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
