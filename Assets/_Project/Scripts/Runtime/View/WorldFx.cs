using CrystalOutpost.Actors;
using CrystalOutpost.Sim;
using CrystalOutpost.World;
using Unity.Collections;
using UnityEngine;

namespace CrystalOutpost.View
{
    public sealed class WorldFx : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private Hero hero;
        [SerializeField] private ParticleSystem debris;
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private int maxDeathBurstsPerFrame = 24;
        [SerializeField] private BuildPad[] pads = new BuildPad[0];
        [SerializeField] private Color crystalColor = new Color(0.35f, 0.9f, 1f);

        private void OnEnable()
        {
            world.Ticked += HandleTick;
            if (hero != null) hero.Fired += HandleHeroShot;
            foreach (BuildPad pad in pads)
            {
                pad.Drained += HandleDrain;
                pad.Purchased += HandlePurchase;
            }
        }

        private void OnDisable()
        {
            world.Ticked -= HandleTick;
            if (hero != null) hero.Fired -= HandleHeroShot;
            foreach (BuildPad pad in pads)
            {
                pad.Drained -= HandleDrain;
                pad.Purchased -= HandlePurchase;
            }
        }

        private void HandleTick(Simulation sim)
        {
            NativeList<DeathEvent> deaths = sim.Deaths;
            int bursts = Mathf.Min(deaths.Length, maxDeathBurstsPerFrame);
            for (int i = 0; i < bursts; i++)
            {
                DeathEvent death = deaths[i];
                Color color = world.EnemyTypes[death.TypeId].deathColor;
                Emit(debris, GameWorld.ToWorld(death.Position, 0.7f), color, 7);
            }

            NativeList<ImpactEvent> impacts = sim.Impacts;
            for (int i = 0; i < impacts.Length; i++)
            {
                ImpactEvent impact = impacts[i];
                int count = impact.Radius > 0f ? 18 : 4;
                Emit(sparks, GameWorld.ToWorld(impact.Position, 0.9f), new Color(1f, 0.85f, 0.4f), count);
            }
        }

        private void HandleHeroShot(Vector3 muzzle)
        {
            Emit(sparks, muzzle, new Color(0.4f, 0.95f, 1f), 3);
        }

        private void HandleDrain(BuildPad pad, int amount)
        {
            const float flightTime = 0.28f;
            Vector3 from = world.HeroPosition + Vector3.up * 1.1f;
            Vector3 to = pad.Position + Vector3.up * 0.3f;
            var parameters = new ParticleSystem.EmitParams
            {
                position = from,
                velocity = (to - from) / flightTime,
                startColor = crystalColor,
                startLifetime = flightTime,
                startSize = 0.35f,
                applyShapeToPosition = false,
            };
            sparks.Emit(parameters, Mathf.Min(amount, 3));
        }

        private void HandlePurchase(BuildPad pad)
        {
            Emit(sparks, pad.Position + Vector3.up * 0.5f, crystalColor, 30);
        }

        private static void Emit(ParticleSystem system, Vector3 position, Color color, int count)
        {
            var parameters = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = color,
                applyShapeToPosition = true,
            };
            system.Emit(parameters, count);
        }
    }
}
