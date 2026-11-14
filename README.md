# Ganryujima: Bounce Back — Unity

**Unity 6.3 LTS (6000.3.10f1), C#, built-in renderer. Milestones 1–2 complete.** A first-person combat practice ground with a procedural arena, katana, stamina, directional defense, and keyboard face mock. The original web prototype is archived in work/web-prototype. AGENTS.md contains the full brief and Unity engine override.

## Open and play

1. Clone the repository (or pull the latest main branch if you already have it).
2. Unity Hub → Add project from disk → select the root folder containing Assets, Packages, and ProjectSettings. Use 6000.3.10f1.
3. Open Assets/Scenes/PracticeGround.unity and press Play. Click Game view, then **Enter the practice ground**.
4. Walk toward the post until the maai readout says **STRIKING DISTANCE**. Try light/heavy strikes, then match the announced incoming zone and time RMB just before its progress bar fills.

If the scene is missing, choose **Ganryujima → Create practice scene**. The scene generates geometry at runtime; no downloaded art, webcam, face model, or extra package is required.

## Combat controls

| Input | Action |
| --- | --- |
| WASD | Footwork / strafing, with collision and court boundary |
| Mouse | Look and select stance with a directional flick |
| Mouse up / down / left / right | Upper / Lower / Middle / Side stance |
| 1 / 2 / 3 / 4 | Upper / Middle / Lower / Side fallback |
| LMB press and release | Light strike |
| Hold LMB for 0.55 s, release | Heavy strike; requires more stamina |
| RMB press | Begin directional guard; first 0.18 s is the parry window |
| Hold RMB | Block matching-zone strikes, consuming stamina |
| Space | Short backward retreat, stamina cost and cooldown; no i-frames |
| Esc | Freeze practice and release the cursor |
| R | Reset player/post health, stamina, rhythm, and position; re-enter practice |

Strikes resolve once after wind-up, using distance and a forward aim cone. Missing still costs stamina. Heavy strikes have more damage, reach, and recovery time. Guard/charge/attack states slow movement. Guarding continuously drains stamina; a late matching guard blocks at additional cost. A wrong zone or an attack from behind hits health. Exhausting guard breaks it and briefly prevents attacks/guard. Releasing guard has a short rearm delay, so repeated clicks cannot keep a permanent parry window. Stamina regenerates after a delay while resting.

## Practice exchange (milestone 2)

The practice post repeats a **fixed rhythm**, cycling Upper → Middle → Lower → Side. A gold tell and filling bar announce impact. The rhythm starts when you approach and resets when you leave its activation range. Retreat out of reach to avoid impact. Match stance and face the post, then time RMB near the end of the tell to parry. Parries refund some stamina, recoil the post, and emit sparks. Hits, blocks, and parries give local hit-stop and camera shake.

This is a combat test fixture, not Arima or an opponent FSM. It has no adaptive behavior, feints, AI movement, or authored counter window. Those belong to milestone 3. The post can be defeated, and the player can lose; either result offers a reset. Narrative loss/wounds/bounce-back tally are later milestones; the displayed parry count is for the current practice session only.

Pausing or losing focus cancels held charge/guard, freezes combat and the practice timeline together, and keeps face.tick() running. Hit-stop uses a local gameplay clock; Unity's global Time.timeScale remains unchanged. Effects use a fixed pool of 16 spark meshes rather than spawning objects on each clash.

## Face mock (readout only)

| Input | Mock value |
| --- | --- |
| Q / E | Lean left/right plus one-frame leanTrigger |
| F | Kiai pulse |
| Y / N | Nod/shake pulse |
| Hold C | Low composure; otherwise high |
| Z / X / V | Gaze left/center/right |
| G | Toggle left wink |

Face-driven dodges, kiai, composure changes to parry/time, and gaze tells are intentionally not wired until milestone 4. The current parry window is a fixed game-config value.

## Code and ownership

- Assets/Scripts/Core/GameLoop.cs: bootstrap, frame loop, keyboard/mouse input, movement, cursor capture, reset.
- Assets/Scripts/Core/GameConfig.cs: game thresholds, costs, damage, ranges, cooldowns, windows, and DEBUG_INPUT.
- Assets/Scripts/Combat/CombatModel.cs: deterministic stamina/health/strike/guard/parry rules; no input or rendering dependency.
- Assets/Scripts/Combat/PracticeExchange.cs: fixed training rhythm and zone tells, separate from future AI.
- Assets/Scripts/Combat/CombatPresentation.cs: procedural sword poses, local hit-stop, shake, recoil, pooled sparks.
- Assets/Scripts/UI/CombatHud.cs: health/stamina, maai, stance/charge, tell, face readout, entry/reset prompts.
- Assets/Scripts/World/ArenaBuilder.cs: procedural court and scenery.
- Assets/Scripts/Input/Face: Dev A's shared C# contract and mock, integrated from their parallel commit; no Dev B gameplay changes in this folder.
- Assets/Editor: scene generation, deterministic regression checks, and Play Mode smoke check.

Gameplay consumes only IFaceInput through FaceInputFactory.createFaceInput(), calling tick() first each frame then getState(). FaceState is a value snapshot with the original field names/ranges, enums for gaze/wink, Task async methods, and millisecond timestamps. Dev A's shared C# contract/mock commit is integrated. The factory currently returns MockFaceInput. **Dev A integration TODO:** preserve this shared interface and add real webcam/model lifecycle, DEBUG_INPUT switching, and failure fallback. Dev B has not implemented face tracking.

Input uses Unity's built-in legacy Input Manager. Resources material templates retain shader references in builds. Close Play Mode before pulling changes from GitHub.

## Verification

Unity 6000.3.10f1 compiled milestone 2 and passed **38 deterministic combat assertions**, covering single-hit resolution, aim/range misses, heavy costs, action locks, regeneration, timing/zone/facing guard rules, breaks/rearm, retreat cost/cooldown, pause, practice rhythm, defeat, and retry. Run **Ganryujima → Check combat rules** to repeat these checks. The batch method Ganryujima.Editor.ProjectSetup.Smoke runs those checks and enters Play Mode, verifying combat/camera/controller initialization and ten face-loop frames.

Validation ran in an isolated project copy so it did not close the working editor or replace its scene. Interactive controls/rendered appearance were not automated. Unity's internal editor SearchDatabase startup code emitted an indexing exception; no gameplay-code exceptions were observed.

## Next milestone

Milestone 3: Arima's opponent FSM, zone attacks, wind-up tells, feints, recoil/stagger, and parry → recoil → counter window. Continue one milestone per request.

## Theme and credits

The core theme is parry → bounce back → counter. Future duel losses cause wounds healed by composure meditation. Unity supplies the engine; all geometry is procedural. Future narrative adapts legend-adjacent Musashi accounts rather than claiming historical reconstruction; document specific historical sources as story content is added.
