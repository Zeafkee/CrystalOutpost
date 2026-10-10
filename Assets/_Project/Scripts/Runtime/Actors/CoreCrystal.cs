using System;
using CrystalOutpost.Sim;
using CrystalOutpost.World;
using UnityEngine;

namespace CrystalOutpost.Actors
{
    public sealed class CoreCrystal : MonoBehaviour, IDamageable
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private float maxHealth = 1500f;
        [SerializeField] private float radius = 1.6f;
        [SerializeField] private Transform crystal;
        [SerializeField] private float spinSpeed = 25f;
        [SerializeField] private float hoverHeight = 2.1f;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;

        public event Action<float, float> HealthChanged;
        public event Action Destroyed;

        private void Start()
        {
            Health = maxHealth;
            world.RegisterTarget(this, transform.position, radius, TargetKind.Core);
        }

        private void Update()
        {
            if (crystal == null) return;

            crystal.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            Vector3 p = crystal.localPosition;
            p.y = hoverHeight + Mathf.Sin(Time.time * 1.6f) * 0.12f;
            crystal.localPosition = p;
        }

        public void TakeDamage(float amount)
        {
            if (Health <= 0f) return;

            Health = Mathf.Max(0f, Health - amount);
            HealthChanged?.Invoke(Health, maxHealth);
            if (Health <= 0f) Destroyed?.Invoke();
        }

        public void Fortify(float amount)
        {
            if (Health <= 0f) return;

            maxHealth += amount;
            Health += amount;
            HealthChanged?.Invoke(Health, maxHealth);
        }

        public void Repair(float amount)
        {
            if (Health <= 0f) return;

            Health = Mathf.Min(maxHealth, Health + amount);
            HealthChanged?.Invoke(Health, maxHealth);
        }
    }
}
