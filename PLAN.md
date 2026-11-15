# Plan

One person, one headset. The deadline is Wednesday 18 November 2026 at 20:00 GMT. My own cut-off is 16:00 GMT that day.

## Where things stand

Done and checked on the PC

- The code compiles with hands, thumb gestures, room scan placement and depth placement all switched on.
- Seventeen unit tests pass.
- A read-through of the headset code against the Meta SDK source found five bugs, now fixed. The worst one would have put the pinch point on the wrong finger joint.
- A scripted run in the editor shows the dock, three lanes and the starter cards. It moves a card to Done, adds a typed task and one with a due time, adds a routine that starts its first step, removes a card and undoes it, clears Done, switches to compact mode and moves the dock, with no errors.
- The APK builds, is signed with the release key and contains only arm64 code.

Not checked yet, because it needs the headset

- Passthrough, pinch, pointing and poking with real hands
- Thumb gestures
- The system keyboard for "New task"
- Dock placement on a real desk, in a scanned and an unscanned room
- Text size, frame rate and field of view

## Saturday and Sunday

1. Back up the `PulseDeskKeys` folder from the user profile to somewhere safe.
2. Confirm Meta VR Start membership on the account that will submit. Approval can take time.
3. Join the hackathon on Devpost.
4. In the Meta developer dashboard, create the app for Quest.
5. Put the Quest in developer mode, plug it in and install `Builds\PulseDeskXR.apk` with Meta Quest Developer Hub.
6. Run the headset checks below. Write down everything that feels wrong.
7. Create a release channel named exactly `Competition`, upload the APK and create the Invite URL. Doing this early proves the delivery route works.

## Headset checks

Put the controllers in another room first.

- [ ] The real room is visible, not black
- [ ] The dock appears within reach, facing me, and the text is readable
- [ ] Pinch a card, carry it to Done, let go. A chime plays
- [ ] From further back, point at a card, pinch and drag it to Urgent. The timer starts
- [ ] Poke "Routines", poke "Deep work 25m". Three cards arrive and the first one starts with a 2 minute timer
- [ ] Poke "Done" beside the timer. A chime plays and three seconds later the next card starts by itself
- [ ] Poke "+5". The timer gains five minutes
- [ ] With nothing running, poke "Start". The front card in Next begins
- [ ] Type "gym 1h 30m". The card shows a 90 minute sprint
- [ ] The app never opens Space Setup by itself, in a scanned or an unscanned room
- [ ] Open the system menu with the palm-up pinch. No card is grabbed by that pinch
- [ ] Poke "Compact". Only the banner and the sprint remain. Poke "Expand" to bring the board back
- [ ] In compact mode let a 1 minute sprint run out. The timer pulses in the last minute and the full board returns at zero
- [ ] Poke "New task". The system keyboard opens. Type a task and confirm. A card arrives in Next
- [ ] In the keyboard, use the microphone key to dictate "call the bank in 6 minutes". The card shows a due time
- [ ] One minute later a soft tone plays and the banner says "In 5 minutes". At the due time a louder alert plays
- [ ] With nothing running, poke minus and plus. The length on the "Start" button changes
- [ ] Let a 1 minute sprint run out. The alert is easy to hear and the banner turns red
- [ ] Drop a card on the "Clear" button. It is deleted and the button says "Undo". Poke it and the card comes back
- [ ] Poke "Clear done". The Done lane empties and the day's count stays
- [ ] With Done empty the button says "Clear all". Poke it, then poke "Undo". The whole board returns
- [ ] Pinch the timer. It pauses. Pinch again and it runs
- [ ] Open and close the system menu. The timer pauses and resumes, and a pause I chose stays paused
- [ ] "Move dock here" puts the dock where I am now looking
- [ ] Thumb swipe left sends the highlighted card to Urgent, right sends it to Done
- [ ] Thumb swipe forward and back changes the highlighted card. Thumb tap pauses
- [ ] Scanned room with a desk. The dock sits on the desk
- [ ] Room scan cleared. The dock still finds the desk
- [ ] Sofa with nothing in front. The dock floats within reach
- [ ] Scene permission denied. The dock still appears
- [ ] Wi-Fi off. Everything still works
- [ ] Quit and reopen. The board is as I left it
- [ ] Next day. Yesterday's Done cards are gone and the streak continues

## Monday

Fix what the headset showed. Rebuild and check again. Measure frame rate with a 15 second trace and aim for 72 frames per second. Run the narrow field of view simulation and confirm the lanes, timer, banner and buttons stay in view.

```
metavr device fov-sim enable --perceived
metavr device fov-sim disable
```

## Tuesday

Record the video in the headset, following the shot list in SUBMISSION.md. Keep it under three minutes and show bare hands in the first 20 seconds. Upload it to YouTube as public. No new features after 18:00 GMT, only fixes.

## Wednesday

1. Build the final APK and upload it to the `Competition` channel.
2. Install it from the Invite URL on a second Meta account and run the headset checks one more time on that install.
3. Update SUBMISSION.md so it describes exactly what shipped, then fill in the Devpost form.
4. Submit before 16:00 GMT. Tag the commit `submission`.
5. Change nothing until the winners are announced around 11 December.

## If something does not work on the headset

Each Meta-specific part can be switched off on its own from the menu PulseDesk, then Features. The app still runs with a part off.

| Problem | What to switch off | What still works |
| --- | --- | --- |
| Thumb gestures do nothing or misfire | Thumb microgestures | Pinch, point and poke |
| Dock lands in a bad place in a scanned room | Room scan placement | Depth placement and the fixed position |
| Dock lands in a bad place in an unscanned room | Depth placement | The fixed position, and "Move dock here" |
| "New task" opens no keyboard | Nothing to switch off. The app shows the goal picker instead | "Break it down" |

## What decides the result

The judges score four things equally. They are innovation, experience design, technical implementation, and polish and presentation. They test on a Quest 3 or 3S and expect 60 frames per second or better.

The parts of this entry that speak to those criteria

- The desk is the interface, so passthrough has a purpose
- It works in a room that was never scanned
- Every action has more than one hands-only route, including one with the hand at rest
- Lanes use colour, an icon and a word together, so colour alone is never needed
- A full loop takes under a minute and there is a streak to come back to

The biggest risk is that none of it has been felt on a headset yet. Time on the headset this weekend matters more than any new feature.
