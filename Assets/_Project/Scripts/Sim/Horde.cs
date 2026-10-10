using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CrystalOutpost.Sim
{
    public struct HordeData : IDisposable
    {
        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;
        public NativeArray<float> Health;
        public NativeArray<byte> TypeId;
        public NativeArray<float> AttackTimer;
        public NativeArray<int> TargetInRange;
        public NativeArray<float> Facing;
        public NativeArray<float> WalkPhase;
        public NativeArray<float> Flash;
        public NativeArray<float> AttackAnim;

        public NativeArray<float2> NextPosition;
        public NativeArray<float2> NextVelocity;

        public int Count;
        public int Capacity => Position.Length;

        public HordeData(int capacity, Allocator allocator)
        {
            Position = new NativeArray<float2>(capacity, allocator);
            Velocity = new NativeArray<float2>(capacity, allocator);
            Health = new NativeArray<float>(capacity, allocator);
            TypeId = new NativeArray<byte>(capacity, allocator);
            AttackTimer = new NativeArray<float>(capacity, allocator);
            TargetInRange = new NativeArray<int>(capacity, allocator);
            Facing = new NativeArray<float>(capacity, allocator);
            WalkPhase = new NativeArray<float>(capacity, allocator);
            Flash = new NativeArray<float>(capacity, allocator);
            AttackAnim = new NativeArray<float>(capacity, allocator);
            NextPosition = new NativeArray<float2>(capacity, allocator);
            NextVelocity = new NativeArray<float2>(capacity, allocator);
            Count = 0;
        }

        public int Spawn(byte typeId, float2 position, float health, float phase)
        {
            if (Count >= Capacity) return -1;

            int i = Count++;
            Position[i] = position;
            Velocity[i] = float2.zero;
            Health[i] = health;
            TypeId[i] = typeId;
            AttackTimer[i] = 0.4f;
            TargetInRange[i] = -1;
            Facing[i] = 0f;
            WalkPhase[i] = phase;
            Flash[i] = 0f;
            AttackAnim[i] = 0f;
            return i;
        }

        public void RemoveAtSwapBack(int index)
        {
            int last = --Count;
            if (index == last) return;

            Position[index] = Position[last];
            Velocity[index] = Velocity[last];
            Health[index] = Health[last];
            TypeId[index] = TypeId[last];
            AttackTimer[index] = AttackTimer[last];
            TargetInRange[index] = TargetInRange[last];
            Facing[index] = Facing[last];
            WalkPhase[index] = WalkPhase[last];
            Flash[index] = Flash[last];
            AttackAnim[index] = AttackAnim[last];
        }

        public void SwapMoveBuffers()
        {
            (Position, NextPosition) = (NextPosition, Position);
            (Velocity, NextVelocity) = (NextVelocity, Velocity);
        }

        public void Dispose()
        {
            Position.Dispose();
            Velocity.Dispose();
            Health.Dispose();
            TypeId.Dispose();
            AttackTimer.Dispose();
            TargetInRange.Dispose();
            Facing.Dispose();
            WalkPhase.Dispose();
            Flash.Dispose();
            AttackAnim.Dispose();
            NextPosition.Dispose();
            NextVelocity.Dispose();
        }
    }

    [BurstCompile]
    public struct BuildSpatialGridJob : IJob
    {
        public int Count;
        public int Width;
        public int Height;
        public float CellSize;
        public float2 Origin;

        [ReadOnly] public NativeArray<float2> Position;
        public NativeArray<int> CellStart;
        public NativeArray<int> CellCount;
        public NativeArray<int> Sorted;
        public NativeArray<int> CellOfEnemy;

        public void Execute()
        {
            int cells = Width * Height;
            for (int c = 0; c < cells; c++) CellCount[c] = 0;

            for (int i = 0; i < Count; i++)
            {
                float2 local = (Position[i] - Origin) / CellSize;
                int x = math.clamp((int)math.floor(local.x), 0, Width - 1);
                int y = math.clamp((int)math.floor(local.y), 0, Height - 1);
                int cell = y * Width + x;
                CellOfEnemy[i] = cell;
                CellCount[cell]++;
            }

            int running = 0;
            for (int c = 0; c < cells; c++)
            {
                CellStart[c] = running;
                running += CellCount[c];
                CellCount[c] = 0;
            }

            for (int i = 0; i < Count; i++)
            {
                int cell = CellOfEnemy[i];
                Sorted[CellStart[cell] + CellCount[cell]] = i;
                CellCount[cell]++;
            }
        }
    }

    [BurstCompile]
    public struct HordeMoveJob : IJobParallelFor
    {
        public float DeltaTime;
        public SimConfig Config;

        public int Width;
        public int Height;
        public float CellSize;
        public float2 Origin;
        public float2 CorePosition;

        [ReadOnly] public NativeArray<byte> Cost;
        [ReadOnly] public NativeArray<float2> Flow;
        [ReadOnly] public NativeArray<int> CellStart;
        [ReadOnly] public NativeArray<int> CellCount;
        [ReadOnly] public NativeArray<int> Sorted;
        [ReadOnly] public NativeArray<EnemyTypeData> Types;
        [ReadOnly] public NativeArray<TargetData> Targets;
        public int TargetCount;

        [ReadOnly] public NativeArray<float2> Position;
        [ReadOnly] public NativeArray<float2> Velocity;
        [ReadOnly] public NativeArray<byte> TypeId;

        [WriteOnly] public NativeArray<float2> NextPosition;
        [WriteOnly] public NativeArray<float2> NextVelocity;
        [WriteOnly] public NativeArray<int> TargetInRange;
        public NativeArray<float> Facing;
        public NativeArray<float> WalkPhase;

        public void Execute(int i)
        {
            float2 p = Position[i];
            EnemyTypeData type = Types[TypeId[i]];

            int nearest = -1;
            float nearestEdgeDistance = float.MaxValue;
            float2 nearestDirection = float2.zero;
            bool heroNearby = false;
            float2 toHero = float2.zero;

            for (int t = 0; t < TargetCount; t++)
            {
                TargetData target = Targets[t];
                if (!target.Alive) continue;

                float2 delta = target.Position - p;
                float distance = math.length(delta);
                float edge = distance - target.Radius - type.Radius;

                if (target.Kind == TargetKind.Hero && distance < Config.AggroRadius)
                {
                    heroNearby = true;
                    toHero = distance > 1e-4f ? delta / distance : float2.zero;
                }

                if (edge < nearestEdgeDistance)
                {
                    nearestEdgeDistance = edge;
                    nearest = t;
                    nearestDirection = distance > 1e-4f ? delta / distance : float2.zero;
                }
            }

            bool inReach = nearest >= 0 && nearestEdgeDistance <= type.AttackRange;
            TargetInRange[i] = inReach ? nearest : -1;

            float2 desired;
            if (inReach)
            {
                desired = float2.zero;
            }
            else if (heroNearby)
            {
                desired = toHero;
            }
            else
            {
                int cx = math.clamp((int)math.floor((p.x - Origin.x) / CellSize), 0, Width - 1);
                int cy = math.clamp((int)math.floor((p.y - Origin.y) / CellSize), 0, Height - 1);
                desired = Flow[cy * Width + cx];
                if (math.lengthsq(desired) < 1e-4f)
                    desired = math.normalizesafe(CorePosition - p);
            }

            float2 push = float2.zero;
            {
                int cx = math.clamp((int)math.floor((p.x - Origin.x) / CellSize), 0, Width - 1);
                int cy = math.clamp((int)math.floor((p.y - Origin.y) / CellSize), 0, Height - 1);
                int x0 = math.max(cx - 1, 0), x1 = math.min(cx + 1, Width - 1);
                int y0 = math.max(cy - 1, 0), y1 = math.min(cy + 1, Height - 1);

                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int cell = y * Width + x;
                        int start = CellStart[cell];
                        int end = start + CellCount[cell];
                        for (int s = start; s < end; s++)
                        {
                            int j = Sorted[s];
                            if (j == i) continue;

                            float2 away = p - Position[j];
                            float minDistance = type.Radius + Types[TypeId[j]].Radius;
                            float distanceSq = math.lengthsq(away);
                            if (distanceSq >= minDistance * minDistance || distanceSq < 1e-8f) continue;

                            float distance = math.sqrt(distanceSq);
                            push += away / distance * ((minDistance - distance) / minDistance);
                        }
                    }
                }
            }

            float2 wanted = desired * type.Speed + push * Config.SeparationStrength;
            float maxSpeed = type.Speed * 1.6f;
            float wantedSpeed = math.length(wanted);
            if (wantedSpeed > maxSpeed) wanted *= maxSpeed / wantedSpeed;

            float2 velocity = math.lerp(Velocity[i], wanted, 1f - math.exp(-10f * DeltaTime));
            float2 next = p + velocity * DeltaTime;

            if (IsBlocked(next))
            {
                float2 onlyX = new float2(next.x, p.y);
                float2 onlyY = new float2(p.x, next.y);
                if (!IsBlocked(onlyX)) next = onlyX;
                else if (!IsBlocked(onlyY)) next = onlyY;
                else next = p;
            }

            float2 min = Origin + 0.25f;
            float2 max = Origin + new float2(Width, Height) * CellSize - 0.25f;
            next = math.clamp(next, min, max);

            NextPosition[i] = next;
            NextVelocity[i] = velocity;

            float speed = math.length(velocity);
            WalkPhase[i] += speed * DeltaTime;

            float2 look = inReach ? nearestDirection : (speed > 0.15f ? velocity / speed : float2.zero);
            if (math.lengthsq(look) > 1e-4f)
            {
                float targetAngle = math.atan2(look.x, look.y);
                float delta = targetAngle - Facing[i];
                delta -= math.round(delta / (2f * math.PI)) * (2f * math.PI);
                Facing[i] += delta * (1f - math.exp(-12f * DeltaTime));
            }
        }

        private bool IsBlocked(float2 position)
        {
            int x = math.clamp((int)math.floor((position.x - Origin.x) / CellSize), 0, Width - 1);
            int y = math.clamp((int)math.floor((position.y - Origin.y) / CellSize), 0, Height - 1);
            return Cost[y * Width + x] == NavGrid.Blocked;
        }
    }

    public struct EnemyShotRequest
    {
        public float2 Position;
        public float2 Direction;
        public float Speed;
        public float Damage;
    }

    [BurstCompile]
    public struct HordeAttackJob : IJob
    {
        public float DeltaTime;
        public int Count;

        [ReadOnly] public NativeArray<EnemyTypeData> Types;
        [ReadOnly] public NativeArray<TargetData> Targets;
        [ReadOnly] public NativeArray<byte> TypeId;
        [ReadOnly] public NativeArray<int> TargetInRange;
        [ReadOnly] public NativeArray<float2> Position;

        public NativeArray<float> AttackTimer;
        public NativeArray<float> AttackAnim;
        public NativeArray<float> Flash;
        public NativeArray<float> DamageToTarget;
        public NativeList<EnemyShotRequest> Shots;

        public void Execute()
        {
            float fade = DeltaTime * 4f;

            for (int i = 0; i < Count; i++)
            {
                AttackTimer[i] = math.max(0f, AttackTimer[i] - DeltaTime);
                AttackAnim[i] = math.max(0f, AttackAnim[i] - fade);
                Flash[i] = math.max(0f, Flash[i] - DeltaTime * 6f);

                int target = TargetInRange[i];
                if (target < 0 || AttackTimer[i] > 0f) continue;

                EnemyTypeData type = Types[TypeId[i]];
                AttackTimer[i] = type.AttackInterval;
                AttackAnim[i] = 1f;

                if (type.ProjectileSpeed > 0f)
                {
                    float2 direction = math.normalizesafe(Targets[target].Position - Position[i], new float2(0f, 1f));
                    Shots.Add(new EnemyShotRequest
                    {
                        Position = Position[i] + direction * type.Radius,
                        Direction = direction,
                        Speed = type.ProjectileSpeed,
                        Damage = type.AttackDamage,
                    });
                }
                else
                {
                    DamageToTarget[target] += type.AttackDamage;
                }
            }
        }
    }
}
