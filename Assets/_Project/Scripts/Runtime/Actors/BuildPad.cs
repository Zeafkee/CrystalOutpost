using System;
using CrystalOutpost.World;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CrystalOutpost.Actors
{
    public enum PadKind
    {
        Structure,
        HeroDamage,
        HeroSpeed,
        RepairCore,
        FortifyCore,
    }

    public sealed class BuildPad : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private PadKind kind;
        [SerializeField] private int[] costs = { 40 };
        [SerializeField] private float endlessGrowth;
        [SerializeField] private float radius = 1.3f;
        [SerializeField] private float drainTime = 1.1f;
        [SerializeField] private float dwellTime = 0.35f;
        [SerializeField] private float pauseAfterPurchase = 2f;

        [Header("What it affects")]
        [SerializeField] private Structure structure;
        [SerializeField] private Turret turret;
        [SerializeField] private Hero hero;
        [SerializeField] private CoreCrystal core;
        [SerializeField] private float amountPerLevel = 0.35f;

        [Header("Visuals")]
        [SerializeField] private GameObject visual;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private string title = "BUILD";
        [SerializeField] private string upgradeTitle = "UPGRADE";
        [SerializeField] private string rebuildTitle = "REBUILD";

        private int _level;
        private int _paid;
        private float _carry;
        private float _dwell;
        private bool _broken;
        private float _baseValue;
        private int _cost;

        public PadKind Kind => kind;
        public Vector3 Position => transform.position;
        public bool HasTurret => turret != null;
        public bool Available => kind == PadKind.RepairCore || _broken || endlessGrowth > 0f || _level < costs.Length;
        public int RemainingCost => Available ? CurrentCost - _paid : 0;
        public bool Useful => kind != PadKind.RepairCore || core.Health < core.MaxHealth;

        private int CurrentCost => _broken ? costs[0] : _cost;

        public bool LastPurchaseBuilt { get; private set; }

        public event Action<BuildPad> Purchased;
        public event Action<BuildPad, int> Drained;

        private void Start()
        {
            if (kind == PadKind.HeroDamage) _baseValue = hero.AttackDamage;
            if (kind == PadKind.HeroSpeed) _baseValue = hero.Speed;
            if (structure != null) structure.Destroyed += HandleStructureDestroyed;
            _cost = CostOfLevel(_level);
            Refresh();
        }

        private void OnDestroy()
        {
            if (structure != null) structure.Destroyed -= HandleStructureDestroyed;
        }

        private void Update()
        {
            if (!world.Running || !Available || !Useful || world.Crystals == 0 || !HeroOnPad())
            {
                _carry = 0f;
                _dwell = 0f;
                return;
            }

            _dwell += Time.deltaTime;
            if (_dwell < dwellTime) return;

            float rate = Mathf.Max(CurrentCost / drainTime, 30f);
            _carry += rate * Time.deltaTime;

            int amount = Mathf.Min((int)_carry, Mathf.Min(CurrentCost - _paid, world.Crystals));
            if (amount <= 0 || !world.TrySpend(amount)) return;

            _carry -= amount;
            _paid += amount;
            Drained?.Invoke(this, amount);
            if (_paid >= CurrentCost) Complete();
            else costLabel.SetText("{0}", (float)(CurrentCost - _paid));
        }

        private int CostOfLevel(int level)
        {
            int last = costs.Length - 1;
            if (level <= last || kind == PadKind.RepairCore || endlessGrowth <= 0f) return costs[Mathf.Min(level, last)];
            float cost = costs[last] * Mathf.Pow(endlessGrowth, level - last);
            return Mathf.RoundToInt(cost / 5f) * 5;
        }

        private bool HeroOnPad()
        {
            if (!world.HeroAlive) return false;
            Vector3 delta = world.HeroPosition - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude < radius * radius;
        }

        private void Complete()
        {
            _paid = 0;
            _carry = 0f;
            _dwell = dwellTime - pauseAfterPurchase;
            bool build = _broken || _level == 0;
            LastPurchaseBuilt = build && kind == PadKind.Structure;
            if (!_broken) _level++;
            _broken = false;
            _cost = CostOfLevel(_level);

            switch (kind)
            {
                case PadKind.Structure:
                    if (build)
                    {
                        structure.gameObject.SetActive(true);
                        Punch(structure.transform);
                    }

                    if (turret != null) turret.SetLevel(_level);
                    break;

                case PadKind.HeroDamage:
                    hero.AttackDamage = _baseValue * (1f + amountPerLevel * _level);
                    break;

                case PadKind.HeroSpeed:
                    hero.Speed = _baseValue * (1f + amountPerLevel * _level);
                    break;

                case PadKind.RepairCore:
                    core.Repair(amountPerLevel);
                    break;

                case PadKind.FortifyCore:
                    core.Fortify(amountPerLevel);
                    break;
            }

            Refresh();
            if (visual.activeSelf) Punch(visual.transform);
            Purchased?.Invoke(this);
        }

        private void HandleStructureDestroyed(Structure destroyed)
        {
            _broken = true;
            _paid = 0;
            Refresh();
        }

        private void Refresh()
        {
            visual.SetActive(Available);
            if (!Available) return;

            titleLabel.text = _broken ? rebuildTitle : _level == 0 || kind == PadKind.RepairCore ? title : upgradeTitle;
            costLabel.SetText("{0}", (float)(CurrentCost - _paid));
        }

        private static void Punch(Transform target)
        {
            target.DOKill(true);
            target.DOPunchScale(Vector3.one * 0.25f, 0.3f, 5, 0.6f);
        }
    }
}
