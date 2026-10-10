using System;
using CrystalOutpost.Actors;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrystalOutpost.World
{
    public sealed class GameSession : MonoBehaviour
    {
        public enum State { Menu, Playing, Paused, Won, Lost }

        [SerializeField] private GameWorld world;
        [SerializeField] private WaveSpawner waves;
        [SerializeField] private CoreCrystal core;
        [SerializeField] private int startingCrystals = 50;
        [SerializeField] private bool showMenu = true;

        private const string BestWaveKey = "best_wave";
        private static bool _skipMenuOnce;

        public State Current { get; private set; } = State.Playing;
        public int Kills => world.Sim != null ? world.Sim.KillCount : 0;
        public int WaveNumber => waves.WaveNumber;
        public int WaveCount => waves.WaveCount;
        public bool Endless => waves.Endless;
        public int BestWave => PlayerPrefs.GetInt(BestWaveKey, 0);

        public event Action<State> StateChanged;
        public event Action<bool> Ended;

        private void Awake()
        {
            bool menu = showMenu && !_skipMenuOnce;
            _skipMenuOnce = false;
            Current = menu ? State.Menu : State.Playing;
            world.Running = !menu;
        }

        private void Start()
        {
            core.Destroyed += Lose;
            waves.AllWavesCleared += Win;
            if (startingCrystals > 0) world.AddCrystals(startingCrystals);
            StateChanged?.Invoke(Current);
        }

        private void OnDestroy()
        {
            if (core != null) core.Destroyed -= Lose;
            if (waves != null) waves.AllWavesCleared -= Win;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Pause();
        }

        public void Begin()
        {
            if (Current == State.Menu) SetState(State.Playing);
        }

        public void Pause()
        {
            if (Current == State.Playing) SetState(State.Paused);
        }

        public void Resume()
        {
            if (Current == State.Paused) SetState(State.Playing);
        }

        private void Win() => End(State.Won);
        private void Lose() => End(State.Lost);

        private void End(State result)
        {
            if (Current != State.Playing) return;

            int cleared = result == State.Won ? waves.WaveNumber : waves.WaveNumber - 1;
            if (cleared > BestWave)
            {
                PlayerPrefs.SetInt(BestWaveKey, cleared);
                PlayerPrefs.Save();
            }

            SetState(result);
            Ended?.Invoke(result == State.Won);
        }

        public void ContinueEndless()
        {
            if (Current != State.Won) return;
            waves.ContinueEndless();
            SetState(State.Playing);
        }

        public void Restart()
        {
            _skipMenuOnce = true;
            DOTween.KillAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SetState(State state)
        {
            Current = state;
            world.Running = state == State.Playing;
            StateChanged?.Invoke(state);
        }
    }
}
