using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace CrystalOutpost.Sim.Tests
{
    public class SimulationTests
    {
        private const float Dt = 1f / 60f;
        private const int Melee = 0;
        private const int Ranged = 1;

        private Simulation _sim;
        private int _core;

        private static EnemyTypeData[] Types() => new[]
        {
            new EnemyTypeData
            {
                MaxHealth = 20f, Speed = 3f, Radius = 0.4f, AttackRange = 0.3f, AttackDamage = 5f,
                AttackInterval = 1f, ProjectileSpeed = 0f, CrystalDrop = 2, Scale = 1f,
            },
            new EnemyTypeData
            {
                MaxHealth = 10f, Speed = 3f, Radius = 0.35f, AttackRange = 6f, AttackDamage = 3f,
                AttackInterval = 1f, ProjectileSpeed = 10f, CrystalDrop = 1, Scale = 1f,
            },
        };

        [SetUp]
        public void SetUp()
        {
            _sim = new Simulation(30, 30, 1f, new float2(-15f, -15f), Types(), 512, SimConfig.Default, 7);
            _sim.Grid.Paint(float2.zero, 1.2f, NavGrid.Blocked);
            _sim.SetCore(float2.zero, 1.2f);
            _core = _sim.AddTarget(float2.zero, 1.2f, TargetKind.Core);
        }

        [TearDown]
        public void TearDown() => _sim.Dispose();

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt) _sim.Tick(Dt);
        }

        [Test]
        public void FlowField_PointsTowardsTheCore()
        {
            float2 east = _sim.Grid.Flow[_sim.Grid.IndexOf(new float2(8.5f, 0.5f))];
            float2 north = _sim.Grid.Flow[_sim.Grid.IndexOf(new float2(0.5f, 9.5f))];

            Assert.Less(east.x, -0.5f, "a cell east of the core should point west");
            Assert.Less(north.y, -0.5f, "a cell north of the core should point south");
        }

        [Test]
        public void FlowField_RoutesAroundAWall()
        {
            for (float y = -14.5f; y <= 9.5f; y += 1f)
                _sim.Grid.Paint(new float2(5.5f, y), 0.4f, NavGrid.Blocked);
            _sim.RebuildFlowField();

            _sim.SpawnEnemy(Melee, new float2(10.5f, 0.5f));
            Run(25f);

            Assert.AreEqual(1, _sim.Horde.Count);
            Assert.Less(math.length(_sim.Horde.Position[0]), 3f, "the enemy should have walked round the wall to the core");
        }

        [Test]
        public void Enemies_SpawnedOnTheSameSpot_SpreadOut()
        {
            for (int i = 0; i < 40; i++)
                _sim.SpawnEnemy(Melee, new float2(9f + i * 0.001f, 9f));
            Run(3f);

            float closest = float.MaxValue;
            for (int a = 0; a < _sim.Horde.Count; a++)
                for (int b = a + 1; b < _sim.Horde.Count; b++)
                    closest = math.min(closest, math.distance(_sim.Horde.Position[a], _sim.Horde.Position[b]));

            Assert.Greater(closest, 0.3f, "separation should keep bodies apart");
        }

        [Test]
        public void Melee_WalksToTheCore_AndDamagesIt()
        {
            _sim.SpawnEnemy(Melee, new float2(6f, 0f));

            float damage = 0f;
            for (float t = 0f; t < 6f; t += Dt)
            {
                _sim.Tick(Dt);
                damage += _sim.GetDamageTo(_core);
            }

            Assert.Greater(damage, 0f);
            Assert.Less(math.length(_sim.Horde.Position[0]), 2.2f, "melee stops at the core's edge");
        }

        [Test]
        public void Ranged_StopsAtRange_AndShoots()
        {
            _sim.SpawnEnemy(Ranged, new float2(13f, 0f));

            float damage = 0f;
            bool sawProjectile = false;
            for (float t = 0f; t < 8f; t += Dt)
            {
                _sim.Tick(Dt);
                damage += _sim.GetDamageTo(_core);
                sawProjectile |= _sim.EnemyProjectiles.Count > 0;
            }

            float distance = math.length(_sim.Horde.Position[0]);
            Assert.Greater(distance, 5f, "ranged enemies keep their distance");
            Assert.IsTrue(sawProjectile, "ranged enemies fire projectiles");
            Assert.Greater(damage, 0f, "projectiles reach the core");
        }

        [Test]
        public void Shot_KillsEnemy_WhichDropsCrystals()
        {
            _sim.SpawnEnemy(Ranged, new float2(8f, 8f));
            _sim.Tick(Dt);

            float2 from = new float2(4f, 8f);
            _sim.FireShot(from, new float2(1f, 0f), 20f, 50f, 0f, 10f, 0);

            int deaths = 0;
            for (float t = 0f; t < 1f; t += Dt)
            {
                _sim.Tick(Dt);
                deaths += _sim.Deaths.Length;
            }

            Assert.AreEqual(0, _sim.Horde.Count);
            Assert.AreEqual(1, deaths);
            Assert.AreEqual(1, _sim.KillCount);
            Assert.AreEqual(1, _sim.Pickups.Count);
            Assert.AreEqual(0, _sim.Shots.Count, "the shot is consumed by the hit");
        }

        [Test]
        public void FastShot_OnALongFrame_DoesNotJumpOverAnEnemy()
        {
            _sim.SpawnEnemy(Ranged, new float2(8f, 8f));
            _sim.Tick(Dt);

            float2 target = _sim.Horde.Position[0];
            _sim.FireShot(target - new float2(4.5f, 0f), new float2(1f, 0f), 60f, 50f, 0f, 20f, 0);
            _sim.Tick(0.05f);
            _sim.Tick(0.05f);
            _sim.Tick(0.05f);

            Assert.AreEqual(0, _sim.Horde.Count);
        }

        [Test]
        public void SplashShot_HitsEveryEnemyInRadius()
        {
            for (int i = 0; i < 5; i++)
                _sim.SpawnEnemy(Ranged, new float2(8f + i * 0.5f, 8f));
            _sim.SpawnEnemy(Ranged, new float2(-8f, -8f));
            _sim.Tick(Dt);

            _sim.FireShot(new float2(4f, 8f), new float2(1f, 0f), 20f, 50f, 3f, 10f, 1);
            Run(1f);

            Assert.AreEqual(1, _sim.Horde.Count);
        }

        [Test]
        public void DamageEnemiesInRadius_OnlyHitsEnemiesInside()
        {
            _sim.SpawnEnemy(Melee, new float2(8f, 8f));
            _sim.SpawnEnemy(Melee, new float2(8.5f, 8f));
            _sim.SpawnEnemy(Melee, new float2(-9f, 3f));
            _sim.Tick(Dt);

            int hits = _sim.DamageEnemiesInRadius(new float2(8f, 8f), 1f, 100f);
            _sim.Tick(Dt);

            Assert.AreEqual(2, hits);
            Assert.AreEqual(1, _sim.Horde.Count);
        }

        [Test]
        public void Crystals_FlyToTheHero_AndAreCounted()
        {
            _sim.AddTarget(new float2(8f, 8f), 0.5f, TargetKind.Hero);
            _sim.Config.AggroRadius = 0f;
            for (int i = 0; i < 6; i++)
                _sim.SpawnPickup(new float2(9f, 8f), 2);

            int collected = 0;
            for (float t = 0f; t < 3f; t += Dt)
            {
                _sim.Tick(Dt);
                collected += _sim.CollectedLastTick;
            }

            Assert.AreEqual(12, collected);
            Assert.AreEqual(0, _sim.Pickups.Count);
        }

        [Test]
        public void TryFindNearestEnemy_ReturnsTheClosestInRange()
        {
            _sim.SpawnEnemy(Melee, new float2(10f, 10f));
            _sim.SpawnEnemy(Melee, new float2(7f, 10f));
            _sim.Tick(Dt);

            bool found = _sim.TryFindNearestEnemy(new float2(5f, 10f), 4f, out float2 position, out _);
            bool none = _sim.TryFindNearestEnemy(new float2(-10f, -10f), 4f, out _, out _);

            Assert.IsTrue(found);
            Assert.Less(math.distance(position, new float2(7f, 10f)), 0.5f);
            Assert.IsFalse(none);
        }

        [Test]
        public void LargeHorde_RunsWithoutErrors()
        {
            var random = new Unity.Mathematics.Random(3);
            for (int i = 0; i < 500; i++)
                _sim.SpawnEnemy(i % 4 == 0 ? Ranged : Melee, random.NextFloat2(new float2(-14f, 8f), new float2(14f, 14f)));

            Run(5f);

            Assert.AreEqual(500, _sim.Horde.Count);
            for (int i = 0; i < _sim.Horde.Count; i++)
            {
                Assert.IsFalse(math.any(math.isnan(_sim.Horde.Position[i])), "position became NaN");
                Assert.IsFalse(_sim.Grid.IsBlocked(_sim.Horde.Position[i]), "enemy inside a blocked cell");
            }
        }

        [Test]
        public void Tick_InSteadyState_DoesNotAllocateManagedMemory()
        {
            var random = new Unity.Mathematics.Random(5);
            for (int i = 0; i < 500; i++)
                _sim.SpawnEnemy(i % 4 == 0 ? Ranged : Melee, random.NextFloat2(new float2(-14f, 6f), new float2(14f, 14f)));
            _sim.AddTarget(new float2(0f, 4f), 0.45f, TargetKind.Hero);
            for (int i = 0; i < 40; i++) _sim.SpawnPickup(random.NextFloat2(new float2(-5f, 2f), new float2(5f, 6f)), 1);
            Run(1f);

            Assert.That(() =>
            {
                for (int i = 0; i < 60; i++)
                {
                    _sim.FireShot(new float2(0f, 4f), new float2(0f, 1f), 28f, 12f, 0f, 9f, 0);
                    _sim.Tick(Dt);
                }
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
