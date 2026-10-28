# Evidence-Grounded Hands-On Placement Tasks

## Learning construct

The placement tasks teach procedural hazard control: learners must select the correct control, physically move it, and release it at the operationally correct location in the required sequence. Completion therefore demonstrates a spatial control decision and a procedural action, not recognition by clicking.

## Construct → evidence → mechanic → signal

| Construct | Evidence | Implemented mechanic | Telemetry signal |
|---|---|---|---|
| Construction hazard mitigation through active practice | Abotaleb et al. developed an interactive Unity VR model based on experiential learning, with programmed responses to trainee actions and improved hazard-identification and mitigation outcomes. [DOI](https://doi.org/10.1002/cae.22585), [OpenAlex W4309858751](https://openalex.org/W4309858751) | Every practical prop must be grabbed or dragged and released inside a highlighted control zone. A click cannot complete it. | `placement_attempt`: site, step, action, release distance, success, input mode |
| Personalized performance feedback | Wolf et al. evaluated active virtual construction-safety environments with personalized feedback and automatically collected performance data. [DOI](https://doi.org/10.1016/j.aei.2021.101469), [OpenAlex W3216772515](https://openalex.org/W3216772515) | Incorrect placement returns the prop and states the expected control location; success snaps the prop into its installed state and triggers coach encouragement. | attempt count, failed distance, successful distance, sequence state |
| Hazard-management performance in realistic contexts | Hasanzadeh et al. combined realistic environments, performance assessment, and feedback in a construction-safety protocol that improved hazard recognition and management. [DOI](https://doi.org/10.1108/ECAM-07-2019-0391), [OpenAlex W3018389716](https://openalex.org/W3018389716) | Destination zones are embedded in the worksite beside the hazard/control context, not presented as a detached quiz. | site and action identity paired with placement result |
| Skills and behavior, not presence alone | A 2024 meta-analysis reported positive effects of construction-safety VR across behavior, skills, and experience, with effects moderated by training context and work experience. [DOI](https://doi.org/10.1016/j.jsr.2023.11.011), [OpenAlex W4389262930](https://openalex.org/W4389262930) | The same scored action works with desktop dragging and VR controller grabbing, allowing modality comparisons without changing the learning rule. | `inputMode = DesktopDrag | XRGrab` |

## Module task map

| Module | Placement sequence |
|---|---|
| Construction | Stage PPE → place exclusion barricade → install guardrail kit → return material cart → deliver final walkdown clipboard |
| Warehouse | Deploy spill kit → separate vehicle route with barrier → relocate aisle load to staging |
| Fire response | Clear extinguisher access → stage response kit → deliver egress checklist |
| Chemical processing | Place containment kit → deliver GHS record → stage eyewash test kit |
| Electrical maintenance | Place lockout/tagout kit → deliver zero-energy record → protect cable crossing |

## Interaction state machine

1. `Locked`: non-current task has no active grab/click collider or destination marker.
2. `Ready`: current prop and destination marker are highlighted.
3. `Held`: desktop left-drag or XR select moves the prop; clicking without movement does not resolve the task.
4. `ReleasedIncorrect`: release outside tolerance logs failure, publishes corrective feedback, and returns the prop to its start pose.
5. `ReleasedCorrect`: release inside tolerance logs success, snaps to the installed pose, awards score, advances the sequence, and triggers NPC encouragement.
6. `Complete`: installed prop is locked and cannot be scored twice.

Interruption behavior: leaving a site, losing focus, or disabling the component returns an unfinished prop to its start pose. Later steps cannot be manipulated until the current placement succeeds.

## Five-component evaluation

- Clarity: only the current prop and its labeled destination are active.
- Motivation: every successful placement advances certified practical progress and score.
- Response: the prop follows the learner continuously and evaluates the actual release position.
- Satisfaction: successful snap, HUD confirmation, score change, and NPC encouragement provide multiple feedback channels.
- Fit: controls are real worksite props placed in operationally meaningful locations.

## Starting values and test plan

- Starting value: placement radius `0.85 m` for portable controls and `1.10 m` for large barriers/carts.
- Micro-test: complete all 17 placements once on desktop and once with XR input. Pass when intended releases succeed at least 9/10 times without accepting visibly wrong zones.
- If valid drops fail, increase radius by `0.10 m`; if wrong placements pass, decrease by `0.10 m` and re-test.
- Starting value: return/snap duration `0.28 s`. Pass when the state change reads clearly without delaying the next action; adjust in `0.05 s` steps.

## Assumptions

- ASSUMPTION: a planar desktop drag is an acceptable mouse analogue for controller grabbing. IMPACT: both modalities assess release location using one rule. IF WRONG: desktop learners may need depth control. VALIDATE: observe whether the five site tasks can be completed without camera repositioning during the drag.
- ASSUMPTION: the authored target locations represent the intended safety controls for this prototype. IMPACT: success means spatially correct mitigation. IF WRONG: a subject-matter expert must revise the destination coordinates. VALIDATE: construction-safety expert walkthrough before summative deployment.

## Abuse and readability checks

- A press-and-release at the start pose must not advance progress.
- Dropping a later-step object must not advance progress.
- Repeated release outside the zone must not award score.
- A completed object cannot be moved or scored again.
- An observer must see the active prop, destination marker, failed return, and successful installed state.
