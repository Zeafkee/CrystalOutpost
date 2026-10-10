# CrystalOutpost

A portrait arcade defense game for mobile, made with Unity 6. Steer the hero with a thumb, hold the
energy core against hundreds of creatures pouring down the roads, collect the crystals they drop and
spend them by standing on build pads: turrets, crystal cannons, walls and upgrades.

![screenshots](Docs/screenshots.png)

**Play in the browser:** [zeafkee.github.io/play/crystaloutpost](https://zeafkee.github.io/play/crystaloutpost/) · **Android:** APK in [Releases](../../releases/latest)

## Features

- Hundreds of enemies at once: melee husks, ranged spitters, Behemoth elites and a final boss
- Floating joystick; the hero shoots automatically, but only while standing still
- Build pads: walk onto a pad and crystals drain into it — bolt turrets, crystal cannons (area damage),
  walls, hero damage and speed, core repair and fortify; destroyed buildings can be rebuilt
- Minable crystal veins between waves
- 10-wave campaign with a boss, then endless mode with ever-growing waves and endless upgrades
- Title screen, pause, sound and music switches, best wave saved on the device
- All models, sprites, sounds and music made for this project (Blender Python, Pillow, numpy)

## Architecture

The horde is a **data-oriented simulation with Jobs and Burst**; everything that exists only a few
times (hero, turrets, pads, UI) is a normal GameObject.

```
Assets/_Project/Scripts
├── Sim/       Enemies, shots, projectiles and pickups as NativeArrays + Burst jobs.
│              Flow field pathing, uniform spatial grid, no GameObjects, unit tested.
├── Runtime/   World (bridge to the simulation, waves, session), Actors (hero, core, turrets, pads),
│              View (instanced rendering, camera, particles), UI, Audio, Data (ScriptableObjects)
├── Editor/    Scene and asset generation, model importer, benchmark and auto-pilot soak tools
└── Tests/     EditMode tests for the simulation
```

- One **flow field** (Dijkstra from the core) steers every enemy: O(1) per enemy per frame.
- A **uniform grid** rebuilt with a counting sort answers every "who is near" query.
- Enemy types are **data rows** (ScriptableObjects copied into a native table); melee, ranged and the
  boss run the same code.
- Enemies are drawn with **GPU instancing** and animated **in the vertex shader** (limbs swing around
  pivots stored in the mesh): no Animator, no skinned meshes, no textures.

Read more:
- **[Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md)** — frame order, data layout, pathing, rendering, trade-offs
- **[Docs/PERFORMANCE.md](Docs/PERFORMANCE.md)** — what was measured, how, and what is still open

## Numbers

| | |
|---|---|
| Simulation tick with 800 enemies | ~0.25 ms (jobs included) |
| Draw calls for all simulated objects | ~6, independent of enemy count |
| Simulation allocations per frame | 0 bytes (tested) |
| Tests | 13 EditMode tests |

Measured in the editor on a desktop PC; see [PERFORMANCE.md](Docs/PERFORMANCE.md) for method and caveats.

## Running it

1. Open the project with **Unity 6000.6** (URP).
2. Open `Assets/_Project/Scenes/Main.unity`, set the Game view (or Simulator) to a portrait phone, press Play.
3. Drag anywhere to move (WASD / arrow keys also work). Stop to shoot. Stand on a pad to buy.

Useful menu items under **CrystalOutpost**:

| Menu | What it does |
|---|---|
| Setup > Build Everything | Regenerates materials, meshes, enemy types, waves and the scene from code |
| Dev > Attach Auto-Pilot (3x speed) | The game plays itself (Play Mode) |
| Dev > Attach Auto-Pilot (soak test, max speed) | Same, as fast as the machine allows; one log line per wave |
| Dev > Log Status | Wave, enemies, crystals, core health, pad prices |

Tests: `Window > General > Test Runner > EditMode`.

## Assets

Models are built with Python in Blender (`Tools/blender`) and imported from a small JSON format
(`.lpmesh`). UI sprites come from `Tools/generate_ui.py`, every sound and the music loop from
`Tools/generate_audio.py`. Nothing is taken from other games. Third-party: DOTween (Demigiant),
TextMesh Pro with Liberation Sans (SIL OFL).

## Tech

Unity 6000.6 · URP · C# Jobs + Burst + Collections + Mathematics · Input System · uGUI + TextMesh Pro ·
DOTween · Unity Test Framework · Blender (Python) for models
