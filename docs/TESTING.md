# Testing

## Automated

Run with the Unity editor closed:

```
powershell -File tools\unity.ps1 -Tests
powershell -File tools\unity.ps1 -Method PulseDesk.EditorTools.PulseDeskAutomation.PlaySmoke -Name smoke
```

**Unit tests (18).** Edit mode, no headset needed. They cover:
- one sprint at a time
- the day count, the streak and the new-day rollover
- saving and loading the board
- timer alerts, user pause and system pause
- due-time alerts, and when a due card starts by itself
- automatic start of the next card, earliest due first
- sprint length changes and +5
- remove, clear done, clear all and undo
- room dimming on and off
- reading times and lengths from text, including cases that must not be read as times, such as "Look at 3 options" and "Buy 5 metres of cable"

**Scripted play test.** It plays the editor scene and runs this sequence:
1. Checks the dock, the three lanes and the starter cards.
2. Types a task on the dock keyboard and adds a routine.
3. Finishes a sprint, removes a card and restores it with undo.
4. Clears Done and switches to compact mode.

It fails on any error in the log and saves screenshots to `Logs/`.

## On a Meta Quest 3

The release build was installed and run on a Quest 3 over USB. The device log confirmed:
- The dock was placed on a real desk by the depth finder, at table height, in a room without a scan.
- A hand pinch pressed the Type button and the Meta system keyboard opened.
- The dock was moved several times with the Move dock button.
- The app ran without exceptions.

Speech input was tested and left out. On Quest, the system speech recognition services are not exported to third-party apps, so Android's speech recogniser reports them as unavailable. The Speak button only appears on devices where recognition is available.

## Manual checklist

Run with controllers out of reach first, then again with controllers.

- [ ] Passthrough shows the real room
- [ ] The dock appears within reach, facing you, with readable text
- [ ] Pinch a card, carry it to Done, release: chime, burst, a pip lights
- [ ] From a distance, point, pinch and drag a card to Urgent: the timer starts
- [ ] Type "call bank at 3pm": the card shows 15:00
- [ ] Routines, Deep work 25m: the first step starts and the room fades to grey
- [ ] Pinch the timer: it pauses and the colour returns
- [ ] Done: three seconds later the next card starts
- [ ] +5 adds five minutes
- [ ] Thumb swipe right sends the highlighted card to Done, thumb tap pauses
- [ ] Compact hides the board; at 0:00 the board opens again
- [ ] Drop a card on Clear, then Undo restores it
- [ ] Controllers: trigger moves cards, A finishes, B toggles compact, X types, Y pauses
- [ ] Open and close the system menu during a sprint: it pauses and resumes
- [ ] Quit and reopen: the board is as you left it
- [ ] Room scan cleared or a new room: the dock still lands in reach
- [ ] Wi-Fi off: everything works
