using CrystalOutpost.Data;
using CrystalOutpost.Sim;
using CrystalOutpost.World;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CrystalOutpost.View
{
    [DefaultExecutionOrder(100)]
    public sealed class HordeRenderer : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private MeshLibrary library;
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color crystalGlow = new Color(0.1f, 0.7f, 1f, 0.22f);
        [SerializeField] private Color orbGlow = new Color(1f, 0.15f, 0.7f, 0.35f);
        [SerializeField] private Color boltGlow = new Color(1f, 0.7f, 0.25f, 0.3f);

        private InstanceBatcher[] _meshBatchers;
        private InstanceBatcher _shadows;
        private InstanceBatcher _glows;

        private NativeArray<Matrix4x4> _enemyMatrices;
        private NativeArray<Vector4> _enemyAnim;
        private Bounds _bounds;
        private int _layer;

        public int DrawCalls { get; private set; }

        private void Start()
        {
            int meshCount = library.meshes.Length;
            _meshBatchers = new InstanceBatcher[meshCount];
            for (int i = 0; i < meshCount; i++)
                _meshBatchers[i] = new InstanceBatcher(library.meshes[i], library.toon, "_Anim");

            Mesh quad = library.Get(MeshId.Quad);
            _shadows = new InstanceBatcher(quad, library.shadow, "_Color");
            _glows = new InstanceBatcher(quad, library.glow, "_Color");

            int capacity = world.Sim.Horde.Capacity;
            _enemyMatrices = new NativeArray<Matrix4x4>(capacity, Allocator.Persistent);
            _enemyAnim = new NativeArray<Vector4>(capacity, Allocator.Persistent);

            Vector2 size = world.Size;
            Vector2 origin = world.Origin;
            _bounds = new Bounds(new Vector3(origin.x + size.x * 0.5f, 2f, origin.y + size.y * 0.5f),
                new Vector3(size.x + 10f, 12f, size.y + 10f));
            _layer = gameObject.layer;
        }

        private void OnDestroy()
        {
            if (_enemyMatrices.IsCreated) _enemyMatrices.Dispose();
            if (_enemyAnim.IsCreated) _enemyAnim.Dispose();
        }

        private void LateUpdate()
        {
            Simulation sim = world.Sim;
            if (sim == null || _meshBatchers == null) return;

            for (int i = 0; i < _meshBatchers.Length; i++) _meshBatchers[i].Begin();
            _shadows.Begin();
            _glows.Begin();

            AddEnemies(sim);
            AddPickups(sim);
            AddShots(sim);
            AddEnemyProjectiles(sim);

            int calls = 0;
            _shadows.Draw(_bounds, _layer);
            calls += _shadows.DrawCalls;
            for (int i = 0; i < _meshBatchers.Length; i++)
            {
                _meshBatchers[i].Draw(_bounds, _layer);
                calls += _meshBatchers[i].DrawCalls;
            }

            _glows.Draw(_bounds, _layer);
            calls += _glows.DrawCalls;
            DrawCalls = calls;
        }

        private void AddEnemies(Simulation sim)
        {
            int count = sim.Horde.Count;
            if (count == 0) return;

            new BuildEnemyInstancesJob
            {
                Position = sim.Horde.Position,
                Facing = sim.Horde.Facing,
                WalkPhase = sim.Horde.WalkPhase,
                Flash = sim.Horde.Flash,
                AttackAnim = sim.Horde.AttackAnim,
                TypeId = sim.Horde.TypeId,
                Types = sim.Types,
                Matrices = _enemyMatrices,
                Anim = _enemyAnim,
            }.Schedule(count, 64).Complete();

            NativeArray<byte> typeIds = sim.Horde.TypeId;
            NativeArray<EnemyTypeData> types = sim.Types;
            NativeArray<float2> positions = sim.Horde.Position;

            for (int i = 0; i < count; i++)
            {
                EnemyTypeData type = types[typeIds[i]];
                _meshBatchers[type.MeshId].Add(_enemyMatrices[i], _enemyAnim[i]);

                float shadowSize = type.Radius * type.Scale * 3.2f;
                _shadows.Add(GroundQuad(positions[i], shadowSize, 0.03f), shadowColor);
            }
        }

        [BurstCompile]
        private struct BuildEnemyInstancesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float2> Position;
            [ReadOnly] public NativeArray<float> Facing;
            [ReadOnly] public NativeArray<float> WalkPhase;
            [ReadOnly] public NativeArray<float> Flash;
            [ReadOnly] public NativeArray<float> AttackAnim;
            [ReadOnly] public NativeArray<byte> TypeId;
            [ReadOnly] public NativeArray<EnemyTypeData> Types;
            [WriteOnly] public NativeArray<Matrix4x4> Matrices;
            [WriteOnly] public NativeArray<Vector4> Anim;

            public void Execute(int i)
            {
                float scale = Types[TypeId[i]].Scale;
                float3 position = new float3(Position[i].x, 0f, Position[i].y);
                quaternion rotation = quaternion.RotateY(Facing[i]);
                Matrices[i] = float4x4.TRS(position, rotation, new float3(scale));
                Anim[i] = new Vector4(WalkPhase[i], Flash[i], AttackAnim[i], 0f);
            }
        }

        private void AddPickups(Simulation sim)
        {
            InstanceBatcher crystals = _meshBatchers[(int)MeshId.Crystal];
            PickupData pickups = sim.Pickups;

            for (int i = 0; i < pickups.Count; i++)
            {
                float age = pickups.Age[i];
                float arc = age < 0.4f ? Mathf.Sin(age / 0.4f * Mathf.PI) * 0.9f : 0f;
                float y = 0.3f + arc + Mathf.Sin(age * 4f + i) * 0.06f;
                float2 p = pickups.Position[i];

                var matrix = Matrix4x4.TRS(new Vector3(p.x, y, p.y), Quaternion.Euler(0f, age * 140f + i * 37f, 0f), Vector3.one);
                crystals.Add(matrix, Vector4.zero);
                _glows.Add(GroundQuad(p, 1.1f, 0.04f), crystalGlow);
            }
        }

        private void AddShots(Simulation sim)
        {
            ShotData shots = sim.Shots;
            for (int i = 0; i < shots.Count; i++)
            {
                float2 p = shots.Position[i];
                float2 v = shots.Velocity[i];
                MeshId mesh = shots.Kind[i] == 0 ? MeshId.Bolt : MeshId.Shell;
                Quaternion rotation = Quaternion.LookRotation(new Vector3(v.x, 0f, v.y), Vector3.up);
                _meshBatchers[(int)mesh].Add(Matrix4x4.TRS(new Vector3(p.x, 1.1f, p.y), rotation, Vector3.one), Vector4.zero);
                _glows.Add(GroundQuad(p, 1.4f, 0.05f), boltGlow);
            }
        }

        private void AddEnemyProjectiles(Simulation sim)
        {
            InstanceBatcher orbs = _meshBatchers[(int)MeshId.EnemyOrb];
            EnemyProjectileData projectiles = sim.EnemyProjectiles;
            for (int i = 0; i < projectiles.Count; i++)
            {
                float2 p = projectiles.Position[i];
                float spin = projectiles.Life[i] * 400f;
                orbs.Add(Matrix4x4.TRS(new Vector3(p.x, 1f, p.y), Quaternion.Euler(spin, spin * 0.7f, 0f), Vector3.one), Vector4.zero);
                _glows.Add(GroundQuad(p, 1.6f, 0.05f), orbGlow);
            }
        }

        private static Matrix4x4 GroundQuad(float2 position, float size, float height)
        {
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = size;
            m.m22 = size;
            m.m03 = position.x;
            m.m13 = height;
            m.m23 = position.y;
            return m;
        }
    }
}
