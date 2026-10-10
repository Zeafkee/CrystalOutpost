#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CrystalOutpost.Actors;
using CrystalOutpost.Sim;
using CrystalOutpost.UI;
using CrystalOutpost.World;
using Unity.Mathematics;
using UnityEngine;

namespace CrystalOutpost.Dev
{
    public sealed class DebugAutoPilot : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private Joystick joystick;
        [SerializeField] private Transform hero;
        [SerializeField] private float fightDistance = 7f;

        private BuildPad[] _pads;

        public void Bind(GameWorld gameWorld, Joystick stick, Transform heroTransform)
        {
            world = gameWorld;
            joystick = stick;
            hero = heroTransform;
        }

        private void OnDisable()
        {
            if (joystick != null) joystick.Override = null;
        }

        private void Update()
        {
            if (world == null || world.Sim == null || joystick == null || hero == null) return;

            Simulation sim = world.Sim;
            float2 position = GameWorld.ToSim(hero.position);
            float2 home = GameWorld.ToSim(world.CorePosition) + new float2(0f, 5f);
            float2 goal = home;
            float stopDistance = 0.5f;

            float best = 10f * 10f;
            bool foundCrystal = false;
            for (int i = 0; i < sim.Pickups.Count; i++)
            {
                float d = math.distancesq(position, sim.Pickups.Position[i]);
                if (d >= best || math.distancesq(home, sim.Pickups.Position[i]) > 16f * 16f) continue;
                best = d;
                goal = sim.Pickups.Position[i];
                foundCrystal = true;
            }

            if (sim.TryFindNearestEnemy(position, 16f, out float2 enemy, out _))
            {
                bool inRange = math.distance(position, enemy) < fightDistance;
                if (inRange || !foundCrystal)
                {
                    goal = enemy;
                    stopDistance = fightDistance;
                }
            }

            if (math.distance(goal, home) > 16f) goal = home;

            if (_pads == null) _pads = FindObjectsByType<BuildPad>();
            int cheapest = int.MaxValue;
            bool weaponsLeft = false;
            for (int i = 0; i < _pads.Length; i++)
                weaponsLeft |= _pads[i].HasTurret && _pads[i].Available;

            for (int i = 0; i < _pads.Length; i++)
            {
                BuildPad pad = _pads[i];
                if (!pad.Available || !pad.Useful) continue;
                if (weaponsLeft && !pad.HasTurret && pad.Kind != PadKind.HeroDamage && pad.Kind != PadKind.RepairCore) continue;
                bool standing = math.distance(position, GameWorld.ToSim(pad.Position)) < 1f;
                if (pad.RemainingCost > world.Crystals && !standing) continue;
                if (standing && world.Crystals == 0) continue;

                int group = pad.HasTurret ? 0 : pad.Kind == PadKind.HeroDamage ? 1 : 2;
                int cost = standing ? -1 : group * 10000 + pad.RemainingCost;
                if (cost >= cheapest) continue;
                cheapest = cost;
                goal = GameWorld.ToSim(pad.Position);
                stopDistance = 0.4f;
            }

            float2 delta = goal - position;
            float distance = math.length(delta);
            joystick.Override = distance > stopDistance ? (Vector2)(delta / distance) : Vector2.zero;
        }
    }
}
#endif
