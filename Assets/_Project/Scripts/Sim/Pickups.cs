using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CrystalOutpost.Sim
{
    public struct PickupData : IDisposable
    {
        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;
        public NativeArray<int> Value;
        public NativeArray<float> Age;
        public NativeArray<bool> Magnetised;
        public int Count;
        public int Capacity => Position.Length;

        public PickupData(int capacity, Allocator allocator)
        {
            Position = new NativeArray<float2>(capacity, allocator);
            Velocity = new NativeArray<float2>(capacity, allocator);
            Value = new NativeArray<int>(capacity, allocator);
            Age = new NativeArray<float>(capacity, allocator);
            Magnetised = new NativeArray<bool>(capacity, allocator);
            Count = 0;
        }

        public void Spawn(float2 position, float2 velocity, int value)
        {
            if (Count >= Capacity)
            {
                Value[Count - 1] += value;
                return;
            }

            int i = Count++;
            Position[i] = position;
            Velocity[i] = velocity;
            Value[i] = value;
            Age[i] = 0f;
            Magnetised[i] = false;
        }

        public void Dispose()
        {
            Position.Dispose();
            Velocity.Dispose();
            Value.Dispose();
            Age.Dispose();
            Magnetised.Dispose();
        }
    }

    [BurstCompile]
    public struct PickupJob : IJob
    {
        public float DeltaTime;
        public float2 HeroPosition;
        public bool HeroAlive;
        public float PickupRange;
        public float PickupSpeed;
        public float PickupAcceleration;
        public float2 BoundsMin;
        public float2 BoundsMax;

        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;
        public NativeArray<int> Value;
        public NativeArray<float> Age;
        public NativeArray<bool> Magnetised;
        public NativeReference<int> Count;
        public NativeReference<int> Collected;

        public void Execute()
        {
            int count = Count.Value;
            int collected = 0;

            for (int i = count - 1; i >= 0; i--)
            {
                Age[i] += DeltaTime;
                float2 p = Position[i];
                float2 toHero = HeroPosition - p;
                float distance = math.length(toHero);

                if (HeroAlive && !Magnetised[i] && Age[i] > 0.35f && distance < PickupRange)
                    Magnetised[i] = true;

                if (Magnetised[i] && HeroAlive)
                {
                    float speed = math.min(math.length(Velocity[i]) + PickupAcceleration * DeltaTime, PickupSpeed);
                    float step = speed * DeltaTime;

                    if (distance - step < 0.45f)
                    {
                        collected += Value[i];
                        int last = --count;
                        Position[i] = Position[last];
                        Velocity[i] = Velocity[last];
                        Value[i] = Value[last];
                        Age[i] = Age[last];
                        Magnetised[i] = Magnetised[last];
                        continue;
                    }

                    float2 direction = toHero / distance;
                    Velocity[i] = direction * speed;
                    Position[i] = math.clamp(p + direction * step, BoundsMin, BoundsMax);
                    continue;
                }

                Velocity[i] *= math.exp(-6f * DeltaTime);
                Position[i] = math.clamp(p + Velocity[i] * DeltaTime, BoundsMin, BoundsMax);
            }

            Count.Value = count;
            Collected.Value += collected;
        }
    }
}
