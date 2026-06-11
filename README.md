# Still Running

A Unity 3D roguelike developed as a school project.

## Context

This project started as a revisit of an older bullet-heaven prototype built during an introduction to Unity course. That original project was simple (basic movement, basic enemies, no real architecture).

The main academic objective was to implement **procedural generation**: a dungeon generator that produces connected rooms and corridors at runtime, with NavMesh baking, door socket matching, and per-room decoration rules driven by ScriptableObjects.

On top of that foundation, the project became an opportunity to go further and explore what makes this genre satisfying to play. That meant rethinking the feel of the game from the ground up: VFX, procedural animations, custom shaders, and a proper data-driven upgrade system.

## How to run

1. Open the project in Unity, ideally version **6000.3.10f1** (other versions untested).
2. Make sure the `Game` scene is added to the Build Settings: `Assets/Scenes/Game`.
3. Open the `Game` scene and press **Play**.

## Debug controls (Play mode only)

| Key | Action |
|-----|--------|
| `Tab` | +50 XP (triggers level-up) |
| `P` | Pause / resume |
| `1` - `5` | Set satellite count (1 to 5) |
| `L` | Unlock laser on satellites |
| `+` / `=` | Add one laser |
| `-` | Remove one laser |
