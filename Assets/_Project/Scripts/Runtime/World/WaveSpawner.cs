using System;
using CrystalOutpost.Data;
using Unity.Mathematics;
using UnityEngine;

namespace CrystalOutpost.World
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        private enum Phase { Preparing, Spawning, Clearing, Finished }

        [SerializeField] private GameWorld world;
        [SerializeField] private WaveAsset[] waves = new WaveAsset[0];
        [SerializeField] private Transform[] spawnPoints = new Transform[0];
        [SerializeField] private float spawnJitter = 2.5f;
        [SerializeField] private float endlessGrowth = 1.12f;

        private Phase _phase = Phase.Preparing;
        private float _timer;
        private int _spawned;
        private int _waveTotal;
        private float _spawnAccumulator;
        private int[] _typeIndex = new int[0];
        private int[] _remainingPerGroup = new int[0];

        public int WaveNumber { get; private set; }
        public int WaveCount => waves.Length;
        public bool Finished => _phase == Phase.Finished;
        public bool Endless { get; private set; }
        public float PrepareTimeLeft => _phase == Phase.Preparing ? _timer : 0f;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;
        public event Action AllWavesCleared;

        private void Start() => BeginPrepare(0);

        private void Update()
        {
            if (!world.Running || _phase == Phase.Finished) return;
            float dt = Time.deltaTime;

            switch (_phase)
            {
                case Phase.Preparing:
                    _timer -= dt;
                    if (_timer <= 0f) BeginSpawning();
                    break;

                case Phase.Spawning:
                    SpawnTick(dt);
                    if (_spawned >= _waveTotal) _phase = Phase.Clearing;
                    break;

                case Phase.Clearing:
                    if (world.Sim.Horde.Count > 0) break;
                    WaveCleared?.Invoke(WaveNumber);
                    if (WaveNumber >= waves.Length && !Endless)
                    {
                        _phase = Phase.Finished;
                        AllWavesCleared?.Invoke();
                    }
                    else
                    {
                        BeginPrepare(WaveNumber);
                    }
                    break;
            }
        }

        public void ContinueEndless()
        {
            if (waves.Length == 0) return;
            Endless = true;
            BeginPrepare(WaveNumber);
        }

        private WaveAsset WaveAt(int index) => waves[Mathf.Min(index, waves.Length - 1)];

        private void BeginPrepare(int waveIndex)
        {
            if (waves.Length == 0)
            {
                _phase = Phase.Finished;
                return;
            }

            _phase = Phase.Preparing;
            _timer = WaveAt(waveIndex).prepareTime;
        }

        private void BeginSpawning()
        {
            WaveAsset wave = WaveAt(WaveNumber);
            float multiplier = Mathf.Pow(endlessGrowth, Mathf.Max(0, WaveNumber - waves.Length + 1));
            WaveNumber++;

            if (_typeIndex.Length != wave.groups.Length)
            {
                _typeIndex = new int[wave.groups.Length];
                _remainingPerGroup = new int[wave.groups.Length];
            }

            for (int g = 0; g < wave.groups.Length; g++)
            {
                _typeIndex[g] = Array.IndexOf(world.EnemyTypes, wave.groups[g].type);
                _remainingPerGroup[g] = _typeIndex[g] >= 0 ? Mathf.RoundToInt(wave.groups[g].count * multiplier) : 0;
            }

            _waveTotal = 0;
            for (int g = 0; g < _remainingPerGroup.Length; g++) _waveTotal += _remainingPerGroup[g];

            _spawned = 0;
            _spawnAccumulator = 0f;
            _timer = wave.spawnDuration;
            _phase = Phase.Spawning;
            WaveStarted?.Invoke(WaveNumber);
        }

        private void SpawnTick(float dt)
        {
            WaveAsset wave = WaveAt(WaveNumber - 1);
            _spawnAccumulator += dt * _waveTotal / Mathf.Max(0.1f, wave.spawnDuration);

            while (_spawnAccumulator >= 1f && _spawned < _waveTotal)
            {
                _spawnAccumulator -= 1f;

                int pick = UnityEngine.Random.Range(0, _waveTotal - _spawned);
                int group = 0;
                while (pick >= _remainingPerGroup[group])
                {
                    pick -= _remainingPerGroup[group];
                    group++;
                }

                Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                Vector2 jitter = UnityEngine.Random.insideUnitCircle * spawnJitter;
                Vector3 position = world.ClampToMap(point.position + new Vector3(jitter.x, 0f, jitter.y), 0.6f);

                if (!world.Sim.SpawnEnemy(_typeIndex[group], new float2(position.x, position.z)))
                    return;

                _remainingPerGroup[group]--;
                _spawned++;
            }
        }
    }
}
