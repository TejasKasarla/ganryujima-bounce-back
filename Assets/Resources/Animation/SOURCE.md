# Sword animation source

Quaternius — Universal Animation Library, CC0 1.0 (license retained beside this file).
Creator: https://quaternius.com/packs/universalanimationlibrary.html
Archive: https://github.com/tilakpatell/tilakverse-assets/tree/main/quaternius/universal-animation-library
Downloaded FBX: https://raw.githubusercontent.com/tilakpatell/tilakverse-assets/main/quaternius/universal-animation-library/Unity/UAL1.fbx
Original SHA256: 0556D52F6BCE01C0982B3548EE3CDFA1B8270977507001F62CBDFCC405570842
Retrieved 2026-11-14. Source credit: Quaternius. No AI-origin claim is made for these authored animations.

SwordMotions.json contains resampled hand translation, hand orientation and chest orientation at 30 Hz, relative to Sword_Idle. Included tracks: Sword_Idle, Sword_Attack_Standing, Sword_Attack, Hit_Chest, Dodge_Left, Dodge_Right, Death01. Active gameplay uses idle, both attacks and Hit_Chest; dodge/death tracks are retained for further adaptation. Source full FBX is cached in work/animation-source, not required at runtime.

This is a motion adaptation layered over the existing samurai rig's poses and arm IK, not complete humanoid retargeting. The player uses the same motion tracks as first-person weapon offsets. Translation and rotation are attenuated for the katana grip, camera framing and zone tells. Authoritative health, movement, collisions, contact timing, pause and hit-stop remain on gameplay clocks. Existing walk and finishing-blow poses remain intact.

Reproduction: import UAL1.fbx as Generic in an isolated Unity project at Assets/AnimationReview/UAL1.fbx, put Documentation/Animation/AnimationBake.cs.txt into its Editor folder with .cs extension, and run Ganryujima.Editor.AnimationBake.Bake. Copy the resulting Resources/Animation/SwordMotions.json. Source mannequin mesh is not shipped. User's TikTok upload was used as a visual reference for broad wind-ups, follow-through and first-person weapon framing; no content was extracted into game assets.
