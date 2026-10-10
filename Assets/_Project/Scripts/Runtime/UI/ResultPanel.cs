using CrystalOutpost.World;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalOutpost.UI
{
    public sealed class ResultPanel : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private Joystick joystick;
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform window;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text statsLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button endlessButton;
        [SerializeField] private Color winColor = new Color32(110, 235, 255, 255);
        [SerializeField] private Color loseColor = new Color32(255, 110, 100, 255);

        private void Awake()
        {
            root.SetActive(false);
            session.Ended += Show;
            restartButton.onClick.AddListener(session.Restart);
            endlessButton.onClick.AddListener(ContinueEndless);
        }

        private void OnDestroy()
        {
            if (session != null) session.Ended -= Show;
        }

        private void Show(bool won)
        {
            joystick.enabled = false;

            titleLabel.text = won ? "OUTPOST HELD" : "CORE LOST";
            titleLabel.color = won ? winColor : loseColor;
            statsLabel.SetText("WAVE {0}\n{1} KILLS\nBEST WAVE {2}", (float)session.WaveNumber, (float)session.Kills, (float)session.BestWave);
            endlessButton.gameObject.SetActive(won);

            root.SetActive(true);
            group.alpha = 0f;
            group.DOFade(1f, 0.35f).SetUpdate(true);
            window.localScale = Vector3.one * 0.8f;
            window.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void ContinueEndless()
        {
            group.DOKill();
            window.DOKill();
            root.SetActive(false);
            joystick.enabled = true;
            session.ContinueEndless();
        }
    }
}
