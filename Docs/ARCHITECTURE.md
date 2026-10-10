# Architecture

CrystalOutpost is a portrait arcade defense game: a hero you steer with a joystick, hundreds of enemies
walking dirt roads towards an energy core, and build pads that turn collected crystals into turrets,
walls and upgrades. The interesting technical problem is the horde, so the code is split by how many
of a thing exist:

| How many | What | How it is made |
|---|---|---|
| hundreds | enemies, player shots, enemy projectiles, crystal pickups | plain data in `NativeArray`s, updated by Burst jobs, drawn with GPU instancing |
| a few dozen | turrets, walls, build pads, scenery | ordinary GameObjects with small components |
| one | hero, core, camera, HUD | ordinary GameObjects |

This is a **data-oriented horde simulation with Jobs and Burst**. It does not use the Entities (ECS)
package: the horde is the only part that needs it, and keeping everything else as normal Unity objects
keeps the project small and easy to read.

```
Assets/_Project/Scripts
├── Sim/       The horde. No GameObjects, no UnityEngine.Object references, runs in EditMode tests.
│              NavGrid + FlowFieldJob, HordeData + jobs, Projectiles, Pickups, Simulation (orchestrator)
├── Runtime/
│   ├── World/    GameWorld (bridge to the simulation), WaveSpawner, GameSession, NavObstacle
│   ├── Actors/   Hero, CoreCrystal, Structure, Turret, BuildPad, CrystalVein
│   ├── View/     HordeRenderer, InstanceBatcher, CameraRig, WorldFx
│   ├── UI/       Hud, Joystick, MenuPanel, PausePanel, ResultPanel, SafeArea
│   ├── Audio/    AudioManager, AudioDirector, GameSettings
│   ├── Data/     EnemyTypeAsset, WaveAsset, MeshLibrary (ScriptableObjects)
│   └── Dev/      DebugAutoPilot (editor and development builds only)
├── Editor/    Scene and asset generation, .lpmesh importer, benchmark and soak-test tools
└── Tests/     EditMode tests for the simulation
```

## The frame

```
GameWorld.Update
  └─ Simulation.Tick(dt)
       remove dead enemies (spawn crystals, record death events)
       spatial grid            BuildSpatialGridJob   (counting sort of enemies into cells)
       movement                HordeMoveJob          (IJobParallelFor, double-buffered)
       attacks                 HordeAttackJob        (melee hits, ranged shot requests)
       enemy projectiles       EnemyProjectileJob
       player shots            ShotJob               (hits, splash damage)
       crystal pickups         PickupJob             (scatter, magnet, collect)
       remove dead, rebuild spatial grid (so turret queries this frame are exact)
  └─ route results: damage to the hero / core / structures, crystals to the wallet, events to effects
Hero, Turret, BuildPad, ...  Update     (query the simulation, fire shots, spend crystals)
HordeRenderer.LateUpdate               (one Burst job builds instance data, then instanced draws)
```

`Simulation` is the only class that knows this order. Everything else talks to it through a small API:
`SpawnEnemy`, `FireShot`, `SpawnPickup`, `DamageEnemiesInRadius`, `TryFindNearestEnemy`, `AddTarget`,
`MoveTarget`, and reads its outputs (`Deaths`, `Impacts`, `CollectedLastTick`, damage per target).

## Enemies as data

`HordeData` is a struct of arrays: `Position`, `Velocity`, `Health`, `TypeId`, `AttackTimer`, `Facing`,
`WalkPhase`, `Flash`, `AttackAnim`, ... one `NativeArray` each. A job that only moves enemies touches
only the arrays it needs, which is what makes the loops cache-friendly and Burst-vectorizable.

- **Spawning** appends at `Count`. **Removing** is swap-back: the last enemy is copied into the hole.
  Both are O(1) and the arrays never move or grow (capacity is fixed at startup).
- **Movement** reads `Position`/`Velocity` and writes `NextPosition`/`NextVelocity`, then the buffers
  are swapped. Every enemy is processed in parallel against the same snapshot, and no two threads
  write the same element.

### Enemy types are table rows, not classes

Melee and ranged enemies run the same code. The difference is data: `EnemyTypeData` holds health,
speed, radius, attack range, damage, interval and `ProjectileSpeed` (0 = melee). Types are authored as
`EnemyTypeAsset` ScriptableObjects and copied into a `NativeArray<EnemyTypeData>` at startup; each
enemy stores a one-byte `TypeId`. The wave-10 boss ("Overlord") is a new asset with bigger numbers and
the same mesh as the Behemoth: no new code.

## Pathing: one flow field for everyone

Every enemy goes to the same place (the core), so instead of one path per enemy the game computes one
**flow field**: a Dijkstra search from the core over the map grid (binary heap, 8 neighbours, no corner
cutting) that stores, for each cell, the direction to walk. An enemy then just looks up its cell:
O(1) per enemy per frame, however many enemies there are.

Cells have costs: roads are cheap (1), open ground is expensive (3), rocks and buildings are blocked.
So enemies prefer roads but cut across ground when that is shorter. When the player builds or loses a
structure, its cells change and the field is rebuilt once, in a Burst job, on the next frame.

## Neighbours: a uniform spatial grid

Separation (enemies pushing each other apart), shot hits, splash damage and "nearest enemy" queries
all need "who is near this point". `BuildSpatialGridJob` sorts enemy indices by cell with a counting
sort (count per cell, prefix sum, scatter). A query then only looks at the 3×3 (or r×r) cells around
the point instead of every enemy. Rebuilding is O(n) per frame.

## Combat

- **Targets.** The core, the hero and every structure register as `TargetData` (position, radius,
  kind). Enemies pick the nearest live target in reach; the hero pulls nearby enemies off the road
  (aggro radius). Damage dealt during a tick is summed per target and handed back to the owning
  GameObject (`IDamageable`) once per frame.
- **Shots** are data too (`ShotData`): position, velocity, damage, splash radius, lifetime. A shot is
  swept in sub-steps of 0.4 units, so a fast shot on a long frame cannot jump over a small enemy
  (there is a test for this).
- **The hero** only fires while standing still. Moving and shooting are a trade-off, which is the main
  decision the player makes second to second.

## Rendering

- **Instancing.** `InstanceBatcher` draws one mesh with `Graphics.RenderMeshInstanced`, up to 1023
  instances per call, each with a matrix and one `Vector4` of per-instance data. `HordeRenderer` fills
  these arrays from the simulation in a Burst job. With 800 enemies, shots and crystals on screen it is
  about 6 draw calls for all simulated objects.
- **Animation without an Animator.** The `CrystalOutpost/Toon` shader animates characters in the
  vertex shader. Each vertex knows which limb it belongs to (`uv0`: body / leg / arm / held weapon,
  side) and the limb's pivot (`uv1`); the per-instance `Vector4` carries walk phase, hit flash and
  attack. Legs and arms swing around their pivots, the weapon recoils. This is procedural animation,
  not baked vertex animation textures: simpler to author and enough for 180-triangle enemies.
- **No textures, no shadow maps, no post-processing.** Colour lives in vertex colours (alpha = emission),
  lighting is a fixed half-Lambert from global shader values, shadows and glows are blob quads. The dark
  look comes from the palette, emissive crystals and a vignette image.
- **Models** are made in Blender with Python (`Tools/blender`) and exported as `.lpmesh` (JSON already in
  Unity's coordinate system). A `ScriptedImporter` turns them into meshes with the vertex layout above.

## Game layer

- **`GameWorld`** creates the simulation from the scene (map size, roads, obstacles, enemy types),
  ticks it, and is the wallet (`AddCrystals`, `TrySpend`).
- **`Structure`** = health + simulation target + blocked cells. Building is activating a pre-placed
  GameObject; destroyed is deactivating it. Nothing is instantiated during play.
- **`BuildPad`** is the shop. While the hero stands on it (after a short dwell, so walking across is
  free), crystals drain into the price; when it is paid the pad builds, upgrades, repairs or fortifies.
  Prices can grow without end (`endlessGrowth`), which is the crystal sink of endless mode.
- **`WaveSpawner`** plays `WaveAsset`s (groups of enemy types, spawn duration, preparation time); after
  the last wave, endless mode repeats it with 12 % more enemies each time.
- **`GameSession`** is the state machine: Menu, Playing, Paused, Won, Lost. Stopping the game freezes the
  world (`GameWorld.Running`) instead of setting `Time.timeScale`, so UI animations keep running.
- **Events, not references.** Hero, turrets, pads and the session raise events; `AudioDirector` and
  `WorldFx` listen. Gameplay code does not know sound or particles exist.

## Tooling

- **`CrystalOutpost > Setup > Build Everything`** regenerates materials, the mesh library, enemy types,
  waves and the whole scene from code (`SceneBuilder`). The scene is never edited by hand.
- **`DebugAutoPilot`** plays the game (collect, fight, buy). With the soak mode in `DevTools` a full
  10-wave run takes well under a minute, logging one status line per wave. This is how balance changes
  and long-run bugs were checked.
- **`DevTools.StartBenchmark(n)`** keeps exactly n enemies alive and measures frame time, simulation
  time, draw calls and GC (see [PERFORMANCE.md](PERFORMANCE.md)).
- **Assets** are generated: models with Blender Python, UI sprites with `Tools/generate_ui.py`, every
  sound and the music loop with `Tools/generate_audio.py`.

## Decisions and trade-offs

| Decision | Why | Cost |
|---|---|---|
| Jobs + Burst + NativeArrays, not Entities | Only the horde needs it; the rest stays simple GameObjects | Hand-written data layout and job scheduling |
| One flow field | O(1) pathing per enemy, all enemies share it | Every enemy has the same goal (fine for this game) |
| Uniform grid, not a quadtree | Enemies are similar in size and spread over a fixed map; O(n) rebuild, no allocation | Cell size is tuned for the enemy radius |
| Procedural shader animation, not VAT | No baking pipeline, tiny meshes, per-instance phase | Rigid limbs only |
| Swap-back removal | O(1), arrays stay dense | Enemy order changes (nothing depends on it) |
| Scene generated by code | Reproducible, reviewable in diffs, no lost references | Layout changes are code changes |
| Reload the scene to restart | Guaranteed clean state, no reset code to forget | Short load on restart |
