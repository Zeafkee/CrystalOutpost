using System;
using CrystalOutpost.Sim;
using CrystalOutpost.World;
using UnityEngine;

namespace CrystalOutpost.Actors
{
    public sealed class Structure : MonoBehaviour, IDamageable
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private float maxHealth = 200f;
        [SerializeField] private float radius = 0.9f;
        [SerializeField] private float blockRadius = 0.7f;
        [SerializeField] private Renderer[] renderers = new Renderer[0];

        private static readonly int AnimId = Shader.PropertyToID("_Anim");

        private MaterialPropertyBlock _block;
        private int _targetHandle = -1;
        private bool _started;
        private float _flash;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;

        public event Action<Structure> Destroyed;

        private void Start()
        {
            _started = true;
            Register();
        }

        private void OnEnable()
        {
            Health = maxHealth;
            _flash = 0f;
            if (_started) Register();
        }

        private void OnDisable() => Unregister();

        private void Register()
        {
            if (world == null) world = GameWorld.Instance;
            if (world == null || world.Sim == null || _targetHandle >= 0) return;

            _targetHandle = world.RegisterTarget(this, transform.position, radius, TargetKind.Structure);
            world.SetBlocked(transform.position, blockRadius, true);
        }

        private void Unregister()
        {
            if (world == null || world.Sim == null || _targetHandle < 0) return;

            world.UnregisterTarget(_targetHandle);
            world.SetBlocked(transform.position, blockRadius, false);
            _targetHandle = -1;
        }

        private void Update()
        {
            if (_flash <= 0f) return;
            _flash = Mathf.Max(0f, _flash - Time.deltaTime * 5f);
            ApplyFlash();
        }

        public void TakeDamage(float amount)
        {
            if (Health <= 0f) return;

            Health -= amount;
            _flash = 1f;
            if (Health > 0f) return;

            _flash = 0f;
            ApplyFlash();
            gameObject.SetActive(false);
            Destroyed?.Invoke(this);
        }

        private void ApplyFlash()
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            _block.SetVector(AnimId, new Vector4(0f, _flash, 0f, 0f));
            for (int i = 0; i < renderers.Length; i++) renderers[i].SetPropertyBlock(_block);
        }
    }
}
