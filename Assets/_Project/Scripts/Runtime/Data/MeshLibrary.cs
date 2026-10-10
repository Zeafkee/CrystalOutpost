using System;
using CrystalOutpost.Sim;
using UnityEngine;

namespace CrystalOutpost.Data
{
    [CreateAssetMenu(menuName = "CrystalOutpost/Mesh Library", fileName = "MeshLibrary")]
    public sealed class MeshLibrary : ScriptableObject
    {
        public Mesh[] meshes = new Mesh[0];
        public Material toon;
        public Material shadow;
        public Material glow;

        public Mesh Get(MeshId id) => meshes[(int)id];
    }
}
