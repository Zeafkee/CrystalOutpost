using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace CrystalOutpost.EditorTools
{
    [ScriptedImporter(1, "lpmesh")]
    public sealed class LowPolyMeshImporter : ScriptedImporter
    {
        [Serializable]
        private sealed class Data
        {
            public string name;
            public float[] positions;
            public float[] normals;
            public float[] colors;
            public float[] limbs;
            public float[] pivots;
            public int[] triangles;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var data = JsonUtility.FromJson<Data>(File.ReadAllText(ctx.assetPath));
            int count = data.positions.Length / 3;

            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var colors = new Color[count];
            var limbs = new Vector2[count];
            var pivots = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3(data.positions[i * 3], data.positions[i * 3 + 1], data.positions[i * 3 + 2]);
                normals[i] = new Vector3(data.normals[i * 3], data.normals[i * 3 + 1], data.normals[i * 3 + 2]);
                pivots[i] = new Vector3(data.pivots[i * 3], data.pivots[i * 3 + 1], data.pivots[i * 3 + 2]);
                limbs[i] = new Vector2(data.limbs[i * 2], data.limbs[i * 2 + 1]);

                Color srgb = new Color(data.colors[i * 4], data.colors[i * 4 + 1], data.colors[i * 4 + 2], 1f);
                Color linear = srgb.linear;
                colors[i] = new Color(linear.r, linear.g, linear.b, data.colors[i * 4 + 3]);
            }

            var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(ctx.assetPath) };
            if (count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, limbs);
            mesh.SetUVs(1, pivots);
            mesh.SetTriangles(data.triangles, 0);
            mesh.RecalculateBounds();

            Bounds bounds = mesh.bounds;
            bounds.Expand(0.6f);
            mesh.bounds = bounds;

            ctx.AddObjectToAsset("mesh", mesh);
            ctx.SetMainObject(mesh);
        }
    }
}
