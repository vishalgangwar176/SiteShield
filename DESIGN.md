# SiteShield HUD Design System

## 1. Atmosphere & Identity

The interface is a restrained construction-site command console: calm under pressure, legible through a headset, and subordinate to the physical scene. Its signature is a low-occlusion bottom rail with a cyan status edge, compact peripheral telemetry, and professionally illustrated industrial UI parts instead of geometry primitives.

## 2. Color

| Role | Token | Value | Usage |
|---|---|---:|---|
| Void | `HudVoid` | `#071018` | Deep back layer |
| Panel | `HudPanel` | `#101C27` | Primary translucent surfaces |
| Chat glass | `ChatGlass` | `#101C27B8` | Conversation shell at 72% opacity so the NPC remains visible |
| Panel raised | `HudPanelRaised` | `#172735` | Chips and progress track |
| Text primary | `HudTextPrimary` | `#F4F8FB` | Titles and actionable feedback |
| Text muted | `HudTextMuted` | `#9FB0BF` | Labels and controls |
| Accent | `HudAccent` | `#37D6C0` | Current mission and positive state |
| Warning | `HudWarning` | `#F2B84B` | Caution and false-positive state |
| Danger | `HudDanger` | `#F0645B` | Hazard/error state |
| Edge | `HudEdge` | `#426174` | Panel rim and divider |

The accent is reserved for current state and progress. Warning and danger always pair color with text.

## 3. Typography

The HUD uses the Kenney UI pack's Future Narrow font for titles, labels, scores, and controls. The chat transcript and input use Unity's bundled runtime body font for denser long-form readability. Data uses tabular-looking fixed-width formatting.

| Level | Size on 1200×700 canvas | Weight | Usage |
|---|---:|---:|---|
| Mission title | 30 | 600 | Current site |
| Feedback | 25 | 500 | Inspection result / next action |
| Score | 28 | 600 | Four-digit score |
| Label | 16 | 600 | Eyebrow and section label |
| Hint | 16 | 500 | Controls and supporting status |

## 4. Spacing & Layout

The base unit is 4 canvas pixels. The HUD uses a 1200×700 world-space canvas within the headset safe area.

| Token | Value | Usage |
|---|---:|---|
| `Space1` | 4 | Optical correction |
| `Space2` | 8 | Icon-to-label |
| `Space3` | 12 | Compact inset |
| `Space4` | 16 | Standard inset |
| `Space6` | 24 | Card padding |
| `Space8` | 32 | Major cluster gap |

The mission chip anchors top-left, score anchors top-right, and the feedback rail stays bottom-center. No persistent element occupies the center 55% of the view.

## 5. Components

### Mission chip
- Structure: Kenney 9-slice shell, status rail, overline, site title.
- Variants: campus, active site, complete.
- States: cyan active and green-complete copy treatment.
- Accessibility: title remains visible without depending on color.
- Layout: top-left peripheral cluster.

### Score cluster
- Structure: compact Kenney shell, score label, four-digit value.
- Variants: normal and mission complete.
- Accessibility: high-contrast text and stable tabular width.
- Layout: top-right peripheral cluster.

### Feedback rail
- Structure: Kenney 9-slice shell, state icon, feedback text, progress track, progress label, control hints.
- Variants: neutral, success, warning, danger, complete.
- States: copy and icon change with inspection outcome; progress fill updates from reviewed targets.
- Accessibility: every color state has a text label; minimum body size is 25.
- Motion: 180 ms opacity transition only when visibility changes.
- Layout: bottom-center; maximum width 760 and height 154.

### Chat panel
- Structure: Kenney 9-slice shell, coach identity, response region, input, send action.
- Surface: `ChatGlass` on the outer shell; input and action controls remain opaque for legibility.
- Variants: idle, waiting, response, error.
- States: keyboard focus, disabled while waiting, error copy.
- Accessibility: high contrast, explicit text prompt, Enter submits.
- Layout: right-sidecar world-space dialog shown only during conversation, offset about 29 degrees right and slightly below eye level so the central 55% remains clear for the NPC face, torso, and complete arm gestures.

## 6. Motion & Interaction

Only opacity and scale may animate. Visibility changes use 180 ms ease-out; state changes are immediate so hazard feedback is not delayed. Reduced-motion mode uses immediate transitions. HUD interaction remains through the existing inspect, coach, and close controls.

## 7. Depth & Surface

The strategy is mixed tonal shift plus asset-authored rim detail. Kenney CC0 9-slice sprites provide the professional frame geometry. A deep translucent back layer, raised inner layer, 1 px cool rim, and subtle directional shadow provide depth. No cube mesh is permitted in the HUD.

## 8. Accessibility Constraints & Accepted Debt

### Constraints

- Body text contrast targets WCAG 2.2 AA even though the surface is a world-space game HUD.
- Persistent content remains outside the central 55% view to reduce occlusion and discomfort.
- State meaning never relies on color alone.
- Input remains keyboard-accessible for desktop learners; VR selection continues through XR interactors.
- Text is English-only in the current training content, so CJK wrapping is not applicable to this build.

### Accepted Debt

| Item | Location | Why accepted | Owner / Exit |
|---|---|---|---|
| No controller-specific glyph variants | Feedback rail | Device binding discovery is not yet implemented | Add when final headset target is selected |
| No learner-selectable HUD scale | Training HUD | Current scale is comfort-tuned for the prototype rig | Add in accessibility settings milestone |

## Asset Provenance

Kenney UI Pack - Sci-Fi 2.0, Creative Commons CC0. Downloaded from the official Kenney asset page and stored under `Assets/ThirdParty/Kenney/UI-Pack-SciFi`.
