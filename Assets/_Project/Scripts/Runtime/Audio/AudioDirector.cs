using CrystalOutpost.Actors;
using CrystalOutpost.Sim;
using CrystalOutpost.World;
using UnityEngine;

namespace CrystalOutpost.Audio
{
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private WaveSpawner waves;
        [SerializeField] private GameSession session;
        [SerializeField] private CoreCrystal core;
        [SerializeField] private Hero hero;
        [SerializeField] private Turret[] turrets = new Turret[0];
        [SerializeField] private BuildPad[] pads = new BuildPad[0];

        private float _coreHealth = float.MaxValue;

        private void OnEnable()
        {
            world.Ticked += HandleTick;
            waves.WaveStarted += HandleWaveStarted;
            session.Ended += HandleEnded;
            core.HealthChanged += HandleCoreHealth;
            hero.Fired += HandleHeroShot;
            foreach (Turret turret in turrets) turret.Fired += HandleTurretShot;
            foreach (BuildPad pad in pads)
            {
                pad.Drained += HandleDrain;
                pad.Purchased += HandlePurchase;
            }
        }

        private void OnDisable()
        {
            world.Ticked -= HandleTick;
            waves.WaveStarted -= HandleWaveStarted;
            session.Ended -= HandleEnded;
            core.HealthChanged -= HandleCoreHealth;
            hero.Fired -= HandleHeroShot;
            foreach (Turret turret in turrets) turret.Fired -= HandleTurretShot;
            foreach (BuildPad pad in pads)
            {
                pad.Drained -= HandleDrain;
                pad.Purchased -= HandlePurchase;
            }
        }

        private void HandleTick(Simulation sim)
        {
            int deaths = sim.Deaths.Length;
            if (deaths > 0) AudioManager.Play(Sfx.EnemyDie, Mathf.Min(1f, 0.6f + deaths * 0.1f));
            if (sim.CollectedLastTick > 0) AudioManager.Play(Sfx.Pickup);
        }

        private void HandleHeroShot(Vector3 muzzle) => AudioManager.Play(Sfx.Shoot);
        private void HandleTurretShot(Turret turret) => AudioManager.Play(turret.IsCannon ? Sfx.Cannon : Sfx.Turret);
        private void HandleDrain(BuildPad pad, int amount) => AudioManager.Play(Sfx.Drain);
        private void HandlePurchase(BuildPad pad) => AudioManager.Play(pad.LastPurchaseBuilt ? Sfx.Build : Sfx.Upgrade);
        private void HandleWaveStarted(int wave) => AudioManager.Play(Sfx.WaveStart);
        private void HandleEnded(bool won) => AudioManager.Play(won ? Sfx.Win : Sfx.Lose);

        private void HandleCoreHealth(float health, float max)
        {
            if (health < _coreHealth) AudioManager.Play(Sfx.CoreHit);
            _coreHealth = health;
        }
    }
}
