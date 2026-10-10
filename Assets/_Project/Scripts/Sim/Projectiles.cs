using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CrystalOutpost.Sim
{
    public struct EnemyProjectileData : IDisposable
    {
        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;
        public NativeArray<float> Damage;
        public NativeArray<float> Life;
        public int Count;
        public int Capacity => Position.Length;

        public EnemyProjectileData(int capacity, Allocator allocator)
        {
            Position = new NativeArray<float2>(capacity, allocator);
            Velocity = new NativeArray<float2>(capacity, allocator);
            Damage = new NativeArray<float>(capacity, allocator);
            Life = new NativeArray<float>(capacity, allocator);
            Count = 0;
        }

        public void Spawn(float2 position, float2 velocity, float damage, float life)
        {
            if (Count >= Capacity) return;
            int i = Count++;
            Position[i] = position;
            Velocity[i] = velocity;
            Damage[i] = damage;
            Life[i] = life;
        }

        public void RemoveAtSwapBack(int index)
        {
            int last = --Count;
            if (index == last) return;
            Position[index] = Position[last];
            Velocity[index] = Velocity[last];
            Damage[index] = Damage[last];
            Life[index] = Life[last];
        }

        public void Dispose()
        {
            Position.Dispose();
            Velocity.Dispose();
            Damage.Dispose();
            Life.Dispose();
        }
    }

    public struct ShotData : IDisposable
    {
        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;
        public NativeArray<float> Damage;
        public NativeArray<float> SplashRadius;
        public NativeArray<float> Life;
        public NativeArray<byte> Kind;
        public int Count;
        public int Capacity => Position.Length;

        public ShotData(int capacity, Allocator allocator)
        {
            Position = new NativeArray<float2>(capacity, allocator);
            Velocity = new NativeArray<float2>(capacity, allocator);
            Damage = new NativeArray<float>(capacity, allocator);
            SplashRadius = new NativeArray<float>(capacity, allocator);
            Life = new NativeArray<float>(capacity, allocator);
            Kind = new NativeArray<byte>(capacity, allocator);
            Count = 0;
        }

        public void Spawn(float2 position, float2 velocity, float damage, float splashRadius, float life, byte kind)
        {
            if (Count >= Capacity) return;
            int i = Count++;
            Position[i] = position;
            Velocity[i] = velocity;
            Damage[i] = damage;
            SplashRadius[i] = splashRadius;
            Life[i] = life;
            Kind[i] = kind;
        }

        public void RemoveAtSwapBack(int index)
        {
            int last = --Count;
            if (index == last) return;
            Position[index] = Position[last];
            Velocity[index] = Velocity[last];
            Damage[index] = Damage[last];
            SplashRadius[index] = SplashRadius[last];
            Life[index] = Life[last];
            Kind[index] = Kind[last];
        }

        public void Dispose()
        {
            Position.Dispose();
            Velocity.Dispose();
            Damage.Dispose();
            SplashRadius.Dispose();
            Life.Dispose();
            Kind.Dispose();
        }
    }

    [BurstCompile]
    public struct EnemyProjectileJob : IJob
    {
        public float DeltaTime;
        public int TargetCount;
        [ReadOnly] public NativeArray<TargetData> Targets;

        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;
        public NativeArray<float> Damage;
        public NativeArray<float> Life;
        public NativeArray<float> DamageToTarget;
        public NativeReference<int> Count;

        public void Execute()
        {
            int count = Count.Value;

            for (int i = count - 1; i >= 0; i--)
            {
                float2 p = Position[i] + Velocity[i] * DeltaTime;
                Position[i] = p;
                Life[i] -= DeltaTime;

                bool dead = Life[i] <= 0f;
                for (int t = 0; t < TargetCount && !dead; t++)
                {
                    TargetData target = Targets[t];
                    if (!target.Alive) continue;
                    float reach = target.Radius + 0.15f;
                    if (math.distancesq(p, target.Position) > reach * reach) continue;

                    DamageToTarget[t] += Damage[i];
                    dead = true;
                }

                if (!dead) continue;

                int last = --count;
                Position[i] = Position[last];
                Velocity[i] = Velocity[last];
                Damage[i] = Damage[last];
                Life[i] = Life[last];
            }

            Count.Value = count;
        }
    }

    [BurstCompile]
    public struct ShotJob : IJob
    {
        private const float MaxStep = 0.4f;

        public float DeltaTime;
        public int Width;
        public int Height;
        public float CellSize;
        public float2 Origin;

        [ReadOnly] public NativeArray<int> CellStart;
        [ReadOnly] public NativeArray<int> CellCount;
        [ReadOnly] public NativeArray<int> Sorted;
        [ReadOnly] public NativeArray<EnemyTypeData> Types;
        [ReadOnly] public NativeArray<byte> TypeId;
        [ReadOnly] public NativeArray<float2> EnemyPosition;
        public NativeArray<float> EnemyHealth;
        public NativeArray<float> EnemyFlash;

        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;
        public NativeArray<float> Damage;
        public NativeArray<float> SplashRadius;
        public NativeArray<float> Life;
        public NativeArray<byte> Kind;
        public NativeReference<int> Count;
        public NativeList<ImpactEvent> Impacts;

        public void Execute()
        {
            int count = Count.Value;

            for (int i = count - 1; i >= 0; i--)
            {
                float2 step = Velocity[i] * DeltaTime;
                int subSteps = math.max(1, (int)math.ceil(math.length(step) / MaxStep));
                step /= subSteps;

                float2 p = Position[i];
                bool hit = false;
                for (int s = 0; s < subSteps && !hit; s++)
                {
                    p += step;
                    hit = FindEnemy(p, 0.25f) >= 0;
                }

                Position[i] = p;
                Life[i] -= DeltaTime;

                bool expired = Life[i] <= 0f;

                if (hit || (expired && SplashRadius[i] > 0f))
                {
                    if (SplashRadius[i] > 0f)
                    {
                        DamageRadius(p, SplashRadius[i], Damage[i]);
                    }
                    else
                    {
                        int enemy = FindEnemy(p, 0.25f);
                        EnemyHealth[enemy] -= Damage[i];
                        EnemyFlash[enemy] = 1f;
                    }

                    Impacts.Add(new ImpactEvent { Position = p, Radius = SplashRadius[i] });
                }

                if (!hit && !expired) continue;

                int last = --count;
                Position[i] = Position[last];
                Velocity[i] = Velocity[last];
                Damage[i] = Damage[last];
                SplashRadius[i] = SplashRadius[last];
                Life[i] = Life[last];
                Kind[i] = Kind[last];
            }

            Count.Value = count;
        }

        private int FindEnemy(float2 p, float radius)
        {
            int cx = math.clamp((int)math.floor((p.x - Origin.x) / CellSize), 0, Width - 1);
            int cy = math.clamp((int)math.floor((p.y - Origin.y) / CellSize), 0, Height - 1);

            for (int y = math.max(cy - 1, 0); y <= math.min(cy + 1, Height - 1); y++)
            {
                for (int x = math.max(cx - 1, 0); x <= math.min(cx + 1, Width - 1); x++)
                {
                    int cell = y * Width + x;
                    int end = CellStart[cell] + CellCount[cell];
                    for (int s = CellStart[cell]; s < end; s++)
                    {
                        int j = Sorted[s];
                        if (EnemyHealth[j] <= 0f) continue;
                        float reach = radius + Types[TypeId[j]].Radius;
                        if (math.distancesq(p, EnemyPosition[j]) <= reach * reach) return j;
                    }
                }
            }

            return -1;
        }

        private void DamageRadius(float2 p, float radius, float damage)
        {
            int reachCells = (int)math.ceil(radius / CellSize);
            int cx = math.clamp((int)math.floor((p.x - Origin.x) / CellSize), 0, Width - 1);
            int cy = math.clamp((int)math.floor((p.y - Origin.y) / CellSize), 0, Height - 1);

            for (int y = math.max(cy - reachCells, 0); y <= math.min(cy + reachCells, Height - 1); y++)
            {
                for (int x = math.max(cx - reachCells, 0); x <= math.min(cx + reachCells, Width - 1); x++)
                {
                    int cell = y * Width + x;
                    int end = CellStart[cell] + CellCount[cell];
                    for (int s = CellStart[cell]; s < end; s++)
                    {
                        int j = Sorted[s];
                        float reach = radius + Types[TypeId[j]].Radius;
                        if (math.distancesq(p, EnemyPosition[j]) > reach * reach) continue;
                        EnemyHealth[j] -= damage;
                        EnemyFlash[j] = 1f;
                    }
                }
            }
        }
    }
}
