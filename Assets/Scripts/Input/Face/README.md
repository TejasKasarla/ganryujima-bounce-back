# Dev A: Face Input

This is the Unity/C# adaptation of the face-input brief. Nothing in Dev B's core, world, scenes, packages, or game configuration is modified. The shared `FaceState` fields and ranges are unchanged. `IFaceInput` retains `start`, `stop`, `calibrate`, `tick`, and `getState`; the factory retains its zero-argument entry point.

## Architecture and limitations

The requested `@mediapipe/tasks-vision` package is a browser library, not a Unity C# library. Rather than silently introducing a native Unity plugin or changing the team's packages, this implementation runs the real FaceLandmarker in a companion local Chrome/Edge tab. A worker performs inference and posts samples to Unity's loopback-only HTTP service. C# owns calibration, smoothing, gesture timing, and `FaceState`.

Keep the companion tab and its camera running during the demo. Chrome/Edge's transferable `MediaStreamTrackProcessor` path keeps inference off both render threads. Other compatible browsers use a throttled bitmap-capture fallback. An OS/browser may suspend a minimized or sleeping camera tab: tracking decays and a disconnected tracker eventually selects keyboard fallback. A completely native, single-window tracker would require a separately agreed Unity MediaPipe integration.

The first-person game remains a Unity desktop application, not the archived Vite prototype. `public/face-test.html` is a standalone diagnostic page with no game imports. It must be served by the bridge, not opened as a `file:` URL. WebGL builds are not supported by this desktop HTTP bridge and fall back to the mock if startup fails.

## Setup

Run once from the repository root in PowerShell, while online:

```powershell
.\Assets\Scripts\Input\Face\Tools\setup-face.ps1
```

This vendors the pinned MediaPipe `0.10.21` npm distribution and the official float16/1 model into this owned folder. After setup, runtime uses only localhost; it does not fetch models, WASM, scripts, images, or video from the internet. Camera frames remain on this device. Microphone permission is never requested.

- Runtime: `Web/vendor/vision_bundle.mjs` and `Web/vendor/wasm/`.
- Model on disk: `Models/face_landmarker.task`.
- Model URL served to MediaPipe: `/models/face_landmarker.task`, matching the original brief's local URL without writing into Dev B's asset folders.
- Official model download: <https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task>.
- Pinned npm archive: <https://registry.npmjs.org/@mediapipe/tasks-vision/-/tasks-vision-0.10.21.tgz>.
- Expected model SHA256: `64184e229b263107bc2b804c6625db1341ff2bb731874b0bcc2fe6544e0bc9ff`.

For an offline development machine, transfer the archive and model from another machine, then run:

```powershell
.\Assets\Scripts\Input\Face\Tools\setup-face.ps1 -OfflinePackage C:\transfer\tasks-vision-0.10.21.tgz -OfflineModel C:\transfer\face_landmarker.task
```

These large third-party files are intentionally ignored by Git. Each developer must run setup or copy the prepared vendor/model directories before an offline demo. The setup preserves the npm license notices. A face-owned post-build callback copies the prepared assets and diagnostic page beside Windows/Linux builds or inside a macOS application bundle.

## Unity usage

1. Open the project with Unity `6000.3.10f1`.
2. Select `Ganryujima > Face > Open standalone test` to run the tracker without game code. Click **Start camera**, grant camera permission, and let the five-second neutral-face countdown finish.
3. For the game, enable `Ganryujima > Face > Use camera next Play`, then enter Play Mode. This session-only override does not edit `GameConfig.cs`. The game opens the companion tracker; start its camera.
4. Press **F1** in the game to toggle the preview, landmarks, event flashes, live signals, and calibration control.
5. To return to keyboard-only mode, stop Play Mode and turn the session override off.

For a release camera build, Dev B can deliberately set `GameConfig.DEBUG_INPUT = false`, or explicitly call `FaceInputFactory.createFaceInput(false)`. The default remains the existing debug flag. TODO(Dev B): the current game HUD text is hardcoded to say keyboard mock; rename that label when integrating real camera mode. No contract change is required.

The preview auto-mounts once per run. It is also reusable:

```csharp
IFaceInput face = FaceInputFactory.createFaceInput();
await face.start();
var preview = gameObject.AddComponent<WebcamPreview>();
preview.Mount(face);
// Every game frame, before consuming input:
face.tick();
FaceState state = face.getState();
```

The game owns provider lifetime and calls `stop` on shutdown. The preview does not stop or create providers. Duplicate overlays are avoided. JPEG previews are generated only while the overlay is visible, at a lower rate than inference.

## State semantics

- `start()` catches camera-service startup failures; denied camera permission, a missing model, GPU/CPU initialization failures, and lost browser connection select the keyboard mock through `FallbackFaceInput`.
- While permission/model loading is pending, the game already has working mock input. Once the browser reports ready, a visible but undetected face is represented by real tracking loss, not fake successful camera tracking.
- The first detected face automatically starts calibration. Rest still, eyes open, mouth relaxed, looking ahead. Recalibration cancels/completes the previous calibration request. Calibration fails cleanly if there are too few samples or insufficient tracked time.
- Calibration records neutral yaw/pitch/roll, lateral position, and all resting control blendshapes. Welford variance estimates personalize jaw/blink thresholds and head-stability tolerance. Head angles and lean are relative to that user's neutral pose. Calibration data stays in memory.
- Pose/lean use a time-based EMA; composure and head velocity have separate smoothing. Signal values are clamped to the shared ranges. No actions are emitted before successful calibration.
- Kiai requires a continuous jaw-open hold and a release before rearming. Nod/shake require opposite angular excursions followed by a return toward neutral; a cooldown and cross-axis dominance reject ambiguous motion.
- Winks require one eye closed and the other open continuously for at least 250 ms. Bilateral/natural short blinks do not wink; closed eyes give unknown gaze. Coarse gaze is not precise screen-coordinate eye tracking.
- `tick()` first clears one-frame events, then drains a bounded queue and computes the current frame. `getState()` is just a value snapshot. Multiple reads do not consume events.
- Tracking loss holds pose/continuous values for 300 ms, but never repeats one-frame events. It then sets `tracking=false`, clears wink/gaze, and exponentially decays lean/angles to zero and composure to one. Calibration survives a temporary tracking loss.
- Browser event counters are diagnostic metadata, not extra `FaceState` fields. They ensure event flashes survive polling between one-frame pulses.
- The bridge binds only `127.0.0.1`, selects an available configured port, rejects foreign origins/hosts, requires a per-run token for API calls, validates samples, and caps request/queue sizes. Stop releases the listener, camera tracks, worker, and pending calibration.

## Keyboard fallback

| Key | Signal |
| --- | --- |
| Q / E | Held lean left/right; one threshold-crossing pulse; opposite keys cancel |
| F | Kiai pulse |
| Y / N | Nod / shake pulse |
| Hold C | Low composure; release returns to high |
| Z / X / V | Gaze left / center / right |
| G | Toggle held left wink |
| F1 | Toggle Unity webcam diagnostic overlay |

## Tuning guide

Change every numeric control threshold/window in [FaceConfig.cs](FaceConfig.cs), then recompile/restart the tracker. Browser settings are exported from that file by the bridge; do not duplicate tuning in JavaScript. Game cooldowns (for example the eight-second kiai ability cooldown) still belong to Dev B, not this signal layer.

| Setting | Default | Meaning / tuning direction |
| --- | --- | --- |
| `DetectionHz` | 30 | Maximum target inference rate; lower to reduce CPU/GPU load. |
| `TrackingHoldMs` | 300 | Time to retain the last tracked continuous state. |
| `TrackingDecayMs` | 450 | Exponential decay time constant after hold. |
| `PoseSmoothingMs` | 90 | EMA time constant for pose/lean; lower is faster/noisier. |
| `ComposureSmoothingMs` | 250 | Composure EMA time constant. |
| `CalibrationDurationMs` | 5000 | Default neutral calibration duration. |
| `CalibrationMinimumSamples` | 45 | Minimum usable calibration observations. |
| `CalibrationCoverage` | 0.5 | Required fraction of the interval with tracked sample coverage. |
| `CalibrationNoiseMultiplier` | 3 | Standard deviations used for personal thresholds. |
| `CalibrationMaximumSampleGapMs` | 100 | Maximum gap counted as tracked calibration coverage. |
| `CalibrationJawMargin` | 0.2 | Minimum jaw increase above rest before applying global floor. |
| `CalibrationBlinkMargin` | 0.35 | Minimum blink increase above rest before global floor. |
| `MaximumJawThreshold` | 0.9 | Ceiling on personalized jaw threshold. |
| `MaximumBlinkThreshold` | 0.95 | Ceiling on personalized closed-eye threshold. |
| `MinimumDetectionConfidence` | 0.5 | Face detector confidence floor. |
| `MinimumPresenceConfidence` | 0.5 | Landmark presence confidence floor. |
| `MinimumTrackingConfidence` | 0.5 | Tracking confidence floor. |
| `LeanRollDegrees` | 18 | Roll giving unit roll contribution. |
| `LeanYawDegrees` | 28 | Yaw giving unit yaw contribution. |
| `LeanLateralDistance` | 0.16 | Normalized camera-width offset giving unit lateral contribution. |
| `LeanRollWeight` | 0.45 | Roll share of combined lean. |
| `LeanYawWeight` | 0.2 | Yaw share of combined lean. |
| `LeanLateralWeight` | 0.35 | Lateral share of combined lean; weights should sum to one. |
| `LeanTriggerThreshold` | 0.5 | Absolute smoothed lean required for a dodge pulse. |
| `LeanReleaseThreshold` | 0.25 | Return below this to rearm lean. |
| `LeanCooldownMs` | 500 | Input-layer lean debounce, independent of game dodge cooldown. |
| `JawOpenThreshold` | 0.38 | Minimum absolute jaw score after calibration adaptation. |
| `JawReleaseRatio` | 0.55 | Hysteresis ratio from rest to threshold to rearm kiai. |
| `KiaiHoldMs` | 120 | Continuous jaw-open hold duration. |
| `KiaiCooldownMs` | 750 | Input-layer shout debounce, not ability cooldown. |
| `BrowRange` | 0.5 | Brow-down change producing full brow tension. |
| `SquintRange` | 0.6 | Squint change producing full squint tension. |
| `MouthRange` | 0.45 | Mouth opening/expression change producing full mouth tension. |
| `HeadSpeedRange` | 100 | Degrees/second producing full instability; adapted upward for calibration noise. |
| `HeadSpeedSmoothingMs` | 180 | Head-angular-speed EMA time constant. |
| `MouthSpeedRange` | 3 | Blendshape-score/second producing full movement tension. |
| `BrowWeight` | 0.25 | Brow share of composure penalty. |
| `SquintWeight` | 0.2 | Squint share of penalty. |
| `MouthWeight` | 0.3 | Mouth share of penalty. |
| `HeadStabilityWeight` | 0.25 | Head stability share; penalty weights should sum to one. |
| `NodAmplitudeDegrees` | 9 | Pitch excursion required on each side of neutral. |
| `ShakeAmplitudeDegrees` | 13 | Yaw excursion required on each side of neutral. |
| `GestureReleaseRatio` | 0.45 | Relative neutral-return zone completing nod/shake. |
| `GestureAxisDominance` | 1.15 | Reject when the other axis dominates by this factor. |
| `GestureWindowMs` | 1200 | Maximum time for an oscillation and return. |
| `GestureCooldownMs` | 900 | Shared nod/shake debounce. |
| `BlinkClosedThreshold` | 0.65 | Global closed-eye floor before per-user adaptation. |
| `BlinkOtherEyeMaximum` | 0.3 | Maximum open-other-eye score for a deliberate wink. |
| `WinkHoldMs` | 250 | Minimum unilateral closure duration. |
| `GazeThreshold` | 0.18 | EyeLook horizontal difference entering left/right. |
| `GazeReleaseThreshold` | 0.1 | Hysteresis threshold returning toward center. |
| `GazeHoldMs` | 100 | Stable gaze candidate duration. |
| `MockCalmComposure` | 0.95 | Default keyboard composure. |
| `MockLowComposure` | 0.15 | Keyboard composure while C is held. |
| `BridgePort` | 8765 | First localhost port; never binds other interfaces. |
| `BridgePortAttempts` | 8 | Number of consecutive ports attempted if occupied. |
| `BridgeConcurrency` | 4 | Maximum concurrent HTTP handlers. |
| `RequestTimeoutMs` | 5000 | Local fetch/read timeout. |
| `ModelLoadTimeoutMs` | 45000 | Camera-worker initialization timeout. |
| `EventFlashMs` | 300 | Browser/Unity diagnostic flash duration. |
| `WebcamWidth` | 640 | Ideal capture width; camera may negotiate another size. |
| `WebcamHeight` | 480 | Ideal capture height. |
| `MaximumLandmarkValues` | 1434 | Packet cap: 478 XYZ landmarks. |
| `PoseYawSign` | 1 | Change to -1 only if hardware/pose convention reverses yaw. |
| `PosePitchSign` | 1 | Pitch sign convention. |
| `PoseRollSign` | -1 | Mirrored player-left/right roll convention. |
| `MaximumRequestBytes` | 262144 | Maximum JSON/JPEG request size. |
| `MaximumPendingSamples` | 8 | Queue cap; old samples are dropped instead of blocking rendering. |
| `BrowserHeartbeatTimeoutMs` | 3000 | Missing browser samples before keyboard fallback. |
| `PreviewHz` | 8 | Maximum JPEG preview rate while F1 overlay is visible. |
| `PreviewWidth` | 320 | Preview JPEG width, preserving camera aspect ratio. |
| `PreviewJpegQuality` | 0.65 | JPEG quality; lower reduces preview traffic/decode cost. |

If neutral head motion triggers dodges, recalibrate first, then raise `LeanTriggerThreshold` or reduce lateral/roll weights. If normal speech triggers shouts, raise `JawOpenThreshold` or `KiaiHoldMs`. For missed deliberate winks, check lighting/other-eye score before lowering `WinkHoldMs`; retaining at least 250 ms is recommended. Gaze is deliberately coarse and may remain unknown when the eyes cannot be observed reliably.

## Validation and standalone server without Unity

.NET 10 is needed only for these developer checks, not by the Unity game:

```powershell
dotnet run --project Assets/Scripts/Input/Face/Tests/FaceInput.Checks.csproj --artifacts-path "$env:TEMP\Ganryujima.FaceChecks.Validation"
node Assets/Scripts/Input/Face/Tests/browser-checks.mjs
node --experimental-vm-modules Assets/Scripts/Input/Face/Tests/browser-lifecycle.mjs
dotnet run --project Assets/Scripts/Input/Face/Tests/FaceInput.Checks.csproj --artifacts-path "$env:TEMP\Ganryujima.FaceDemo" -- --serve "$PWD"
```

Open the localhost URL printed by the last command, normally <http://127.0.0.1:8765/>. Stop with Ctrl+C. Keep demo/check artifact directories separate on Windows: an active server locks its executable. Test source has `.cs.txt` extensions so Unity does not accidentally compile the fake API shim. Build output is placed outside `Assets`.

The checks cover the frozen contract, keyboard controls, edge timing, calibration success/failure, composure, gaze/winks, nod/shake, invalid data, tracking hold/decay, localhost authentication/origin protection, and lifecycle cleanup. The browser check enforces tuning-guide coverage for every config constant. The C# harness uses a minimal Unity API shim; this is not a substitute for compiling and running the real Unity editor. Test live camera movement and both desktop/browser focus states on the demo machine before presenting.

### Delivery checks

- The contract/config/mock first milestone was committed and pushed first as `e9ab776`, so Dev B could consume the interface before the camera implementation.
- 93 C# contract/signal/bridge assertions executed successfully before the final factory compatibility and lifecycle refinements. The latest sources, including their added regression cases and the preview, compile successfully with the .NET harness. Windows Smart App Control blocked executing the newest generated test assembly; no security policy was changed or bypassed.
- Browser packet conversion, all 72 tuning-guide entries, Unity script GUID checks, JavaScript syntax, permission denial, overlapping starts/stops, stale worker errors, and camera/worker release checks passed.
- The real pinned local model initialized on GPU. Forcing GPU creation to fail successfully selected CPU. A temporary official test portrait used as synthetic video produced face landmarks, completed calibration, centered neutral pose/gaze, and composure near 1.0; the test portrait was removed afterward. Deterministic packets produced exactly one lean and one kiai event with visible diagnostic flashes.
- Desktop 1440x900 and mobile 390x844 DOM/layout checks found no horizontal overflow or overlapping panels; preview/landmark pixels and screenshots were inspected. Measured inference varied with load around the configured 30 Hz target; 60 FPS Unity rendering has not been measured here.
- Remaining demo-machine checks: compile in the real Unity editor, verify F1 preview/camera override and packaged build assets, grant a physical webcam, and tune gestures against a live face. The project's Unity editor was not available for a Play Mode check on this device.

## Credits

Google MediaPipe Tasks Vision and the official FaceLandmarker model provide detection, blendshapes, and the facial transform; see <https://ai.google.dev/edge/mediapipe/solutions/vision/face_landmarker/web_js> and the vendored package notices. No face images or downloaded art are shipped. This folder does not implement story/combat/history content; those remain Dev B's responsibility and the original legend-adjacent historical disclaimer still applies.