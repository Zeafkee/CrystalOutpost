using System;
using CrystalOutpost.Sim;
using CrystalOutpost.UI;
using CrystalOutpost.World;
using Unity.Mathematics;
using UnityEngine;

namespace CrystalOutpost.Actors
{
    public sealed class Hero : MonoBehaviour, IDamageable
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private Joystick joystick;
        [SerializeField] private Renderer body;
        [SerializeField] private Transform respawnPoint;

        [Header("Movement")]
        [SerializeField] private float speed = 6.5f;
        [SerializeField] private float radius = 0.45f;
        [SerializeField] private float turnSpeed = 14f;

        [Header("Combat")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float attackInterval = 0.32f;
        [SerializeField] private float attackRange = 9f;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float shotSpeed = 28f;
        [SerializeField] private float stillThreshold = 0.08f;
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0.1f, 0.84f, 1.4f);
        [SerializeField] private float regenDelay = 3f;
        [SerializeField] private float regenPerSecond = 12f;
        [SerializeField] private float respawnTime = 3f;

        private static readonly int AnimId = Shader.PropertyToID("_Anim");
        private const byte HeroShotKind = 1;

        private static readonly float[] SlideAngles = { 35f, 65f, 95f, 125f, 155f };

        private float _slideSide = 1f;
        private MaterialPropertyBlock _block;
        private Transform _transform;
        private int _targetHandle;
        private float _attackTimer;
        private float _walkPhase;
        private float _flash;
        private float _attackAnim;
        private float _sinceDamage;
        private float _respawnTimer;
        private Vector3 _facing = Vector3.forward;
        private bool _moving;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsAlive { get; private set; } = true;
        public float AttackDamage { get => attackDamage; set => attackDamage = value; }
        public float Speed { get => speed; set => speed = value; }

        public event Action<Vector3> Fired;
        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Start()
        {
            _transform = transform;
            _block = new MaterialPropertyBlock();
            Health = maxHealth;
            _targetHandle = world.RegisterTarget(this, _transform.position, radius, TargetKind.Hero);
            world.HeroPosition = _transform.position;
            world.HeroAlive = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (!world.Running) return;

            if (!IsAlive)
            {
                _respawnTimer -= dt;
                if (_respawnTimer <= 0f) Respawn();
                return;
            }

            Move(dt);
            Attack(dt);
            Regenerate(dt);

            _flash = Mathf.Max(0f, _flash - dt * 5f);
            _attackAnim = Mathf.Max(0f, _attackAnim - dt * 4.5f);
            _block.SetVector(AnimId, new Vector4(_walkPhase, _flash, _attackAnim, 0f));
            body.SetPropertyBlock(_block);

            world.Sim.MoveTarget(_targetHandle, GameWorld.ToSim(_transform.position));
            world.HeroPosition = _transform.position;
        }

        private void Move(float dt)
        {
            Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
            Vector3 direction = new Vector3(input.x, 0f, input.y);
            float magnitude = Mathf.Min(direction.magnitude, 1f);
            _moving = magnitude > stillThreshold;

            if (_moving)
            {
                Vector3 step = direction.normalized * (magnitude * speed * dt);
                Vector3 position = _transform.position;

                _transform.position = world.ClampToMap(Slide(position, step), radius + 0.3f);
                _walkPhase += magnitude * speed * dt;
                _facing = direction.normalized;
            }

            Quaternion target = Quaternion.LookRotation(_facing, Vector3.up);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * dt));
        }

        private Vector3 Slide(Vector3 position, Vector3 step)
        {
            Vector3 next = position + step;
            if (!world.IsBlocked(next) || world.IsBlocked(position)) return next;

            for (int i = 0; i < SlideAngles.Length; i++)
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    float side = attempt == 0 ? _slideSide : -_slideSide;
                    Vector3 turned = position + Quaternion.Euler(0f, SlideAngles[i] * side, 0f) * step;
                    if (world.IsBlocked(turned)) continue;
                    _slideSide = side;
                    return turned;
                }
            }

            return position;
        }

        private void Attack(float dt)
        {
            _attackTimer -= dt;
            if (_moving || _attackTimer > 0f) return;

            float2 position = GameWorld.ToSim(_transform.position);
            if (!world.Sim.TryFindNearestEnemy(position, attackRange, out float2 enemy, out float2 velocity)) return;

            _attackTimer = attackInterval;
            _attackAnim = 1f;

            float travel = math.distance(position, enemy) / shotSpeed;
            float2 direction = math.normalizesafe(enemy + velocity * travel - position, new float2(0f, 1f));
            _facing = new Vector3(direction.x, 0f, direction.y);

            Vector3 muzzle = _transform.position + Quaternion.LookRotation(_facing, Vector3.up) * muzzleOffset;
            world.Sim.FireShot(GameWorld.ToSim(muzzle), direction, shotSpeed, attackDamage, 0f, attackRange * 1.15f, HeroShotKind);
            Fired?.Invoke(muzzle);
        }

        private void Regenerate(float dt)
        {
            _sinceDamage += dt;
            if (_sinceDamage < regenDelay || Health >= maxHealth) return;

            Health = Mathf.Min(maxHealth, Health + regenPerSecond * dt);
            HealthChanged?.Invoke(Health, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            Health -= amount;
            _flash = 1f;
            _sinceDamage = 0f;
            HealthChanged?.Invoke(Mathf.Max(Health, 0f), maxHealth);

            if (Health > 0f) return;

            IsAlive = false;
            _respawnTimer = respawnTime;
            body.enabled = false;
            world.Sim.SetTargetAlive(_targetHandle, false);
            world.HeroAlive = false;
            Died?.Invoke();
        }

        private void Respawn()
        {
            IsAlive = true;
            Health = maxHealth;
            _sinceDamage = regenDelay;
            if (respawnPoint != null) _transform.position = respawnPoint.position;
            body.enabled = true;
            world.Sim.MoveTarget(_targetHandle, GameWorld.ToSim(_transform.position));
            world.Sim.SetTargetAlive(_targetHandle, true);
            world.HeroPosition = _transform.position;
            world.HeroAlive = true;
            HealthChanged?.Invoke(Health, maxHealth);
        }
    }
}
