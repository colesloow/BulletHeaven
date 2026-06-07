# BulletHeaven

A Unity 3D bullet-heaven / roguelike developed as a school project with two overlapping goals.

## Context

This project started as a revisit of an older bullet-heaven prototype built during an introduction to Unity course. That original project was simple (basic movement, basic enemies, no real architecture).

The main academic objective was to implement **procedural generation**: a dungeon generator that produces connected rooms and corridors at runtime, with NavMesh baking, door socket matching, and per-room decoration rules driven by ScriptableObjects.

On top of that foundation, the project became an opportunity to go further and explore what makes this genre satisfying to play. That meant rethinking the feel of the game from the ground up: VFX, procedural animations, custom shaders, and a proper data-driven upgrade system.

## What was built

### Procedural dungeon generation
- Room and corridor placement with door socket matching (Normal / Wide)
- Runtime NavMesh baking from flat navmesh meshes embedded in prefabs
- Per-room decoration profiles (ScriptableObjects) with weighted prop placement rules
- Wall cutout system: stencil sphere follows the player, walls fade out when occluding the camera

### Data-driven architecture
- `WeaponData` and `WeaponUpgrade` ScriptableObjects -- all weapon stats and upgrade trees defined in assets, not in code
- `WaveConfig` ScriptableObjects for enemy wave parameters
- `DungeonRules` / `RoomDecorationProfile` ScriptableObjects for generation and decoration rules
- Object pooling (`PoolManager`) for enemies and collectables

### Weapons and upgrades
- Satellite weapon: orbiting spheres that deal contact damage
- Laser beam: fires from satellites, expands and retracts with a hit zone; multiple lasers synchronize their fire cycle
- In-game upgrade panel at each level-up: weighted random selection, rarity system (Common / Uncommon / Rare), scrap cost

### Game feel
- Procedural animations: player roll, enemy drone hover, spider leg IK, collectable float/spin
- Custom HLSL shaders: wall cutout (stencil), hologram dissolve on enemy spawn, wall ghost overlay, stencil mask
- VFX: pickup sparkle, laser tip impact, player death explosion, screw drop, hologram spawn
- HitEffect system: decoupled hit flash from weapon logic via `Health.OnDamaged` event

### Game loop
- Main menu -> game -> game over with score / kills / damage taken / level reached
- Pause (P key), restart, return to main menu
- XP / level-up, scrap currency, countdown timer

## How to run

1. Open the project in Unity **6000.3.10f1** (other versions untested).
2. Make sure both scenes are added to the Build Settings: `Assets/Scenes/MainMenu` and `Assets/Scenes/Game`.
3. Open the `MainMenu` scene and press **Play**.

Alternatively, open the `Game` scene directly and press **Play** to skip the main menu.

## Debug controls (Play mode only)

| Key | Action |
|-----|--------|
| `Tab` | +50 XP (triggers level-up) |
| `P` | Pause / resume |
| `1` - `5` | Set satellite count (1 to 5) |
| `L` | Unlock laser on satellites |
| `+` / `=` | Add one laser |
| `-` | Remove one laser |
