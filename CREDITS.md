# Ganryujima: Bounce Back — credits and sources

Game: Unity/C# first-person fiction inspired by the Musashi legends. Dev B owns gameplay, world, story and presentation; Dev A owns the local face tracker and upcoming start screen.

## Included third-party art and sound

| Asset | Source / license | Included provenance |
| --- | --- | --- |
| Rigged samurai and walk animation | [Grab3D Samurai](https://grab3d.com/models/characters/samurai/) — CC0 | Assets/Art/Samurai/LICENSE.txt, SOURCE.md and source-manifest.json |
| Coastal sand color and normal textures | [Poly Haven Coast Sand 01](https://polyhaven.com/a/coast_sand_01) — CC0 | Assets/Resources/Art/Coast/LICENSE.md and source-manifest.json |
| Finishing sword slash | [SFXMint Sharp Sword Slash With Metal Ring 03](https://sfxmint.com/sounds/impact-sword-03) — CC0 | Assets/Resources/Audio/Finisher/LICENSE.md and source-manifest.json |
| Meditation bowl | [SFXMint Temple Bell 01](https://sfxmint.com/sounds/temple-bell-01) — CC0 | Assets/Resources/Audio/Meditation/LICENSE.md |
| Wind, ocean and sword clash | [Wind 55](https://sfxmint.com/sounds/ambience-wind-55), [Ocean 53](https://sfxmint.com/sounds/ambience-ocean-53), [Clash 01](https://sfxmint.com/sounds/sword-clash-01) — CC0 | Assets/Resources/Audio/Atmosphere/LICENSE.md, including SHA256 hashes |

The Grab3D character is AI-assisted art according to its source (Meshy). All SFXMint sounds above are AI-generated or procedurally synthesized according to their sources. Poly Haven supplies photographic material scans. Arima, Yoshioka and Kojiro are three palette/weapon variants of the same imported character, not separate downloaded models or historical portraits.

Environment meshes, katana geometry, cloth/paper shaders, enso, drifting-particle sprite, taiko/breath-flute/kiai-like synthesis and other procedural effects are authored in the game code. The dynamic Georgia/Times fallback uses system fonts; no font files are distributed.

## Camera dependencies

Dev A's optional companion tracker uses [MediaPipe Tasks Vision 0.10.21](https://www.npmjs.com/package/@mediapipe/tasks-vision), [MediaPipe](https://github.com/google-ai-edge/mediapipe) and the official Face Landmarker model. See Assets/Scripts/Input/Face/README.md, vendor package notices and Documentation/Licenses/MediaPipe-LICENSE.txt for setup, architecture and licensing. Third-party runtime/model files are installed by Dev A's setup script and copied beside the Windows build; they are intentionally not committed to this repository. Camera frames remain on the local machine.

## Historical context

[Japan Tourism Agency: Ganryujima](https://www.mlit.go.jp/tagengo-db/en/R5-00678.html) and [Shimonoseki official tourism](https://shimonoseki.travel/about-en/) provide location/duel background. Dialogue, mercy decisions, venue choices, endings and the playable Swallow Cut are authored game fiction. This is not a historical reconstruction or a claim that the branching events occurred.

Sword motion: [Quaternius Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html), CC0. Resampled/adapted hand and chest tracks; provenance and source hash retained in Assets/Resources/Animation/SOURCE.md, with original license beside it.

Fight soundtrack: live Taiko performed by Singapore Wind Symphony's Percussion Ensemble (composition credited to Scott Harding), listed as CC0 public domain at https://commons.wikimedia.org/wiki/File:02_Taiko2_(short).oga . Source credits/license and original verified SHA1 are retained in Assets/Resources/Audio/Battle/LICENSE.md. Converted Ogg loops with overlapped ends, fades with combat, follows Music volume and pauses with the game. 224 rule assertions and polish integration checks pass with the clip loaded.
