using System;
using CrystalOutpost.Data;
using CrystalOutpost.Sim;
using Unity.Mathematics;
using UnityEngine;

namespace CrystalOutpost.World
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }

    [Serializable]
    public struct MapPath
    {
        public Vector2[] points;
        [Min(0.5f)] public float halfWidth;
    }

    [DefaultExecutionOrder(-100)]
    public sealed class GameWorld : MonoBehaviour
    {
        public static GameWorld Instance { get; private set; }

        [Header("Map (XZ plane)")]
        [SerializeField] private int width = 44;
        [SerializeField] private int height = 60;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 origin = new Vector2(-22f, -30f);
        [SerializeField] private MapPath[] paths = new MapPath[0];

        [Header("Core")]
        [SerializeField] private Transform core;
        [SerializeField] private float coreRadius = 1.6f;

        [Header("Enemies")]
        [SerializeField] private EnemyTypeAsset[] enemyTypes = new EnemyTypeAsset[0];
        [SerializeField] private int enemyCapacity = 1024;

        [Header("Lighting (global shader values)")]
        [SerializeField] private Vector3 lightDirection = new Vector3(0.35f, 0.85f, -0.4f);
        [SerializeField] private Color lightColor = new Color(0.95f, 0.9f, 1f);
        [SerializeField] private Color ambientColor = new Color(0.32f, 0.3f, 0.45f);

        private static readonly int LightDirId = Shader.PropertyToID("_CO_LightDir");
        private static readonly int LightColorId = Shader.PropertyToID("_CO_LightColor");
        private static readonly int AmbientId = Shader.PropertyToID("_CO_Ambient");

        private readonly IDamageable[] _owners = new IDamageable[Simulation.MaxTargets];
        private readonly System.Diagnostics.Stopwatch _tickTimer = new System.Diagnostics.Stopwatch();
        private bool _flowDirty;

        public Simulation Sim { get; private set; }
        public float LastTickMilliseconds { get; private set; }
        public EnemyTypeAsset[] EnemyTypes => enemyTypes;
        public Vector2 Origin => origin;
        public Vector2 Size => new Vector2(width * cellSize, height * cellSize);
        public Vector3 CorePosition => core.position;

        public bool Running { get; set; } = true;
        public int Crystals { get; private set; }

        public Vector3 HeroPosition { get; set; }
        public bool HeroAlive { get; set; }

        public event Action<int> CrystalsChanged;
        public event Action<Simulation> Ticked;

        private void Awake()
        {
            Instance = this;
            ApplyLighting();

            var types = new EnemyTypeData[enemyTypes.Length];
            for (int i = 0; i < types.Length; i++) types[i] = enemyTypes[i].ToData();

            Sim = new Simulation(width, height, cellSize, origin, types, enemyCapacity, SimConfig.Default,
                (uint)UnityEngine.Random.Range(1, int.MaxValue));

            for (int p = 0; p < paths.Length; p++)
                PaintPath(paths[p]);

            NavObstacle[] obstacles = FindObjectsByType<NavObstacle>();
            for (int i = 0; i < obstacles.Length; i++)
                Sim.Grid.Paint(ToSim(obstacles[i].transform.position), obstacles[i].Radius, NavGrid.Blocked);

            float2 corePosition = ToSim(core.position);
            Sim.Grid.Paint(corePosition, coreRadius, NavGrid.Blocked);
            Sim.SetCore(corePosition, coreRadius + 0.2f);
        }

        private void OnDestroy()
        {
            Sim?.Dispose();
            Sim = null;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!Running) return;

            if (_flowDirty)
            {
                _flowDirty = false;
                Sim.RebuildFlowField();
            }

            _tickTimer.Restart();
            Sim.Tick(Mathf.Min(Time.deltaTime, 0.05f));
            LastTickMilliseconds = (float)_tickTimer.Elapsed.TotalMilliseconds;

            for (int i = 0; i < Sim.TargetCount; i++)
            {
                float damage = Sim.GetDamageTo(i);
                if (damage > 0f && _owners[i] != null) _owners[i].TakeDamage(damage);
            }

            if (Sim.CollectedLastTick > 0)
                AddCrystals(Sim.CollectedLastTick);

            Ticked?.Invoke(Sim);
        }

        public static float2 ToSim(Vector3 world) => new float2(world.x, world.z);
        public static Vector3 ToWorld(float2 sim, float y = 0f) => new Vector3(sim.x, y, sim.y);

        public int RegisterTarget(IDamageable owner, Vector3 position, float radius, TargetKind kind)
        {
            int handle = Sim.AddTarget(ToSim(position), radius, kind);
            _owners[handle] = owner;
            return handle;
        }

        public void UnregisterTarget(int handle)
        {
            if (Sim == null) return;
            Sim.SetTargetAlive(handle, false);
            _owners[handle] = null;
        }

        public void SetBlocked(Vector3 position, float radius, bool blocked)
        {
            Sim.Grid.Paint(ToSim(position), radius, blocked ? NavGrid.Blocked : NavGrid.GroundCost);
            _flowDirty = true;
        }

        public bool IsBlocked(Vector3 position) => Sim.Grid.IsBlocked(ToSim(position));

        public Vector3 ClampToMap(Vector3 position, float margin)
        {
            position.x = Mathf.Clamp(position.x, origin.x + margin, origin.x + width * cellSize - margin);
            position.z = Mathf.Clamp(position.z, origin.y + margin, origin.y + height * cellSize - margin);
            return position;
        }

        public void AddCrystals(int amount)
        {
            Crystals += amount;
            CrystalsChanged?.Invoke(Crystals);
        }

        public bool TrySpend(int amount)
        {
            if (Crystals < amount) return false;
            Crystals -= amount;
            CrystalsChanged?.Invoke(Crystals);
            return true;
        }

        private void PaintPath(MapPath path)
        {
            if (path.points == null) return;
            for (int i = 0; i + 1 < path.points.Length; i++)
            {
                Vector2 a = path.points[i];
                Vector2 b = path.points[i + 1];
                int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / (cellSize * 0.5f));
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / (float)Mathf.Max(1, steps));
                    Sim.Grid.Paint(new float2(p.x, p.y), path.halfWidth, NavGrid.PathCost);
                }
            }
        }

        private void ApplyLighting()
        {
            Shader.SetGlobalVector(LightDirId, lightDirection.normalized);
            Shader.SetGlobalVector(LightColorId, lightColor);
            Shader.SetGlobalVector(AmbientId, ambientColor);
        }

#if UNITY_EDITOR
        private void OnValidate() => ApplyLighting();
#endif
    }
}
