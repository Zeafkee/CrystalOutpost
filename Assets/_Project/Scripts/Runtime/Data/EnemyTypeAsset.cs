using System;
using CrystalOutpost.Sim;
using UnityEngine;

namespace CrystalOutpost.Data
{
    [CreateAssetMenu(menuName = "CrystalOutpost/Enemy Type", fileName = "Enemy_")]
    public sealed class EnemyTypeAsset : ScriptableObject
    {
        public string displayName = "Enemy";
        public MeshId mesh = MeshId.EnemyMelee;
        [Min(1f)] public float maxHealth = 20f;
        [Min(0.1f)] public float speed = 2.5f;
        [Min(0.1f)] public float radius = 0.4f;
        [Min(0.1f)] public float scale = 1f;

        [Header("Attack")]
        [Min(0f)] public float attackRange = 0.3f;
        [Min(0f)] public float attackDamage = 5f;
        [Min(0.1f)] public float attackInterval = 1f;
        [Min(0f)] public float projectileSpeed;

        [Header("Reward")]
        [Min(0)] public int crystalDrop = 1;
        public Color deathColor = new Color(0.5f, 0.2f, 0.6f);

        public EnemyTypeData ToData() => new EnemyTypeData
        {
            MaxHealth = maxHealth,
            Speed = speed,
            Radius = radius,
            AttackRange = attackRange,
            AttackDamage = attackDamage,
            AttackInterval = attackInterval,
            ProjectileSpeed = projectileSpeed,
            CrystalDrop = crystalDrop,
            MeshId = (int)mesh,
            Scale = scale,
        };
    }
}
