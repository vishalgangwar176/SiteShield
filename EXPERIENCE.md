# Field Mentor VR Experience Contract

## Player goal and feel

The learner should feel like a new hire completing a supervised pre-shift safety walk, not like a player collecting glowing targets. The world presents believable construction conditions. A field mentor joins only when the learner enters a work zone, stays beside the learner without blocking the route, answers questions in plain language, and helps the learner connect a visible condition to a control decision.

## Core loop

1. Approach a marked work zone with an unobstructed view.
2. Receive a short peripheral HUD brief as the zone becomes active.
3. Observe the scene before selecting anything.
4. Click or XR-select a condition and receive a consequence-focused explanation.
5. Ask the accompanying mentor a typed question or request a hint with `C`.
6. Complete the hands-on control steps, see progress update, and move to the next zone.

## 20–25 minute guided session

Certification requires activity, not passive waiting. Each of the five sites contributes a four-minute field review and requires all four visible conditions to be recorded plus two mentor turns. Construction also requires its five ordered hands-on controls. The HUD keeps a live `SESSION mm:ss / 20:00` clock and blends time, inspections, mentor turns, and practical steps into the progress rail.

| Segment | Required learner work | Target time |
|---|---|---:|
| Campus orientation | Movement, controls, route selection | 1–2 min |
| Construction | Four condition reviews, two coach turns, five hands-on controls | 5–6 min |
| Warehouse | Four condition reviews, two coach turns, pedestrian/vehicle separation reasoning | 4 min |
| Fire response | Four condition reviews, two coach turns, egress/access reasoning | 4 min |
| Chemical processing | Four condition reviews, two coach turns, labeling/segregation reasoning | 4 min |
| Electrical maintenance | Four condition reviews, two coach turns, lockout/crossing reasoning | 4 min |
| Final review | Score, missed controls, coach debrief | 1–2 min |

The certification state unlocks only after 20 active site minutes, 20/20 condition reviews, 10 mentor turns, all required hazards, and 5/5 construction practical steps.

## State machine

| State | Entry | Exit | Interrupts | Next |
|---|---|---|---|---|
| Campus roaming | Start or leave a zone | Enter radius of nearest zone | Portal travel | Zone briefing |
| Zone briefing | Enter radius | First inspection or brief acknowledged by movement | Chat open | Guided inspection |
| Guided inspection | Active zone | Required hazards found / zone exit | Chat, hands-on step | Debrief or roaming |
| Mentor chat | Click mentor or press `C` while close | Close or 25 s inactivity | Zone exit | Guided inspection |
| Debrief | Site requirements complete | Leave zone | Mentor chat | Campus roaming |

## Five-component evaluation

- Clarity: HUD appears only inside a zone and names the active work zone; the mentor occupies a consistent right-rear position.
- Motivation: findings change score, site progress, and the final safety review.
- Response: click/XR select records a condition immediately; `C` opens the nearby coach; chat accepts learner questions.
- Satisfaction: every correct finding changes text, state color, score, and progress. Audio remains future debt.
- Fit: movement is restrained and human-scale; the mentor does not teleport except for recovery when badly separated.

## Starting values and playtest plan

All values below are prototype starting values, not claimed standards.

| Value | Start | Pass metric | Adjustment if it fails |
|---|---:|---|---|
| Zone enter radius | 9.5 m | Brief appears before the learner reaches the first inspectable object in 10/10 approaches | Increase by 1 m if late; decrease if adjacent zones cross-trigger |
| Zone exit radius | 11 m | HUD does not flicker during perimeter inspection | Increase hysteresis by 1 m |
| Mentor side offset | 1.55 m right, 0.8 m rear | Mentor is visible with a small head turn and blocks the route 0/10 times | Move farther right in 0.25 m steps |
| Mentor speed | 1.65 m/s, 2.5 m/s catch-up | Mentor settles beside a continuously walking learner within 4 s | Raise catch-up only; preserve close-range calm |
| Chat range | 3.2 m | `C` works whenever the mentor visually reads as conversationally close | Increase by 0.5 m if learners repeatedly miss |
| Guided site time | 4 min × 5 sites | Median first-attempt completion is 20–25 min without idle waiting | Add/trim one guided prompt before changing the time floor |

## Edge and abuse cases

- Adjacent zones: nearest zone wins; exit hysteresis prevents flicker.
- Teleport/portal travel: the destination zone activates on the next proximity sample.
- Mentor separation over 8 m: one recovery reposition is allowed behind the learner, then normal following resumes.
- Player personal space: mentor stops at the side offset and never targets the camera position.
- Zone exit during chat: chat closes, mentor returns home, HUD hides.
- No camera or coordinator: systems remain inactive without throwing.

## Playtest scenarios

- New learner: walk from campus to construction without using a portal and infer what to do.
- Stress: sprint across two zone boundaries, repeatedly press `C`, and exit while a reply is pending.
- Skill: inspect only the two hazards and complete five ordered construction controls.
- Abuse: circle the mentor, stand in its path, teleport between zones, and attempt cross-site chat.
- Readability: an observer should identify active zone, last outcome, progress, and mentor availability from one frame.
