# Ganryujima: Bounce Back — Unity

Active engine: **Unity 6.3 LTS, 6000.3.10f1**, C#, built-in 3D renderer. Milestone 1 only: first-person exploration scaffold. The old web prototype is archived in work/web-prototype. AGENTS.md contains the engine correction and full brief.

## Open and play

1. Unity Hub → Add project from disk → select this root folder containing Assets, Packages, ProjectSettings. Use editor 6000.3.10f1.
2. Open Assets/Scenes/PracticeGround.unity. If missing, choose **Ganryujima → Create practice scene** from the editor menu.
3. Press Play, click Game view, then **Enter the practice ground**.

The arena, lighting, scenery, marker, player, and sword are generated when Play starts. No imported art, webcam, MediaPipe package, or face model is required. The entry gate captures/releases the cursor; a full title/pause system belongs to milestone 8.

| Input | Action |
| --- | --- |
| WASD | Walk/strafe inside the court |
| Mouse | First-person look while captured |
| Esc | Release cursor and show entry gate |
| Q / E | Mock lean left/right |
| F | Mock kiai pulse |
| Y / N | Mock nod/shake pulse |
| Hold C | Mock low composure; otherwise high |
| Z / X / V | Set mock gaze left/center/right |
| G | Toggle mock left wink |

Mock values currently affect the readout only. Combat, dodging, camera lean, story, meditation, and AI are future milestones. Katana/marker are static. Space has no action yet. Movement is normalized on diagonals, follows yaw independently of pitch, collides with the practice marker, and stays inside the court.

## Code and ownership

- Assets/Scripts/Core/GameLoop.cs: bootstrap, frame loop, movement, cursor gate, minimal readout.
- Assets/Scripts/Core/GameConfig.cs: game tuning and DEBUG_INPUT.
- Assets/Scripts/World/ArenaBuilder.cs: procedural primitives and materials.
- Assets/Scripts/Input/Face: temporary Dev B bootstrap; replace with Dev A's C# implementation at merge.
- Assets/Editor/ProjectSetup.cs: scene generation and command-line smoke check.

Gameplay consumes only IFaceInput through FaceInputFactory.createFaceInput(). It calls face.tick() first each frame, then reads face.getState(). FaceState is a value snapshot; changing a returned copy cannot mutate the provider. Fields/ranges and camelCase method names match the original contract; C# uses Task for async calls, enums for gaze/wink, and milliseconds for timestamps. One-frame pulses use GetKeyDown and reset on tick. Mock tracking/calibration mean simulated readiness, not webcam access.

**Dev A merge TODO:** Unity needs a C# face provider; the original TypeScript implementation cannot be merged directly. Preserve the ported interface, factory, fields, ranges, and frame semantics. The bootstrap always returns MockFaceInput, including when DEBUG_INPUT is false. Dev A must supply the real provider, DEBUG_INPUT switching, and webcam/model failure fallback. Dev B has not selected or implemented a Unity face-tracking dependency.

This dependency-free scaffold uses Unity's built-in legacy Input, with Input Manager selected in Active Input Handling. Mouse look uses default Mouse X/Y axes. All art is procedural; no runtime downloads. The procedural scene is recreated each Play session.

## Validation

The editor menu creates the scene and includes it in Build Settings. The command-line method Ganryujima.Editor.ProjectSetup.Smoke enters Play Mode, checks the camera/controller, waits for ten game frames with face.tick(), and exits with a result. This does not replace interactive movement/rendering checks.

## Next

Milestone 2: mouse stances, strike/heavy strike, guard/parry, maai, health/stamina, hit-stop, shake, sparks. Keep one milestone per request. Arima AI follows in milestone 3.

## Theme and credits

The planned Bounce Back loop is parry → recoil → counter; wounds from lost duels can be healed through composure meditation. This milestone establishes the practice space. Unity supplies the engine; geometry is procedural. Future story adapts legend-adjacent Musashi accounts rather than claiming historical reconstruction; document specific sources when narrative content is added.

### Checks performed for this delivery

Unity 6000.3.10f1 compiled the C# scripts and passed the headless Play Mode smoke check (camera/controller created; face loop ticked ten frames). Interactive mouse/keyboard behavior and rendered appearance were not automated. Unity's internal SearchDatabase indexing code emitted an editor exception during startup; no gameplay-code exception was observed. Material templates in Resources retain the procedural scene's shader references for builds.
