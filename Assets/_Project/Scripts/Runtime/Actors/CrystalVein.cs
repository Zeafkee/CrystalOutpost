using CrystalOutpost.World;
using UnityEngine;

namespace CrystalOutpost.Actors
{
    public sealed class CrystalVein : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private int capacity = 12;
        [SerializeField] private float range = 2.6f;
        [SerializeField] private float mineInterval = 0.4f;
        [SerializeField] private float regrowInterval = 6f;

        private int _stock;
        private float _timer;
        private Vector3 _fullScale;

        private void Start()
        {
            if (world == null) world = GameWorld.Instance;
            _stock = capacity;
            _fullScale = transform.localScale;
        }

        private void Update()
        {
            if (!world.Running) return;

            Vector3 delta = world.HeroPosition - transform.position;
            delta.y = 0f;
            bool mining = world.HeroAlive && _stock > 0 && delta.sqrMagnitude < range * range;

            if (!mining && _stock >= capacity) return;

            _timer += Time.deltaTime;
            if (mining)
            {
                if (_timer < mineInterval) return;
                _timer = 0f;
                _stock--;
                Vector3 drop = transform.position + delta.normalized * 0.9f;
                world.Sim.SpawnPickup(GameWorld.ToSim(drop), 1);
            }
            else
            {
                if (_timer < regrowInterval) return;
                _timer = 0f;
                _stock++;
            }

            transform.localScale = _fullScale * Mathf.Lerp(0.5f, 1f, _stock / (float)capacity);
        }
    }
}
