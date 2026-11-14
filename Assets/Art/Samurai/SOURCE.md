# Samurai character source

Provider: Grab3D
Model page: https://grab3d.com/models/characters/samurai/
Downloaded FBX: https://grab3d.com/dl/samurai-rigged/samurai-rigged.fbx
License: CC0 1.0; exact provider text retained in LICENSE.txt.
Manifest: https://grab3d.com/dl/samurai-rigged/manifest.json (snapshot retained).
Retrieved: 2026-11-14.
FBX SHA-256: d46e395bf55418b6b49898b72d89e2996b39b70bd0eb00f7a893a89b86a23b3b

Origin: AI-assisted. Provider discloses Meshy image-to-3D, Blender cleanup, Meshy auto-rig and walk/run clips, and finger bones/hand pose added in Blender. This disclosure must remain available for distribution/platform declarations.

Integration changes: extracted 2048px texture maps; normal-map import; roughness inverted into Unity Standard smoothness alpha and combined with metallic red channel; generic rig; normalized to 1.9m including helmet crest; CC0 walk clip sampled on the local combat clock; procedural two-bone sword/foot posing and finger curl. Original geometry and source FBX retained. Model has 295,015 triangles / 179,000 imported vertices. The katana in the hands is our existing generated weapon.

Resources/Characters/Arima.prefab is included: cloning needs no asset-site account or runtime network. Editor menu Ganryujima/Prepare samurai character rebuilds it from these files. Do not copy generated prefabs between projects without their matching .meta files.

Milestone 7 variants reuse this same mesh and texture set: Yoshioka dyes saturated crimson lacquer charcoal/indigo; Kojiro dyes it ivory, increases model scale by 5.5%, and lengthens our generated sword by 38%. Original files remain unchanged. These are fictional character representations, not independent downloaded models or verified historical likenesses.
