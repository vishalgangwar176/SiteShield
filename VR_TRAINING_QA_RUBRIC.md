# SiteShield Quality Assurance Rubric

This rubric is a release gate for every build. Score each dimension from 0 (absent) to 3 (verified). A release requires no critical failure, every dimension at least 2, and a total of at least 25/30.

| Dimension | 3 - verified release quality | Critical failure |
|---|---|---|
| Learning and job fidelity | Each scenario links a recognizable construction hazard to a hands-on control, feedback, and debrief. | Passive viewing replaces required participation or the control taught is unsafe. |
| Environment and embodiment | Human scale, grounded props, collision-safe placement, isolated zones, and realistic worksite context are verified in-headset. | Falling, clipping, floating safety equipment, or another zone is visible through the boundary. |
| Interaction feedback | Every actionable target has visible affordance, hover/highlight, press/select response, success/error feedback, and a recoverable next step. | A visible control does nothing or selection state is ambiguous. |
| Comfort and locomotion | One transform writer controls the player; horizon and spawn are stable; turning/movement alternatives exist; flicker and forced acceleration are avoided. | Camera jitter, involuntary motion, trapping, or repeated high-frequency flashing. |
| Accessibility | XR and desktop/keyboard paths coexist; targets are large and spaced; text is legible and customizable; important audio has text; interaction does not require fine or simultaneous motion. | A required task is inaccessible through the supported input path or critical information is color/audio-only. |
| HUD, dialogue, and NPC behavior | Critical HUD is depth-independent; NPCs never mask it; NPCs are clickable; replies appear in both a readable panel and an anchored speech bubble; idle/walk/encourage transitions are natural. | NPC or geometry hides critical UI, dialogue cannot be closed, or an LLM failure blocks progress. |
| Runtime performance | Target-headset refresh is sustained in representative sites, with frame-time, draw-call, memory, and garbage-collection evidence recorded. | Sustained missed frames, thermal instability, or interaction latency that harms task performance. |
| Reliability and recovery | Start, portal travel, reset/return, input focus, collider boundaries, and interruption recovery are repeatable. | User cannot start, enter/leave a module, or recover without restarting. |
| Assessment validity | Observations map to objectives; scoring is deterministic; hints/dialogue do not silently alter scores; completion and debrief evidence are retained. | Score changes without a learner action or completion can be earned without demonstrating the skill. |
| Verification evidence | Automated composition/logic tests, Windows build, direct user-path playtest, screenshots, logs, and headset checks are attached to the build. | Release is based only on editor inspection or unverified assumptions. |

## Current source anchors

- OSHA construction training requires employee participation, hazard recognition, and instruction in controls, including fall protection: <https://www.osha.gov/laws-regs/regulations/standardnumber/1926/1926.761>
- OSHA notes that standardized video alone is not interactive, hands-on, site-specific training with testing: <https://www.osha.gov/laws-regs/standardinterpretations/1999-08-03-0>
- W3C XR Accessibility User Requirements covers multiple input methods, large targets, customization, orientation, sickness triggers, captions, and text alternatives: <https://www.w3.org/TR/xaur/>

## Evidence required per build

1. Automated test result and build log.
2. Stable-start telemetry and representative frame-time capture.
3. Menu, hover/press, site entry, object inspection, NPC chat/speech bubble, and return-path screenshots.
4. One complete construction scenario playthrough plus one error-and-recovery path.
5. Headset verification before a release candidate is labeled VR-ready.
