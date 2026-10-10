using CrystalOutpost.Actors;
using CrystalOutpost.World;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalOutpost.UI
{
    public sealed class Hud : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private WaveSpawner waves;
        [SerializeField] private CoreCrystal core;
        [SerializeField] private Hero hero;

        [SerializeField] private TMP_Text crystalLabel;
        [SerializeField] private RectTransform crystalIcon;
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private TMP_Text enemyLabel;
        [SerializeField] private Image coreFill;
        [SerializeField] private Image heroFill;
        [SerializeField] private TMP_Text waveBanner;

        private int _shownEnemies = -1;
        private int _shownCountdown = -1;
        private int _shownWave = -1;

        private void Start()
        {
            world.CrystalsChanged += SetCrystals;
            core.HealthChanged += SetCoreHealth;
            hero.HealthChanged += SetHeroHealth;

            waves.WaveStarted += ShowWaveBanner;

            crystalLabel.SetText("{0}", (float)world.Crystals);
            coreFill.fillAmount = 1f;
            heroFill.fillAmount = 1f;
            waveBanner.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (waves != null) waves.WaveStarted -= ShowWaveBanner;
            if (world != null) world.CrystalsChanged -= SetCrystals;
            if (core != null) core.HealthChanged -= SetCoreHealth;
            if (hero != null) hero.HealthChanged -= SetHeroHealth;
        }

        private void Update()
        {
            if (world.Sim == null) return;

            int countdown = Mathf.CeilToInt(waves.PrepareTimeLeft);
            int wave = waves.WaveNumber;
            if (countdown != _shownCountdown || wave != _shownWave)
            {
                _shownCountdown = countdown;
                _shownWave = wave;
                if (waves.Finished) waveLabel.text = "ALL WAVES CLEARED";
                else if (countdown > 0) waveLabel.SetText("WAVE {0} IN {1}", (float)(wave + 1), (float)countdown);
                else if (waves.Endless) waveLabel.SetText("ENDLESS  WAVE {0}", (float)wave);
                else waveLabel.SetText("WAVE {0} / {1}", (float)wave, (float)waves.WaveCount);
            }

            int enemies = world.Sim.Horde.Count;
            if (enemies != _shownEnemies)
            {
                _shownEnemies = enemies;
                enemyLabel.SetText("{0}", (float)enemies);
            }
        }

        private void SetCrystals(int amount)
        {
            crystalLabel.SetText("{0}", (float)amount);
            if (crystalIcon == null) return;
            crystalIcon.DOKill();
            crystalIcon.localScale = Vector3.one;
            crystalIcon.DOPunchScale(Vector3.one * 0.2f, 0.15f, 4, 0.5f);
        }

        private void ShowWaveBanner(int wave)
        {
            if (wave == waves.WaveCount) waveBanner.text = "FINAL WAVE";
            else waveBanner.SetText("WAVE {0}", (float)wave);

            waveBanner.gameObject.SetActive(true);
            waveBanner.DOKill();
            waveBanner.alpha = 0f;
            waveBanner.rectTransform.localScale = Vector3.one * 1.4f;
            DOTween.Sequence().SetTarget(waveBanner)
                .Append(waveBanner.DOFade(1f, 0.25f))
                .Join(waveBanner.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutCubic))
                .AppendInterval(1.2f)
                .Append(waveBanner.DOFade(0f, 0.4f))
                .OnComplete(() => waveBanner.gameObject.SetActive(false));
        }

        private void SetCoreHealth(float health, float max) => coreFill.fillAmount = health / max;
        private void SetHeroHealth(float health, float max) => heroFill.fillAmount = health / max;
    }
}
