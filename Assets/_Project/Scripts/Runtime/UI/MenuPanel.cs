using CrystalOutpost.Audio;
using CrystalOutpost.World;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalOutpost.UI
{
    public sealed class MenuPanel : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private Button playButton;
        [SerializeField] private RectTransform playRect;

        private void Awake()
        {
            playButton.onClick.AddListener(Play);
            session.StateChanged += HandleState;
        }

        private void OnDestroy()
        {
            if (session != null) session.StateChanged -= HandleState;
        }

        private void HandleState(GameSession.State state)
        {
            bool show = state == GameSession.State.Menu;
            root.SetActive(show);
            if (!show) return;

            int best = session.BestWave;
            if (best > 0) bestLabel.SetText("BEST WAVE {0}", (float)best);
            else bestLabel.text = "DEFEND THE CORE";

            playRect.DOKill();
            playRect.localScale = Vector3.one;
            playRect.DOScale(1.06f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        private void Play()
        {
            AudioManager.Play(Sfx.Click);
            playRect.DOKill();
            group.DOFade(0f, 0.25f).OnComplete(() =>
            {
                group.alpha = 1f;
                session.Begin();
            });
        }
    }
}
