using CrystalOutpost.Audio;
using CrystalOutpost.World;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalOutpost.UI
{
    public sealed class PausePanel : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private Joystick joystick;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private TMP_Text soundLabel;
        [SerializeField] private TMP_Text musicLabel;

        private void Awake()
        {
            root.SetActive(false);
            pauseButton.onClick.AddListener(() => Click(session.Pause));
            resumeButton.onClick.AddListener(() => Click(session.Resume));
            restartButton.onClick.AddListener(() => Click(session.Restart));
            soundButton.onClick.AddListener(() => Click(() => GameSettings.SoundOn = !GameSettings.SoundOn));
            musicButton.onClick.AddListener(() => Click(() => GameSettings.MusicOn = !GameSettings.MusicOn));
            session.StateChanged += HandleState;
            GameSettings.Changed += RefreshLabels;
        }

        private void OnDestroy()
        {
            if (session != null) session.StateChanged -= HandleState;
            GameSettings.Changed -= RefreshLabels;
        }

        private static void Click(System.Action action)
        {
            AudioManager.Play(Sfx.Click);
            action();
        }

        private void HandleState(GameSession.State state)
        {
            bool playing = state == GameSession.State.Playing;
            joystick.enabled = playing;
            pauseButton.gameObject.SetActive(playing);

            bool paused = state == GameSession.State.Paused;
            if (paused == root.activeSelf) return;
            root.SetActive(paused);
            if (!paused) return;

            RefreshLabels();
            window.DOKill();
            window.localScale = Vector3.one * 0.85f;
            window.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

        private void RefreshLabels()
        {
            soundLabel.text = GameSettings.SoundOn ? "SOUND: ON" : "SOUND: OFF";
            musicLabel.text = GameSettings.MusicOn ? "MUSIC: ON" : "MUSIC: OFF";
        }
    }
}
