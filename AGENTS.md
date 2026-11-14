# Active project instructions — Unity (user correction)

The user explicitly changed the engine to **Unity/C#**. These instructions take precedence over all web-stack references in the original brief below. Work on **one milestone per request**. Milestones 1–4 and the requested environment art pass are complete. Milestone 3 adds Arima opponent FSM, zone tells, feints, movement, stagger, and parry → recoil → counter window. The user subsequently requested internet-sourced samurai models: Arima now uses the bundled Grab3D CC0 rigged samurai, with source/license in Assets/Art/Samurai and prefab in Resources/Characters. Preserve its .meta references, the coastal art pass, and Dev A face-input boundary. Milestone 4 is implemented: face gameplay wiring through IFaceInput snapshots, buffered lean/kiai pulses, composure guard/clash effects, gaze tells, and HUD. The user has separately requested a finishing-blow cutscene on Arima victory: implement this focused cinematic before milestone 5. Preserve Dev A's start screen; do not begin the wider story/meditation milestone in this request.

- Engine: Unity 6.3 LTS, installed editor 6000.3.10f1. Built-in 3D renderer, procedural environment plus licensed downloaded character art (explicitly requested by the user). Desktop keyboard/mouse prototype.
- Project root contains Assets/, Packages/, ProjectSettings/. Open this folder with Unity Hub.
- Dev B owns Assets/Scripts/Core, World, and gameplay/story/HUD/pause/audio work plus Assets/Editor, except Dev A's start-screen work below.
- Dev A owns Assets/Scripts/Input/Face AND the start/title screen. The user says Dev A is currently building the start screen and will push it to GitHub later. Do not build a competing start screen or overwrite Dev A's screen assets/scripts. Its paths are not yet known: inspect the incoming commit before integration, preserve its design, and make only the gameplay handoff changes needed. Dev A's shared C# contract, real local MediaPipe bridge, and mock fallback are already integrated. Do not implement webcam tracking or edit their face-input files here.
- For the rest of the game, use suitable internet-sourced assets: characters, animation, environment models, textures, effects, audio, and UI art as applicable. This explicit user direction overrides the original procedural-only/no-external-assets brief. Choose assets that improve the requested realistic samurai look while retaining the ink-wash art direction; do not default to primitive stand-ins for finished content. Check each asset's license and retain source URLs, attribution, license text, and origin disclosures with imported files. Keep imports compatible with Unity's built-in renderer and account for performance.
- Before integration or publishing, fetch the shared GitHub branch and inspect Dev A's incoming changes. Their start-screen push is pending, not a blocker for independent gameplay work. Continue one milestone per request.
- Consume face input ONLY through IFaceInput from FaceInputFactory.createFaceInput(). Preserve the brief's camelCase field names, ranges, method names, and one-frame event semantics in C#. Call face.tick() at the start of every game Update, then getState().
- Game tuning: Assets/Scripts/Core/GameConfig.cs. DEBUG_INPUT remains true by default. Dev A's provider/fallback and camera override are integrated; consult their README for setup. F1 is owned by face diagnostics; Dev B field notes use F2.
- Use Unity's built-in legacy Input for this dependency-free scaffold. A later intentional Input System migration should preserve controls and the face boundary.
- The former Vite prototype is archived in work/web-prototype and is not the active game.

## Milestones (in strict order)
1. Scaffold: Unity/C#, first-person camera, game loop, face.tick() per frame, GameConfig.cs, procedural arena.
2. Combat: stances, strike/heavy, guard/parry, footwork/maai, stamina/health, hit-stop/shake/sparks.
3. Opponent FSM: Arima first, attacks/tells/feints/recoil/stagger/counter window.
4. Face gameplay wiring: dodge, kiai, composure, gaze, full debug HUD.
5. JSON narrative state machine, Honor/Fame/wounds/tally, cutscenes and choices.
6. Composure meditation and enso healing.
7. Content: Arima, Yoshioka, Kojiro, arenas and endings. Cut Yoshioka first if necessary.
8. Ink/paper postprocessing, sourced/procedural audio, pause menu, polish; integrate Dev A's title/start screen.

## Original design brief (reference; engine/path references overridden above)
# PROJECT BRIEF: "Ganryujima: Bounce Back"
A first-person, story-driven sword duel game based on the legend of Miyamoto Musashi (Japan, early 1600s), built for a hackathon with the theme **"Bounce Back"**. It must be 3D and use **computer vision for hybrid input**: the player's hands are on keyboard and mouse while their **face (webcam)** provides extra input simultaneously. It runs locally in the browser and is demoable in 3 to 5 minutes.

## Stack
Vite + vanilla TypeScript + Three.js. MediaPipe `@mediapipe/tasks-vision` FaceLandmarker (blendshapes + facial transformation matrix) in the browser. Web Audio API for sound. No external asset downloads required; use procedural geometry, shaders, and generated sounds. Commands: `npm install && npm run dev`. Do not add other dependencies without a strong reason.

Store the MediaPipe model at `/public/models/face_landmarker.task` and load it locally (the dev environment may be offline). Document in the README where to download it.

## Team structure (two owners, working in parallel, merged at the end)
- **Dev A: Face input.** Owns `/src/input/face/*` and `/public/face-test.html`.
- **Dev B: Everything else.** Owns the game: combat, arenas, story, cutscenes, visuals, audio, UI, and `/src/core/*`.

Stay in your own folders. Never edit the other owner's files. If you need a change to the shared contract, add a TODO comment and flag it in your summary.

## THE SHARED CONTRACT (do not change field names or ranges)
Both devs create (or already have) `/src/input/face/FaceState.ts` exactly as below. Dev A owns it; Dev B copies it verbatim until the merge.

```ts
export type Gaze = 'left' | 'center' | 'right' | 'unknown';
export type Wink = 'none' | 'left' | 'right';

export interface FaceState {
  tracking: boolean;       // face currently detected
  calibrated: boolean;     // calibration finished
  leanX: number;           // -1 (lean left) .. +1 (lean right), smoothed, 0 = neutral
  leanTrigger: -1 | 0 | 1; // one-frame event when a lean crosses threshold (dodge)
  kiai: boolean;           // one-frame event when a shout is detected (jaw open held ~120ms)
  composure: number;       // 0..1, smoothed. 1 = perfectly calm neutral face
  nod: boolean;            // one-frame event
  shake: boolean;          // one-frame event
  wink: Wink;              // held state: deliberate wink (>=250ms) of one eye
  gaze: Gaze;              // coarse horizontal gaze zone
  headYaw: number;         // degrees, smoothed
  headPitch: number;       // degrees, smoothed
  headRoll: number;        // degrees, smoothed
  timestamp: number;       // ms
}

export interface IFaceInput {
  start(): Promise<void>;       // request webcam, load model; must never throw (set tracking=false on failure)
  stop(): void;
  calibrate(durationMs?: number): Promise<void>; // default 5000ms neutral-face calibration
  tick(): void;                 // call once at the start of each frame; clears one-frame events
  getState(): Readonly<FaceState>; // called every frame by gameplay; must be cheap
}
```

One-frame events (`leanTrigger`, `kiai`, `nod`, `shake`) are true/non-zero for exactly one frame, cleared by `tick()`.

`/src/input/face/MockFaceInput.ts` implements `IFaceInput` using the keyboard so the game works with no camera:
Q/E = lean left/right (sets leanX and fires leanTrigger), F = kiai, N = shake, Y = nod, hold C = low composure (else high), Z/X/V = gaze left/center/right, G = toggle wink.
`createFaceInput()` (in `/src/input/face/index.ts`) returns the real implementation, or the mock if `DEBUG_INPUT` is on in `core/config.ts` or the webcam fails.

**Merge plan:** Dev B codes only against `IFaceInput` via `createFaceInput()`. At merge, Dev A's folder replaces Dev B's copy of the stub, and nothing else should need to change.

## Theme integration ("Bounce Back")
- Parry-riposte is the core loop: a deflected strike makes the enemy literally recoil/bounce back, opening a counter window.
- Losing a duel is not game over: the player is **wounded** (stamina/speed handicap) and continues. A meditation scene, where holding a calm face (composure) heals the wound, is how they bounce back.
- The end screen shows a "Bounce Back" tally (recoveries, parries, comebacks).

## Game design summary
- **First-person duels**, 1v1, small procedural arenas, visible katana in the lower view.
- **Mouse:** move to select a stance (UPPER/MIDDLE/LOWER/SIDE); left click strikes (hold for heavy); right click guards/parries (timed). **WASD:** footwork and distance (maai); A/D circle; Space retreats; Esc pauses.
- **Face mapping:**
  - Lean (`leanTrigger`) is a dodge with brief i-frames and a cooldown; `leanX` also nudges the camera.
  - Kiai staggers the opponent (cooldown ~8s) with screen shake and a shockwave.
  - Composure widens the parry window and slows time slightly during clashes when high; blurs screen edges and narrows the window when low.
  - Nod/shake answer yes/no choices (keyboard Y/N fallback).
  - Gaze (coarse) reveals enemy "tells" (feint vs. real attack) only when centered on the opponent; it must degrade gracefully.
  - Wink is optional, for a special move or menu selection.
- **Opponents (FSM AI):** IDLE -> CIRCLE -> WINDUP(zone, tell) -> STRIKE -> RECOVER, plus FEINT, STAGGERED, GUARD, DEAD/YIELD.
  1. Arima Kihei (tutorial; slow, predictable)
  2. Yoshioka Denshichiro (feints; pre-duel choice changes the arena)
  3. Sasaki Kojiro at Ganryujima (final boss; long reach, "Swallow Cut" signature move; beach arena at dawn)
- **Story:** data-driven narrative state machine (JSON/TS), in-engine cutscenes with letterbox, typewriter subtitles, Space to skip. Choices: spare or kill (Honor vs. Fame), approach to the Yoshioka challenge, arrive on time or late for Kojiro. Three endings: *The Undefeated*, *The Way*, *The Ronin*. Include respectful in-world historical blurbs noting that the accounts are legend-adjacent.
- **Look and feel:** minimalist ink-wash (sumi-e) 3D, limited palette (paper white, ink black, gray washes) with one red accent. Fog, petals/snow, a low red sun, paper-grain and ink-edge postprocessing. Sparse audio: wind, taiko, shakuhachi-like tone, metal clash, kiai, silence between exchanges.

## Quality bar
- 60 FPS target. Face detection throttled to ~30Hz and never blocks rendering.
- Fully playable with the keyboard fallbacks and never crashes on webcam denial or tracking loss.
- All thresholds, cooldowns, and windows live in `src/core/config.ts` (game) or `src/input/face/faceConfig.ts` (face).
- Clean, commented TypeScript. README with setup, controls table, theme explanation, credits, and a historical sources note.

## Working style
Work in small milestones, keep the project runnable after each, and summarize what changed and how to test it. Where something is unspecified, make a sensible choice and note it in the README.
