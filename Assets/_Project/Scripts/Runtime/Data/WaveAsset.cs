using System;
using CrystalOutpost.Sim;
using UnityEngine;

namespace CrystalOutpost.Data
{
    [Serializable]
    public struct WaveGroup
    {
        public EnemyTypeAsset type;
        [Min(1)] public int count;
    }

    [CreateAssetMenu(menuName = "CrystalOutpost/Wave", fileName = "Wave_00")]
    public sealed class WaveAsset : ScriptableObject
    {
        [Min(0f)] public float prepareTime = 6f;
        [Min(1f)] public float spawnDuration = 20f;
        public WaveGroup[] groups = new WaveGroup[0];

        public int TotalCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < groups.Length; i++) total += groups[i].count;
                return total;
            }
        }
    }
}
