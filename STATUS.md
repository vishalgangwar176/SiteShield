# Current build status

Updated: 2026-07-16

- Unity 6.0 LTS project scaffold: complete
- OpenXR + XR Interaction Toolkit package manifest: complete
- Microsoft Rocketbox NPC assets: 3 characters imported
- Training sites: Construction, Warehouse, Fire Response, Chemical Processing, Electrical Maintenance
- Hidden-answer inspection model: 2 hazards + 2 safe distractors per site
- Natural site props: platforms, guardrails, access obstructions, spill, pallets, extinguishers, egress routes, chemical drums/eyewash, electrical panels, lockout tags, cable ramps
- Deterministic scoring: +100 hazard, -25 first false positive, repeated selections 0
- Completion and debrief: per-site progress, total score, all-sites completion message
- Exploration: five XR/mouse-selectable site portals, continuous campus ground, bright route lanes, and desktop movement
- Startup safety: XR rig spawn height and runtime grounding guard prevent initial drop
- HUD polish: compact field-ops header, score readout, cyan accent, and wrapped guidance panel
- Construction practical: five ordered XR-grab actions (PPE, barricade, guardrail, material cart, final walkdown) with sequence gating, visual completion state, HUD coaching, and +20 per-step bonus
- Construction modeling pass: authored multi-part scaffold, PPE case, barricade, guardrail kit, material cart, and clipboard assemblies replace the single-block placeholders
- External asset pass: Poly Haven CC0 hand truck, ladder, cement bag, drill, and industrial barrel downloaded into `Assets/ThirdParty/PolyHaven/` and wired into the construction scene
- NPC chat: click a Rocketbox coach to open a live input panel; typed questions route to the grounded LLM service with deterministic offline fallback
- NPC idle behavior: timed weight shift, field-pointing/explanation gestures on humanoid rigs, generic-rig body sway, and conversation-aware head/talk motion
- Environment lighting: procedural sky, warm soft-shadow sun, site spotlights, tri-light ambient color, distance fog, and five baked reflection probes
- NPC dialogue: four-step progress-aware coaching/debrief prompt cycle per site
- Privacy-conscious event log: JSONL inspection outcomes only; no learner identity or conversation text
- Core scoring validation: passed after the inspection-model update
- LLM NPC conversation: OpenAI-compatible client + 8-turn rolling context
- Offline fallback: implemented
- Local model check: `hermes3:8b` returned a grounded construction-safety answer
- Runtime/editor source revalidation: passed with 0 warnings and 0 errors
- Unity license and scene generation: complete
- Generated scene: `Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity`
- OpenXR Project Validation: 0 issues out of 16 checks
- Unity EditMode tests: 11/11 passed
- Final visual tour: `Captures/construction-hands-on-v1` (13/13 frames generated, including the hands-on construction frame)
- Independent visual QA: functional PASS and typography/visual PASS
- Windows standalone build: succeeded, `Builds/Windows/SiteShield.exe`

## Prototype status

Complete and runnable. Remaining items are optional production polish: replace the static Rocketbox poses with authored idle animations and conduct physical-headset usability testing for the target device.
