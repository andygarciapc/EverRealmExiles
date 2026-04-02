# EverRealm: Exiles — Development Roadmap

## Context

EverRealm: Exiles is a medieval voxel extraction game built in Unity 6.3 LTS (URP). The project was initialized with the standard URP template — URP rendering pipeline configured (PC + Mobile variants), Unity Input System wired up, NavMesh AI package installed, and no game code written yet. This plan maps the full journey from blank canvas to a playable extraction loop, organized into discrete phases that can each be picked up in a fresh session.

**Goal:** Get a vertical slice of the core loop playable (enter → loot → fight → extract), then expand.

---

## Phase 1 — Project Foundation & Folder Architecture
**Status: DONE**

Set up the code/asset organization that all future systems will live inside. Do this once, correctly.

### Tasks
- Create the `Assets/Scripts/` folder hierarchy:
  ```
  Scripts/
  ├── World/          # Voxel terrain, chunks, generation
  ├── Player/         # Controller, camera, input handling
  ├── Combat/         # Weapons, damage, hit detection
  ├── AI/             # Enemy behavior, states, pathfinding
  ├── Items/          # Loot, pickups, inventory
  ├── Extraction/     # Extract points, run end flow
  ├── UI/             # HUD, menus, run summary
  ├── Data/           # ScriptableObject definitions
  └── Core/           # Bootstrap, game state, scene management
  ```
- Create matching `Assets/ScriptableObjects/` folder for data assets
- Create scene structure: `MainMenu`, `Game` (replaces SampleScene), `RunSummary`
- Rename `SampleScene` → `Game`
- Set up `GameBootstrap` MonoBehaviour + `GameState` enum (MainMenu, InRun, RunEnd)
- Configure Tags and Layers: `Player`, `Enemy`, `Terrain`, `Loot`, `Interactable`
- Configure Physics layer collision matrix (e.g., Player vs. Terrain, Enemy vs. Terrain)
- Wire up the new Input System action map with actions: `Move`, `Look`, `Jump`, `Attack`, `Dodge`, `Interact`, `Sprint`

### Key Files Created
- `Assets/Scripts/Core/GameBootstrap.cs`
- `Assets/Scripts/Core/GameState.cs`
- `Assets/Scripts/Core/RunManager.cs` (stub)

### Verification
- Project compiles with no errors
- Input actions load and trigger in Play mode via debug log

---

## Phase 2 — Voxel Terrain Generation
**Status: DONE**

Build the core voxel world: chunk-based terrain with greedy meshing, procedural height map, and basic block types.

### Architecture
- **Block data:** `BlockType` enum (Air, Stone, Dirt, Grass, Sand, Ore variants)
- **Chunk:** 16×64×16 pure C# class holding a `BlockType[16,64,16]` array — no MonoBehaviour
- **ChunkRenderer:** MonoBehaviour that owns a `MeshFilter` + `MeshCollider`, receives a `Chunk` and builds mesh
- **ChunkMesher:** Static utility — greedy meshing algorithm (merge coplanar same-type faces into quads)
- **WorldGenerator:** Uses `Mathf.PerlinNoise` for height + biome generation
- **WorldManager:** MonoBehaviour that owns a `Dictionary<Vector2Int, ChunkRenderer>`, manages chunk loading/unloading around player

### Generation Parameters (ScriptableObject: `WorldGenSettings`)
- World seed (int)
- Map size in chunks (e.g., 16×16 chunks = 256×256 blocks)
- Height range (min/max surface Y)
- Noise scale and octaves
- Ore distribution settings per ore type

### Key Files Created
- `Assets/Scripts/World/BlockType.cs`
- `Assets/Scripts/World/Chunk.cs`
- `Assets/Scripts/World/ChunkMesher.cs`
- `Assets/Scripts/World/ChunkRenderer.cs`
- `Assets/Scripts/World/WorldGenerator.cs`
- `Assets/Scripts/World/WorldManager.cs`
- `Assets/Scripts/Data/WorldGenSettings.cs`

### Setup Required in Unity Editor
1. Create `Assets/ScriptableObjects/WorldGenSettings.asset` via Assets → Create → EverRealm → World Gen Settings
2. Create a URP Lit material named `ChunkMaterial` (leave texture empty — debug atlas auto-generates at runtime)
3. Assign both to the `WorldManager` GameObject in the Game scene
4. Leave `Player Transform` empty until Phase 3 — world streams around origin

### Performance Notes
- Generate chunks on background threads; apply meshes on main thread
- Chunk streaming radius: load 5, unload beyond 7 (tunable in WorldGenSettings)

### Verification
- A chunk world generates and renders at runtime
- Player can stand on terrain (colliders work)
- Walking to edge loads new chunks without hitches
- Frame rate above 60 fps on a basic scene

---

## Phase 3 — Player Controller & Camera
**Status: TODO**

Responsive first-person or third-person controller with ground movement, jumping, sprinting, and a smooth camera.

### Architecture
- `PlayerController` MonoBehaviour — owns `CharacterController`, receives input
- `PlayerCamera` MonoBehaviour — follow camera with mouse look, adjustable sensitivity
- `PlayerStats` ScriptableObject — move speed, sprint multiplier, jump height, gravity scale
- Keep `PlayerController` as thin as possible; movement math lives in `PlayerMover` (pure C# helper)

### Tasks
- Implement `PlayerController` using `CharacterController` component:
  - Ground movement (WASD) with sprint
  - Jump with coyote time (0.15s)
  - Gravity accumulation
  - Slope handling
- Implement `PlayerCamera`:
  - Mouse look (X = yaw on player, Y = pitch on camera pivot)
  - Clamp vertical look (-80° to +80°)
  - Cursor lock/unlock
- Implement `PlayerStats` ScriptableObject
- Add player capsule placeholder mesh + basic material
- Wire Input System actions to controller

### Key Files
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/Player/PlayerCamera.cs`
- `Assets/Scripts/Player/PlayerMover.cs`
- `Assets/Scripts/Data/PlayerStats.cs`

### Verification
- Player moves and jumps correctly on voxel terrain
- Camera follows without jitter
- Sprint works, coyote time feels natural
- No physics tunneling through terrain at high speed

---

## Phase 4 — Basic Combat System
**Status: TODO**

Melee combat with one weapon type: light attack, heavy attack, and dodge roll. Hit detection, damage, and basic feedback.

### Architecture
- `WeaponDefinition` ScriptableObject — damage, range, swing duration, combo count, stamina cost
- `WeaponController` MonoBehaviour — owns current weapon, drives attack animations/hitboxes
- `HitboxVolume` MonoBehaviour — collider that activates during swing frames, calls `IDamageable.TakeDamage()`
- `IDamageable` interface — `TakeDamage(DamageInfo info)` where `DamageInfo` holds amount, source, knockback
- `PlayerCombat` MonoBehaviour — reads attack input, feeds into `WeaponController`, manages stamina
- Dodge roll as a movement burst on the `PlayerController` (iframe window ~0.3s)
- Hit feedback: screen shake (small), hit sound, hit VFX (particle flash)

### Tasks
- Define `IDamageable` interface
- Define `DamageInfo` struct
- Implement `WeaponDefinition` ScriptableObject
- Implement `HitboxVolume` with enable/disable from animation events
- Implement `WeaponController` (swing state machine: Idle → Windup → Active → Recovery)
- Implement `PlayerCombat` (light attack, heavy attack, stamina management)
- Implement dodge roll on `PlayerController` (directional burst + iframe flag)
- Create one sword weapon definition asset
- Basic hit VFX (particle system) and placeholder sound

### Key Files
- `Assets/Scripts/Combat/IDamageable.cs`
- `Assets/Scripts/Combat/DamageInfo.cs`
- `Assets/Scripts/Combat/WeaponController.cs`
- `Assets/Scripts/Combat/HitboxVolume.cs`
- `Assets/Scripts/Player/PlayerCombat.cs`
- `Assets/Scripts/Data/WeaponDefinition.cs`

### Verification
- Player can swing sword and damage test target (cube with health bar)
- Heavy attack does more damage with longer windup
- Dodge grants brief iframes (damage during roll does nothing)
- Stamina depletes on attack/dodge, regenerates over time
- Attacks feel punchy (timing, camera shake, sound)

---

## Phase 5 — Basic Enemy AI
**Status: TODO**

One enemy type: a melee grunt that patrols, detects the player, chases, and attacks.

### Architecture
- `EnemyDefinition` ScriptableObject — health, move speed, attack damage, detection radius, attack range
- `EnemyController` MonoBehaviour — state machine owner (Patrol → Chase → Attack → Stagger → Dead)
- `EnemyMover` — pure C# class wrapping NavMesh movement calls
- `EnemyAttack` MonoBehaviour — owns hitbox volume, drives attack timing
- `EnemyHealth` MonoBehaviour — implements `IDamageable`, drives hit reactions and death

### States
- **Patrol:** Wander to random nearby points on NavMesh
- **Chase:** Nav to player position when within detection radius
- **Attack:** Stop, play swing animation, activate hitbox during active frames
- **Stagger:** Brief interrupt on heavy hit (cancel current action)
- **Dead:** Ragdoll/disable, optionally drop loot

### Tasks
- Bake NavMesh on terrain (runtime NavMesh bake or pre-bake)
- Implement `EnemyDefinition` ScriptableObject
- Implement `EnemyController` state machine
- Implement `EnemyMover` (NavMesh agent wrapper)
- Implement `EnemyAttack` with hitbox volume
- Implement `EnemyHealth` implementing `IDamageable`
- Wire enemy to loot drop (stub: spawn a loot pickup on death)
- Placeholder enemy mesh (capsule + material color)
- One enemy definition asset (Grunt)

### Key Files
- `Assets/Scripts/AI/EnemyController.cs`
- `Assets/Scripts/AI/EnemyMover.cs`
- `Assets/Scripts/AI/EnemyAttack.cs`
- `Assets/Scripts/AI/EnemyHealth.cs`
- `Assets/Scripts/Data/EnemyDefinition.cs`

### Verification
- Enemy patrols on terrain
- Enemy detects and chases player when close
- Enemy attacks player, dealing correct damage
- Player can kill enemy (health depletes → death state)
- Dead enemy does not continue to act

---

## Phase 6 — Loot & Inventory System
**Status: TODO**

Items, loot pickups, and a lightweight inventory that persists through the run.

### Architecture
- `ItemDefinition` ScriptableObject — item ID, display name, icon, rarity (Common/Rare/Epic), stackable, weight/value
- `LootPickup` MonoBehaviour — world item; shows icon billboard, triggers pickup on interact input
- `Inventory` pure C# class — `List<ItemStack>` with add/remove/find operations
- `PlayerInventory` MonoBehaviour — owns `Inventory`, exposed via events for UI
- `LootTable` ScriptableObject — weighted list of `ItemDefinition` references; `Roll()` returns an item
- Loot containers (chests) — `LootContainer` MonoBehaviour with a `LootTable` reference, opens on interact

### Tasks
- Define `ItemRarity` enum
- Implement `ItemDefinition` ScriptableObject
- Implement `ItemStack` struct (item + quantity)
- Implement `Inventory` class
- Implement `PlayerInventory` MonoBehaviour
- Implement `LootPickup` MonoBehaviour (pickup interaction via `IInteractable` interface)
- Define `IInteractable` interface — `Interact(PlayerController player)`
- Implement `LootTable` ScriptableObject with weighted roll
- Implement `LootContainer` (chest) MonoBehaviour
- Wire player interact action (`E` key) to raycast → `IInteractable.Interact()`
- Create 3–5 item definitions: Sword, Healing Potion, Iron Ore, Gold Coin, Cloth

### Key Files
- `Assets/Scripts/Items/ItemDefinition.cs`
- `Assets/Scripts/Items/ItemStack.cs`
- `Assets/Scripts/Items/Inventory.cs`
- `Assets/Scripts/Items/PlayerInventory.cs`
- `Assets/Scripts/Items/LootPickup.cs`
- `Assets/Scripts/Items/LootContainer.cs`
- `Assets/Scripts/Items/IInteractable.cs`
- `Assets/Scripts/Data/LootTable.cs`

### Verification
- Player can walk over loot pickups and collect them
- Player can open a chest and receive rolled loot
- Inventory correctly tracks item counts
- Enemy death drops loot using a loot table

---

## Phase 7 — Extraction System & Run Flow
**Status: TODO**

Extraction points, run timer, run-end summary, and the loop between runs.

### Architecture
- `ExtractionPoint` MonoBehaviour — trigger zone, requires player to stand inside for X seconds to extract
- `RunManager` MonoBehaviour — tracks run state (Active, Extracting, Ended), holds run start time
- `RunResult` class — carries extraction success flag, items carried out, time elapsed, kills
- `RunSummary` scene (or overlay) — displays result, items kept vs. lost, stat breakdown
- On failed run (death): `RunResult` with `success = false` — items lost (or partial if stash mechanic added later)

### Tasks
- Implement `ExtractionPoint` with hold-to-extract progress bar
- Implement `RunManager` tracking active state and time
- Implement `RunResult` data class
- Implement player death: trigger `RunManager.EndRun(success: false)`
- Implement extraction: trigger `RunManager.EndRun(success: true)` with inventory snapshot
- Implement basic Run Summary UI (canvas overlay): show success/fail, list items, show time + kills
- Wire scene transitions: `Game` → `RunSummary` → `Game` (new run)
- Stub persistent progression (saved items bank) — just log for now, full persistence in Phase 9

### Key Files
- `Assets/Scripts/Extraction/ExtractionPoint.cs`
- `Assets/Scripts/Core/RunManager.cs`
- `Assets/Scripts/Core/RunResult.cs`
- `Assets/Scripts/UI/RunSummaryUI.cs`

### Verification
- Player stands on extraction point → progress fills → run ends as success
- Player dying ends run as failure
- Run Summary screen shows correct data
- New run can be started from the summary screen

---

## Phase 8 — HUD & Basic UI
**Status: TODO**

In-run heads-up display covering health, stamina, inventory count, and extraction status.

### Tasks
- Player health bar (top-left or center-top)
- Stamina bar (near health)
- Carry weight / item count indicator
- Extraction point interaction prompt (world-space or screen-space)
- Hold-to-extract radial progress indicator
- Death screen with "You Died" + cause of death
- Minimap stub (optional for this phase — can be Phase 10)
- Basic main menu scene: Play button → loads Game scene

### Key Files
- `Assets/Scripts/UI/HUDController.cs`
- `Assets/Scripts/UI/HealthBarUI.cs`
- `Assets/Scripts/UI/StaminaBarUI.cs`
- `Assets/Scripts/UI/InteractionPromptUI.cs`

### Verification
- Health bar updates in real time when taking damage
- Stamina bar drains/fills correctly
- Extraction prompt appears when near extraction point
- Death screen appears on player death with restart option

---

## Phase 9 — Persistence & Progression
**Status: TODO**

Carry extracted loot between runs. Basic persistent stash and upgrade hooks.

### Architecture
- `SaveData` class (JSON-serializable) — persistent stash, run count, stats
- `SaveManager` static class or singleton — `Save()`, `Load()`, using `Application.persistentDataPath`
- Stash Screen (between runs): display saved items, allow equipping gear for next run
- Basic loadout selection: choose starting weapon from unlocked options

### Tasks
- Implement `SaveData` serialization (Newtonsoft.Json or `JsonUtility`)
- Implement `SaveManager`
- Implement stash inventory (separate from run inventory)
- Transfer extracted items to stash on successful run
- Stash UI screen between runs
- Basic loadout selection (weapon choice)
- Persist run statistics (total runs, total extractions, total kills)

### Key Files
- `Assets/Scripts/Core/SaveData.cs`
- `Assets/Scripts/Core/SaveManager.cs`
- `Assets/Scripts/UI/StashUI.cs`
- `Assets/Scripts/UI/LoadoutUI.cs`

### Verification
- Extracted items persist after closing and reopening the game
- Stash displays correctly between runs
- Player can choose starting weapon and it appears in-run

---

## Phase 10 — Map Generation & POI Placement
**Status: TODO**

Upgrade procedural generation to include meaningful Points of Interest: enemy camps, hidden caches, dungeon entrances, and extraction zones.

### Tasks
- `MapLayout` class — divides map into regions (safe, medium, high-risk) using zone masks
- POI placement algorithm: scatter POIs with minimum distance constraints
- Prefab-based POIs: EnemyCamp, TreasureCache, DungeonEntrance (stub), ExtractionZone
- Enemy spawners placed at enemy camp POIs
- Loot containers placed at treasure cache POIs
- Multiple extraction points per map (player must choose route)
- World border/boundary: invisible walls or off-limits terrain marker

### Key Files
- `Assets/Scripts/World/MapLayout.cs`
- `Assets/Scripts/World/POIPlacement.cs`
- POI prefabs in `Assets/Prefabs/POI/`

### Verification
- Generated maps have enemy camps with enemies, treasure caches with loot, and 2–3 extraction points
- No two POIs spawn on top of each other
- Map feels varied across different seeds

---

## Phase 11 — Polish Pass & Vertical Slice
**Status: TODO**

Tie everything together into a shippable vertical slice: one full run that feels good.

### Tasks
- Placeholder art pass: block textures (hand-painted or free CC0 assets), medieval palette
- Sound design pass: footsteps on terrain, sword swings, enemy death, chest open, extraction success
- Combat feel polish: screen shake tuning, hitpause (0.05s freeze on hit), improved hit VFX
- Enemy variety: add 1–2 enemy variants (ranged attacker, heavy melee)
- Terrain variety: cave systems pass, surface detail (grass, rocks, trees as non-voxel props)
- Performance pass: profile chunk generation, AI updates, physics calls; migrate ChunkMesher to Unity Job System + NativeArrays
- Bug fix sweep: report and fix all found issues in the core loop

### Verification
- Full run from main menu → generate world → loot → fight → extract → summary → repeat
- No critical bugs in the core loop
- Frame rate stable at 60+ fps on mid-range hardware target
- Game feels fun to play for 10 minutes

---

## Deferred / Future Phases

These are intentionally out of scope for the vertical slice but tracked here for planning:

- **Multiplayer** — PvP encounters within runs (the multiplayer center package is already installed)
- **Dungeon Interiors** — Hand-crafted or procedural interior spaces as mid-run POIs
- **Crafting System** — Use harvested resources to craft consumables or upgrade gear
- **Perk/Upgrade Tree** — Spend extracted resources on permanent upgrades between runs
- **Full Art Pass** — Stylized voxel block textures, enemy models, animations
- **Full Audio** — Music, ambient, full SFX coverage
- **Terrain Destruction** — Player can mine blocks during runs (Minecraft-style)
- **Boss Encounters** — High-risk POI with boss enemy guarding high-value loot
- **Minimap / Map Reveal** — Fog of war minimap for navigation

---

## Cross-Cutting Notes

### Folder & Namespace Conventions
- All scripts use namespace `EverRealm.Exiles.<System>` (e.g., `EverRealm.Exiles.World`)
- ScriptableObject assets live in `Assets/ScriptableObjects/<Category>/`
- Prefabs live in `Assets/Prefabs/<Category>/`
- Each phase should ship its folder subtree clean before moving on

### Performance Checkpoints
- After Phase 2: profile chunk generation and meshing
- After Phase 5: profile NavMesh queries and AI update rate
- After Phase 10: full profile pass on a 16×16 chunk map with 20 enemies

### Prototyping Rule
- If mechanics feel wrong after 30 min of testing, stop and reassess before coding deeper
- Mark any prototype-quality code with `// PROTOTYPE:` comment so it's easy to find and replace
