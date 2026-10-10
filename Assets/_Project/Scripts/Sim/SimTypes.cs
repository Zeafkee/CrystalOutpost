using Unity.Mathematics;

namespace CrystalOutpost.Sim
{
    public struct EnemyTypeData
    {
        public float MaxHealth;
        public float Speed;
        public float Radius;
        public float AttackRange;
        public float AttackDamage;
        public float AttackInterval;
        public float ProjectileSpeed;
        public int CrystalDrop;
        public int MeshId;
        public float Scale;

        public bool IsRanged => ProjectileSpeed > 0f;
    }

    public enum TargetKind : byte
    {
        Core = 0,
        Hero = 1,
        Structure = 2,
    }

    public struct TargetData
    {
        public float2 Position;
        public float Radius;
        public TargetKind Kind;
        public bool Alive;
    }

    public struct DeathEvent
    {
        public float2 Position;
        public byte TypeId;
    }

    public struct ImpactEvent
    {
        public float2 Position;
        public float Radius;
    }

    public struct SimConfig
    {
        public float AggroRadius;
        public float SeparationStrength;
        public float PickupRange;
        public float PickupSpeed;
        public float PickupAcceleration;

        public static SimConfig Default => new SimConfig
        {
            AggroRadius = 4.5f,
            SeparationStrength = 8f,
            PickupRange = 3f,
            PickupSpeed = 22f,
            PickupAcceleration = 60f,
        };
    }
}
