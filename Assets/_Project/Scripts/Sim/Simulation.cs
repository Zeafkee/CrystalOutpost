using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CrystalOutpost.Sim
{
    public sealed class Simulation : IDisposable
    {
        public const int MaxTargets = 64;

        public NavGrid Grid;
        public HordeData Horde;
        public EnemyProjectileData EnemyProjectiles;
        public ShotData Shots;
        public PickupData Pickups;
        public SimConfig Config;

        private NativeArray<EnemyTypeData> _types;
        private NativeArray<TargetData> _targets;
        private NativeArray<float> _damageToTarget;
        private int _targetCount;

        private NativeArray<int> _cellStart;
        private NativeArray<int> _cellCount;
        private NativeArray<int> _sorted;
        private NativeArray<int> _cellOfEnemy;

        private NativeList<EnemyShotRequest> _shotRequests;
        private NativeList<DeathEvent> _deaths;
        private NativeList<ImpactEvent> _impacts;
        private NativeReference<int> _enemyProjectileCount;
        private NativeReference<int> _shotCount;
        private NativeReference<int> _pickupCount;
        private NativeReference<int> _collected;

        private Unity.Mathematics.Random _random;
        private float2 _corePosition;
        private float _coreRadius;

        public int KillCount { get; private set; }
        public int CollectedLastTick { get; private set; }

        public NativeArray<EnemyTypeData> Types => _types;
        public NativeArray<TargetData> Targets => _targets;
        public int TargetCount => _targetCount;
        public NativeList<DeathEvent> Deaths => _deaths;
        public NativeList<ImpactEvent> Impacts => _impacts;

        public Simulation(int width, int height, float cellSize, float2 origin, EnemyTypeData[] types,
            int enemyCapacity, SimConfig config, uint seed = 1)
        {
            Config = config;
            Grid = new NavGrid(width, height, cellSize, origin, Allocator.Persistent);
            Horde = new HordeData(enemyCapacity, Allocator.Persistent);
            EnemyProjectiles = new EnemyProjectileData(256, Allocator.Persistent);
            Shots = new ShotData(256, Allocator.Persistent);
            Pickups = new PickupData(1024, Allocator.Persistent);

            _types = new NativeArray<EnemyTypeData>(types, Allocator.Persistent);
            _targets = new NativeArray<TargetData>(MaxTargets, Allocator.Persistent);
            _damageToTarget = new NativeArray<float>(MaxTargets, Allocator.Persistent);

            _cellStart = new NativeArray<int>(Grid.CellCount, Allocator.Persistent);
            _cellCount = new NativeArray<int>(Grid.CellCount, Allocator.Persistent);
            _sorted = new NativeArray<int>(enemyCapacity, Allocator.Persistent);
            _cellOfEnemy = new NativeArray<int>(enemyCapacity, Allocator.Persistent);

            _shotRequests = new NativeList<EnemyShotRequest>(128, Allocator.Persistent);
            _deaths = new NativeList<DeathEvent>(128, Allocator.Persistent);
            _impacts = new NativeList<ImpactEvent>(64, Allocator.Persistent);
            _enemyProjectileCount = new NativeReference<int>(Allocator.Persistent);
            _shotCount = new NativeReference<int>(Allocator.Persistent);
            _pickupCount = new NativeReference<int>(Allocator.Persistent);
            _collected = new NativeReference<int>(Allocator.Persistent);

            _random = new Unity.Mathematics.Random(math.max(seed, 1u));
        }

        public void SetCore(float2 position, float radius)
        {
            _corePosition = position;
            _coreRadius = radius;
            RebuildFlowField();
        }

        public void RebuildFlowField()
        {
            new FlowFieldJob
            {
                Width = Grid.Width,
                Height = Grid.Height,
                CellSize = Grid.CellSize,
                Origin = Grid.Origin,
                Goal = _corePosition,
                GoalRadius = _coreRadius,
                Cost = Grid.Cost,
                Distance = Grid.Distance,
                Flow = Grid.Flow,
            }.Run();
        }

        public int AddTarget(float2 position, float radius, TargetKind kind)
        {
            int slot = -1;
            for (int i = 0; i < _targetCount && slot < 0; i++)
                if (!_targets[i].Alive && _targets[i].Kind == TargetKind.Structure && kind == TargetKind.Structure) slot = i;

            if (slot < 0)
            {
                if (_targetCount >= MaxTargets) throw new InvalidOperationException("Too many targets.");
                slot = _targetCount++;
            }

            _targets[slot] = new TargetData { Position = position, Radius = radius, Kind = kind, Alive = true };
            return slot;
        }

        public void MoveTarget(int handle, float2 position)
        {
            TargetData target = _targets[handle];
            target.Position = position;
            _targets[handle] = target;
        }

        public void SetTargetAlive(int handle, bool alive)
        {
            TargetData target = _targets[handle];
            target.Alive = alive;
            _targets[handle] = target;
        }

        public float GetDamageTo(int handle) => _damageToTarget[handle];

        public bool SpawnEnemy(int typeId, float2 position)
        {
            EnemyTypeData type = _types[typeId];
            return Horde.Spawn((byte)typeId, position, type.MaxHealth, _random.NextFloat(0f, 10f)) >= 0;
        }

        public void FireShot(float2 position, float2 direction, float speed, float damage, float splashRadius, float range, byte kind)
        {
            Shots.Spawn(position, direction * speed, damage, splashRadius, range / math.max(speed, 0.01f), kind);
        }

        public void SpawnPickup(float2 position, int value)
        {
            float2 direction = _random.NextFloat2Direction();
            Pickups.Spawn(position, direction * _random.NextFloat(1.5f, 4.5f), value);
        }

        public int DamageEnemiesInRadius(float2 center, float radius, float damage)
        {
            int hits = 0;
            int reach = (int)math.ceil((radius + 1f) / Grid.CellSize);
            int2 cell = Grid.CellOf(center);

            for (int y = math.max(cell.y - reach, 0); y <= math.min(cell.y + reach, Grid.Height - 1); y++)
            {
                for (int x = math.max(cell.x - reach, 0); x <= math.min(cell.x + reach, Grid.Width - 1); x++)
                {
                    int c = y * Grid.Width + x;
                    int end = _cellStart[c] + _cellCount[c];
                    for (int s = _cellStart[c]; s < end; s++)
                    {
                        int j = _sorted[s];
                        if (Horde.Health[j] <= 0f) continue;

                        float r = radius + _types[Horde.TypeId[j]].Radius;
                        if (math.distancesq(center, Horde.Position[j]) > r * r) continue;

                        Horde.Health[j] -= damage;
                        Horde.Flash[j] = 1f;
                        hits++;
                    }
                }
            }

            return hits;
        }

        public bool TryFindNearestEnemy(float2 center, float range, out float2 position, out float2 velocity)
        {
            position = velocity = float2.zero;
            float best = range * range;
            int found = -1;

            int reach = (int)math.ceil(range / Grid.CellSize);
            int2 cell = Grid.CellOf(center);

            for (int y = math.max(cell.y - reach, 0); y <= math.min(cell.y + reach, Grid.Height - 1); y++)
            {
                for (int x = math.max(cell.x - reach, 0); x <= math.min(cell.x + reach, Grid.Width - 1); x++)
                {
                    int c = y * Grid.Width + x;
                    int end = _cellStart[c] + _cellCount[c];
                    for (int s = _cellStart[c]; s < end; s++)
                    {
                        int j = _sorted[s];
                        if (Horde.Health[j] <= 0f) continue;

                        float d = math.distancesq(center, Horde.Position[j]);
                        if (d >= best) continue;
                        best = d;
                        found = j;
                    }
                }
            }

            if (found < 0) return false;
            position = Horde.Position[found];
            velocity = Horde.Velocity[found];
            return true;
        }

        public void Tick(float deltaTime)
        {
            _deaths.Clear();
            _impacts.Clear();
            _shotRequests.Clear();
            _collected.Value = 0;
            for (int i = 0; i < _targetCount; i++) _damageToTarget[i] = 0f;

            RemoveDeadEnemies();
            BuildSpatialGrid().Complete();

            new HordeMoveJob
            {
                DeltaTime = deltaTime,
                Config = Config,
                Width = Grid.Width,
                Height = Grid.Height,
                CellSize = Grid.CellSize,
                Origin = Grid.Origin,
                CorePosition = _corePosition,
                Cost = Grid.Cost,
                Flow = Grid.Flow,
                CellStart = _cellStart,
                CellCount = _cellCount,
                Sorted = _sorted,
                Types = _types,
                Targets = _targets,
                TargetCount = _targetCount,
                Position = Horde.Position,
                Velocity = Horde.Velocity,
                TypeId = Horde.TypeId,
                NextPosition = Horde.NextPosition,
                NextVelocity = Horde.NextVelocity,
                TargetInRange = Horde.TargetInRange,
                Facing = Horde.Facing,
                WalkPhase = Horde.WalkPhase,
            }.Schedule(Horde.Count, 32).Complete();
            Horde.SwapMoveBuffers();

            JobHandle handle = new HordeAttackJob
            {
                DeltaTime = deltaTime,
                Count = Horde.Count,
                Types = _types,
                Targets = _targets,
                TypeId = Horde.TypeId,
                TargetInRange = Horde.TargetInRange,
                Position = Horde.Position,
                AttackTimer = Horde.AttackTimer,
                AttackAnim = Horde.AttackAnim,
                Flash = Horde.Flash,
                DamageToTarget = _damageToTarget,
                Shots = _shotRequests,
            }.Schedule();

            _enemyProjectileCount.Value = EnemyProjectiles.Count;
            handle = new EnemyProjectileJob
            {
                DeltaTime = deltaTime,
                TargetCount = _targetCount,
                Targets = _targets,
                Position = EnemyProjectiles.Position,
                Velocity = EnemyProjectiles.Velocity,
                Damage = EnemyProjectiles.Damage,
                Life = EnemyProjectiles.Life,
                DamageToTarget = _damageToTarget,
                Count = _enemyProjectileCount,
            }.Schedule(handle);

            _shotCount.Value = Shots.Count;
            handle = new ShotJob
            {
                DeltaTime = deltaTime,
                Width = Grid.Width,
                Height = Grid.Height,
                CellSize = Grid.CellSize,
                Origin = Grid.Origin,
                CellStart = _cellStart,
                CellCount = _cellCount,
                Sorted = _sorted,
                Types = _types,
                TypeId = Horde.TypeId,
                EnemyPosition = Horde.Position,
                EnemyHealth = Horde.Health,
                EnemyFlash = Horde.Flash,
                Position = Shots.Position,
                Velocity = Shots.Velocity,
                Damage = Shots.Damage,
                SplashRadius = Shots.SplashRadius,
                Life = Shots.Life,
                Kind = Shots.Kind,
                Count = _shotCount,
                Impacts = _impacts,
            }.Schedule(handle);

            TargetData hero = FindHero(out bool heroExists);
            _pickupCount.Value = Pickups.Count;
            handle = new PickupJob
            {
                DeltaTime = deltaTime,
                HeroPosition = hero.Position,
                HeroAlive = heroExists && hero.Alive,
                PickupRange = Config.PickupRange,
                PickupSpeed = Config.PickupSpeed,
                PickupAcceleration = Config.PickupAcceleration,
                BoundsMin = Grid.Min + 0.3f,
                BoundsMax = Grid.Max - 0.3f,
                Position = Pickups.Position,
                Velocity = Pickups.Velocity,
                Value = Pickups.Value,
                Age = Pickups.Age,
                Magnetised = Pickups.Magnetised,
                Count = _pickupCount,
                Collected = _collected,
            }.Schedule(handle);

            handle.Complete();

            EnemyProjectiles.Count = _enemyProjectileCount.Value;
            Shots.Count = _shotCount.Value;
            Pickups.Count = _pickupCount.Value;
            CollectedLastTick = _collected.Value;

            for (int i = 0; i < _shotRequests.Length; i++)
            {
                EnemyShotRequest request = _shotRequests[i];
                EnemyProjectiles.Spawn(request.Position, request.Direction * request.Speed, request.Damage, 4f);
            }

            RemoveDeadEnemies();
            BuildSpatialGrid().Complete();
        }

        private JobHandle BuildSpatialGrid()
        {
            return new BuildSpatialGridJob
            {
                Count = Horde.Count,
                Width = Grid.Width,
                Height = Grid.Height,
                CellSize = Grid.CellSize,
                Origin = Grid.Origin,
                Position = Horde.Position,
                CellStart = _cellStart,
                CellCount = _cellCount,
                Sorted = _sorted,
                CellOfEnemy = _cellOfEnemy,
            }.Schedule();
        }

        private void RemoveDeadEnemies()
        {
            for (int i = Horde.Count - 1; i >= 0; i--)
            {
                if (Horde.Health[i] > 0f) continue;

                byte typeId = Horde.TypeId[i];
                float2 position = Horde.Position[i];
                _deaths.Add(new DeathEvent { Position = position, TypeId = typeId });

                int drops = _types[typeId].CrystalDrop;
                for (int d = 0; d < drops; d++)
                    SpawnPickup(position, 1);

                Horde.RemoveAtSwapBack(i);
                KillCount++;
            }
        }

        private TargetData FindHero(out bool exists)
        {
            for (int i = 0; i < _targetCount; i++)
            {
                if (_targets[i].Kind != TargetKind.Hero) continue;
                exists = true;
                return _targets[i];
            }

            exists = false;
            return default;
        }

        public void Dispose()
        {
            Grid.Dispose();
            Horde.Dispose();
            EnemyProjectiles.Dispose();
            Shots.Dispose();
            Pickups.Dispose();
            _types.Dispose();
            _targets.Dispose();
            _damageToTarget.Dispose();
            _cellStart.Dispose();
            _cellCount.Dispose();
            _sorted.Dispose();
            _cellOfEnemy.Dispose();
            _shotRequests.Dispose();
            _deaths.Dispose();
            _impacts.Dispose();
            _enemyProjectileCount.Dispose();
            _shotCount.Dispose();
            _pickupCount.Dispose();
            _collected.Dispose();
        }
    }
}
