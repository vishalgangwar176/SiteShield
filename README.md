# SiteShield - VR Safety Training Explorer

Unity 6 + OpenXR prototype for exploring multiple safety-training sites and talking with Microsoft Rocketbox NPCs. The experience contains five workplace zones in one continuous campus:

![Chemical hands-on safety training with PPE and mission HUD](docs/images/chemical-hands-on-ppe.png)

## Training mechanics at a glance

![Safety-training mechanics workflow](docs/images/mechanics-workflow.svg)

The assessment engine owns hazards, action order, completion, and scoring. The NPC coach provides grounded, role-aware explanations and natural chat interaction without changing the deterministic training outcome.

## In-game documentation captures

| Training hub | Electrical safety coach |
| --- | --- |
| ![Five-module training hub](docs/images/training-hub.png) | ![Electrical coach dialogue and protected cable-crossing task](docs/images/electrical-coach-dialogue.png) |

- Construction: fall protection and blocked-access hazards
- Warehouse: spill and vehicle-route hazards
- Fire response: extinguisher access and evacuation hazards
- Chemical processing: solvent storage, labeling, and eyewash access
- Electrical maintenance: energized-panel lockout and protected cable crossings

Each site contains two real hazards and two controlled look-alikes. Nothing is labeled or colored as a hazard before inspection. A correct identification earns 100 points; the first selection of a safe condition costs 25 points; repeats do not change the score. The deterministic training engine owns completion and scoring. The language model only produces grounded NPC coaching, so a model response cannot change the correct answer or score.

Five bright route lanes and portal pads move the learner across a continuous walkable ground plane; each site can also be entered directly through its portal. A startup grounding guard prevents the XR rig from dropping before locomotion is initialized. Each Rocketbox coach cycles through inspection guidance, progress-aware hints, control explanations, and a score-neutral debrief. Inspection events are written as JSONL under Unity's persistent data folder without learner identity or raw conversation text.

## Run the completed prototype

Run `Builds/Windows/SiteShield.exe`, or open this folder with Unity `6000.0.75f1` and play `Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity`. The generated scene already contains the XR Origin, controller interaction, three sites, HUD, portals, hazards, and Rocketbox coaches. `Safety Training > Build Prototype Scene` regenerates it.

The scene supports WASD movement, right-mouse look, and mouse selection as a desktop fallback. In VR, inspection targets, coaches, and site portals use `XRSimpleInteractable` selection. Green and amber colors appear only after a learner makes a selection. The HUD uses a compact dark field-ops panel with site header, score, and wrapped feedback text.

## Construction practical

Construction Site is the hands-on lead scenario. Students complete a deliberately ordered five-step control loop by grabbing marked props with an XR controller (or clicking them in desktop fallback): pick up the PPE kit, set the exclusion barricade, install the guardrail kit, move the material cart to staging, and complete the final walkdown with the clipboard. Out-of-order actions produce an immediate HUD sequence cue; completed actions turn green and award a 20-point practical bonus. The sequence is repeat-safe and ends with a coach debrief prompt.

The construction pass uses authored multi-part model assemblies rather than single placeholder blocks: scaffold uprights/crossbars/decks/base jacks, PPE case contents and latch, barricade feet/posts/striping, rail-kit base plates/uprights, cart handle/wheels, and clipboard clip.

The site is also dressed with downloaded Poly Haven CC0 assets: a hand truck, sectioned ladder, cement bag, drill, and industrial barrel. Source attribution and local files are tracked under `Assets/ThirdParty/PolyHaven/`.

Rocketbox coaches now run an idle behavior loop: subtle body sway/weight shift for generic rigs, timed field-pointing and explanation gestures where humanoid bones are available, head motion during conversation, and a separate talking pose so chat interaction does not snap the NPC back to the default pose.

Environment lighting uses a warm directional sun with soft shadows, site work lights, tri-light ambient color, linear distance fog, a procedural sky, and one baked reflection probe per workplace. The baked probes avoid the GPU/headless instability of realtime cubemap updates while preserving stable VR performance.

Click any Rocketbox coach to open the live chat panel. Students can type a question and press Enter/Send; the coach answers through the configured OpenAI-compatible endpoint, with a grounded offline fallback when the endpoint is unavailable. Conversation context is retained for the active coach, while authored safety facts and deterministic scoring remain authoritative.

## Validation

- Core scoring smoke test: passed
- Unity EditMode suite: 11/11 passed
- OpenXR Project Validation: 0 issues out of 16 checks
- Independent visual QA: two reviewers passed the 13-frame construction-focused set under `Captures/construction-hands-on-v1`
- Windows standalone build: succeeded at `Builds/Windows/SiteShield.exe`

## LLM endpoint

`LlmEndpointConfig` defaults to a local OpenAI-compatible endpoint:

- URL: `http://localhost:11434/v1/chat/completions`
- Model: `hermes3:8b`

For a hosted provider, change the endpoint and model in the NPC inspector. Store the API key in an operating-system environment variable and set only its variable name in Unity. Do not put secrets in scenes, assets, or source control. If the endpoint is unavailable, NPCs use a deterministic offline response.

NPC replies are grounded in authored site facts, limited to concise complete sentences, and paginated when necessary. The deterministic training engine remains the only authority for hazards, scoring, progress, and completion.

## Rocketbox attribution

The selected character files under `Assets/ThirdParty/MicrosoftRocketbox` come from the Microsoft Rocketbox Avatar Library and retain its MIT license file. Research use should cite Gonzalez-Franco et al. (2020), *The Rocketbox library and the utility of freely available rigged avatars*.
