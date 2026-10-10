# Performance

The goal: hundreds of enemies on screen at 60 FPS on a mid-range Android phone, flat memory during a
session, and no garbage collection while the player is fighting.

> **Where the numbers come from.** Everything below was measured in the Unity Editor (Windows, DX12,
> desktop CPU/GPU) with the tools in this repo. Editor numbers are good for counting draw calls,
> comparing loads and checking allocations; they are **not phone numbers**. Frame time on a device
> still has to be measured with a development build and the Profiler.

## Measured

Fixed-load benchmark: `DevTools.StartBenchmark(n)` keeps exactly n enemies alive (topping the horde up
at the spawn points every frame), with the hero and the turrets fighting them, frame cap removed,
2 s warm-up, then 4 s of samples.

| Enemies alive | Simulation tick (main thread, jobs included) | Instanced draw calls (all sim objects) | Avg frame (editor) |
|---|---|---|---|
| 100 | 0.13 ms | 3 | 3.4 ms |
| 300 | 0.19 ms | 7 | 3.7 ms |
| 500 | 0.21 ms | 6 | 3.8 ms |
| 800 | 0.25 ms | 6 | 3.8 ms |

| What | Result | How |
|---|---|---|
| Simulation allocations | **0 bytes** per tick in steady state (500 enemies, hero, shots, pickups) | `Tick_InSteadyState_DoesNotAllocateManagedMemory` (Test Framework `AllocatingGCMemory` constraint) |
| Simulation tests | 13 EditMode tests, ~1 s | Test Runner |
| Full run | 10 waves + endless waves 11-15, ~2,500 kills, no errors | `DevTools` soak mode with the auto-pilot |

Reading the table: the simulation cost grows slowly with the horde (Burst + parallel movement), and
the draw call count does not grow at all (instancing). The editor frame time is dominated by the
editor itself, the HUD and the GameObject side, not by the horde. On a phone the same structure should
hold; the absolute numbers will be several times higher and have to be measured there.

How to reproduce: enter Play Mode, then run `CrystalOutpost.EditorTools.DevTools.StartBenchmark(500)`
(for example from a small editor script) and read `DevTools.BenchmarkReport()` after
about 6 seconds.

## What was done, and why

### CPU
- **Data-oriented horde.** Enemies, shots, enemy projectiles and pickups are structs of arrays in
  `NativeArray`s, processed by Burst-compiled jobs. Movement, the most expensive system, runs as an
  `IJobParallelFor` over all worker threads with double-buffered positions.
- **O(1) pathing.** One flow field to the core for all enemies, rebuilt only when a structure is built
  or destroyed. An enemy reads one cell per frame.
- **Spatial grid instead of distance checks against everyone.** Counting sort into cells once per
  frame; separation, hits, splash and turret targeting only look at nearby cells.
- **No physics.** No colliders or rigidbodies anywhere; physics simulation is switched off
  (`SimulationMode.Script`). Collisions are grid lookups.
- **Few GameObjects with `Update`.** Only the hero, turrets, pads, veins and UI; none per enemy.
- **Dirty-checked HUD.** Labels are only set when a value changed, using `TMP_Text.SetText` with
  numeric arguments (no string allocation).

### GPU
- **Instancing.** One `Graphics.RenderMeshInstanced` call per mesh type per 1023 instances. The instance
  data is built by a Burst job straight from the simulation arrays.
- **Animation in the vertex shader** (limb swing from per-instance phase), so no `SkinnedMeshRenderer`,
  no Animator, no bone matrices.
- **Cheap shading.** Vertex colours instead of textures, fixed half-Lambert lighting from global shader
  values, no real-time lights, no shadow maps (blob shadow quads instead), no post-processing, no HDR,
  no MSAA. The look is built from palette, emissive colours and one vignette image.
- **Low-poly models.** Enemies are 170–190 triangles (no bevels) because hundreds are drawn; the hero
  and the buildings, of which there are few, have more detail.
- **Effects:** two shared particle systems fed with `Emit()`; any number of deaths per frame is still
  two draw calls and no instantiation.

### Memory / GC
- **Fixed capacities.** Horde (1024), shots, projectiles and pickups are allocated once at startup and
  never grow; removal is swap-back.
- **Nothing instantiated during play.** Structures are pre-placed and toggled; restart reloads the scene.
- **Audio:** short effects are decompressed on load (no decode cost at play time), the music streams.
  A minimum interval per effect stops twenty simultaneous deaths from becoming twenty voices.
- **Settings:** incremental GC, IL2CPP + ARM64, engine code stripping, frame pacing on Android,
  `targetFrameRate = 60`.

## Platform notes

- **WebGL** has no worker threads and no Burst: the jobs run on the main thread as plain C#. The game
  works, but the enemy count it can hold is lower; the waves are sized so the campaign stays playable.
- **Thermals:** a horde game keeps the CPU busy for minutes. The 60 FPS cap (not uncapped) and the
  absence of post-processing are as much about heat and battery as about frame time.

## Still open

- **Measure on a device.** Development build + Profiler on a mid-range Android phone, 300 and 500
  enemies, then fill in a device column above.
- **GameObject-side allocations.** The simulation is proven allocation-free; the rest of the frame
  (DOTween punches on the HUD, particle emission) has not been profiled for per-frame GC yet.
- **Overdraw** from blob shadows and additive glows in dense fights is not measured.
- **Level of detail** for enemies far from the camera (fewer limbs animated, smaller meshes) is not
  needed at current counts and was not built.
