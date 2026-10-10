using CrystalOutpost.World;
using Unity.Mathematics;
using UnityEngine;

namespace CrystalOutpost.Actors
{
    public sealed class Turret : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private Transform head;
        [SerializeField] private Transform muzzle;

        [Header("Weapon")]
        [SerializeField] private float range = 9f;
        [SerializeField] private float interval = 0.55f;
        [SerializeField] private float damage = 12f;
        [SerializeField] private float shotSpeed = 24f;
        [SerializeField] private float splashRadius;
        [SerializeField] private int shotKind;

        [Header("Upgrades (per level above 1)")]
        [SerializeField] private float damagePerLevel = 0.6f;
        [SerializeField] private float fireRatePerLevel = 0.2f;

        private float _timer;
        private float _recoil;
        private float _headHeight;
        private bool _started;
        private float _damageScale = 1f;
        private float _intervalScale = 1f;
        private Quaternion _headRotation = Quaternion.identity;

        public int Level { get; private set; } = 1;
        public bool IsCannon => splashRadius > 0f;

        public event System.Action<Turret> Fired;

        private void Start()
        {
            if (world == null) world = GameWorld.Instance;
            if (head != null) _headHeight = head.localPosition.y;
            _started = true;
        }

        private void OnEnable() => _timer = UnityEngine.Random.Range(0f, interval);

        public void SetLevel(int level)
        {
            Level = Mathf.Max(1, level);
            _damageScale = 1f + damagePerLevel * (Level - 1);
            _intervalScale = 1f / (1f + fireRatePerLevel * (Level - 1));
            if (head != null) head.localScale = Vector3.one * (1f + 0.14f * Mathf.Min(Level - 1, 4));
        }

        private void Update()
        {
            if (!_started || !world.Running) return;
            float dt = Time.deltaTime;

            _recoil = Mathf.Max(0f, _recoil - dt * 6f);
            _timer -= dt;

            float2 position = GameWorld.ToSim(transform.position);
            if (world.Sim.TryFindNearestEnemy(position, range, out float2 enemy, out float2 velocity))
            {
                float travel = math.distance(position, enemy) / shotSpeed;
                float2 aim = enemy + velocity * travel;
                float2 direction = math.normalizesafe(aim - position, new float2(0f, 1f));
                _headRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y), Vector3.up);

                if (_timer <= 0f)
                {
                    _timer = interval * _intervalScale;
                    _recoil = 1f;
                    float2 from = muzzle != null ? GameWorld.ToSim(muzzle.position) : position;
                    world.Sim.FireShot(from, direction, shotSpeed, damage * _damageScale, splashRadius, range * 1.2f, (byte)shotKind);
                    Fired?.Invoke(this);
                }
            }

            if (head == null) return;
            head.rotation = Quaternion.Slerp(head.rotation, _headRotation, 1f - Mathf.Exp(-16f * dt));
            head.localPosition = new Vector3(0f, _headHeight, 0f) - head.localRotation * Vector3.forward * (_recoil * 0.18f);
        }
    }
}
