using System.IO;
using CrystalOutpost.Data;
using UnityEditor;
using UnityEngine;

namespace CrystalOutpost.EditorTools
{
    internal static class MeshFactory
    {
        public const string Folder = "Assets/_Project/Art/Meshes";

        private static readonly Color32 Steel = new Color32(92, 104, 140, 255);
        private static readonly Color32 SteelDark = new Color32(52, 58, 86, 255);
        private static readonly Color32 Cloth = new Color32(48, 92, 160, 255);
        private static readonly Color32 Skin = new Color32(224, 176, 140, 255);
        private static readonly Color32 Cyan = new Color32(90, 235, 255, 255);
        private static readonly Color32 HuskBody = new Color32(74, 52, 96, 255);
        private static readonly Color32 HuskDark = new Color32(44, 30, 60, 255);
        private static readonly Color32 EyeRed = new Color32(255, 70, 60, 255);
        private static readonly Color32 SpitterBody = new Color32(60, 78, 70, 255);
        private static readonly Color32 SpitterDark = new Color32(36, 48, 46, 255);
        private static readonly Color32 Magenta = new Color32(255, 80, 220, 255);
        private static readonly Color32 Stone = new Color32(70, 72, 96, 255);
        private static readonly Color32 StoneDark = new Color32(44, 46, 66, 255);
        private static readonly Color32 Wood = new Color32(96, 66, 52, 255);
        private static readonly Color32 Gold = new Color32(255, 200, 80, 255);
        private static readonly Color32 Orange = new Color32(255, 140, 50, 255);

        public static Mesh Save(Mesh mesh)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            string path = $"{Folder}/{mesh.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            existing.name = mesh.name;
            existing.UploadMeshData(false);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        public const string ModelFolder = "Assets/_Project/Art/Models";

        public static Mesh Load(string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelFolder}/{name}.lpmesh");
            return model != null ? model : AssetDatabase.LoadAssetAtPath<Mesh>($"{Folder}/{name}.asset");
        }

        public static Mesh[] BuildAll()
        {
            Save(Hero());
            Save(Core());
            Save(CoreCrystal());
            Save(TurretBase());
            Save(TurretHead());
            Save(Rock());
            Save(CrystalVein());
            Save(DeadTree());

            var library = new Mesh[System.Enum.GetValues(typeof(MeshId)).Length];
            library[(int)MeshId.EnemyMelee] = Save(Husk("EnemyMelee", 1f));
            library[(int)MeshId.EnemyRanged] = Save(Spitter());
            library[(int)MeshId.EnemyBoss] = Save(Husk("EnemyBoss", 1f, true));
            library[(int)MeshId.Crystal] = Save(Pickup());
            library[(int)MeshId.Bolt] = Save(Bolt());
            library[(int)MeshId.Shell] = Save(Shell());
            library[(int)MeshId.EnemyOrb] = Save(Orb());
            library[(int)MeshId.Quad] = Save(GroundQuad());
            AssetDatabase.SaveAssets();

            foreach (MeshId id in System.Enum.GetValues(typeof(MeshId)))
            {
                var model = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelFolder}/{id}.lpmesh");
                if (model != null) library[(int)id] = model;
            }

            return library;
        }

        private static Mesh Hero()
        {
            var b = new MeshBuilder();
            Vector3 hipL = new Vector3(-0.17f, 0.72f, 0f), hipR = new Vector3(0.17f, 0.72f, 0f);
            Vector3 shoulderL = new Vector3(-0.42f, 1.32f, 0f), shoulderR = new Vector3(0.42f, 1.32f, 0f);

            b.Paint(SteelDark).Limb(MeshBuilder.Leg, -1f, hipL).Box(new Vector3(-0.17f, 0.36f, 0f), new Vector3(0.24f, 0.72f, 0.28f));
            b.Paint(Steel).Box(new Vector3(-0.17f, 0.08f, 0.05f), new Vector3(0.27f, 0.16f, 0.4f));
            b.Paint(SteelDark).Limb(MeshBuilder.Leg, 1f, hipR).Box(new Vector3(0.17f, 0.36f, 0f), new Vector3(0.24f, 0.72f, 0.28f));
            b.Paint(Steel).Box(new Vector3(0.17f, 0.08f, 0.05f), new Vector3(0.27f, 0.16f, 0.4f));

            b.NoLimb().Paint(Cloth).Box(new Vector3(0f, 1.05f, 0f), new Vector3(0.62f, 0.66f, 0.38f));
            b.Paint(Steel).Box(new Vector3(0f, 0.74f, 0f), new Vector3(0.66f, 0.12f, 0.42f));
            b.Paint(Cyan, 1f).Box(new Vector3(0f, 1.14f, 0.2f), new Vector3(0.18f, 0.18f, 0.04f));
            b.Paint(new Color32(36, 60, 120, 255)).Box(new Vector3(0f, 0.98f, -0.23f), new Vector3(0.56f, 0.8f, 0.06f));

            b.Paint(Skin).Box(new Vector3(0f, 1.6f, 0f), new Vector3(0.4f, 0.38f, 0.38f));
            b.Paint(Steel).Box(new Vector3(0f, 1.74f, -0.02f), new Vector3(0.46f, 0.2f, 0.46f));
            b.Paint(Cyan, 0.9f).Box(new Vector3(0f, 1.6f, 0.2f), new Vector3(0.32f, 0.08f, 0.03f));

            b.Paint(Steel).Limb(MeshBuilder.Arm, -1f, shoulderL).Box(new Vector3(-0.42f, 1.34f, 0f), new Vector3(0.26f, 0.22f, 0.34f));
            b.Paint(Cloth).Box(new Vector3(-0.42f, 1.0f, 0f), new Vector3(0.18f, 0.56f, 0.2f));
            b.Paint(Steel).Limb(MeshBuilder.Arm, 1f, shoulderR).Box(new Vector3(0.42f, 1.34f, 0f), new Vector3(0.26f, 0.22f, 0.34f));
            b.Paint(Cloth).Box(new Vector3(0.42f, 1.0f, 0f), new Vector3(0.18f, 0.56f, 0.2f));
            b.Paint(Gold).Box(new Vector3(0.42f, 0.72f, 0.12f), new Vector3(0.1f, 0.1f, 0.34f));
            b.Paint(Cyan, 1f).Box(new Vector3(0.42f, 0.72f, 0.78f), new Vector3(0.08f, 0.16f, 1.0f));
            return b.Build("Hero");
        }

        private static Mesh Husk(string name, float size, bool boss = false)
        {
            var b = new MeshBuilder();
            Vector3 hipL = new Vector3(-0.2f, 0.55f, 0f), hipR = new Vector3(0.2f, 0.55f, 0f);
            Vector3 shoulderL = new Vector3(-0.5f, 1.05f, 0.05f), shoulderR = new Vector3(0.5f, 1.05f, 0.05f);

            b.Paint(HuskDark).Limb(MeshBuilder.Leg, -1f, hipL).Box(new Vector3(-0.2f, 0.28f, 0f), new Vector3(0.26f, 0.56f, 0.3f));
            b.Limb(MeshBuilder.Leg, 1f, hipR).Box(new Vector3(0.2f, 0.28f, 0f), new Vector3(0.26f, 0.56f, 0.3f));

            Quaternion lean = Quaternion.Euler(18f, 0f, 0f);
            b.NoLimb().Paint(HuskBody).Box(new Vector3(0f, 0.92f, 0.06f), new Vector3(0.82f, 0.78f, 0.6f), lean);
            b.Paint(HuskDark).Box(new Vector3(0f, 1.22f, -0.12f), new Vector3(0.6f, 0.3f, 0.5f), lean);
            b.Paint(HuskBody).Box(new Vector3(0f, 1.2f, 0.42f), new Vector3(0.44f, 0.38f, 0.4f));
            b.Paint(EyeRed, 1f).Box(new Vector3(-0.11f, 1.24f, 0.63f), new Vector3(0.1f, 0.08f, 0.03f));
            b.Box(new Vector3(0.11f, 1.24f, 0.63f), new Vector3(0.1f, 0.08f, 0.03f));
            b.Paint(HuskDark);
            for (int i = 0; i < 3; i++)
                b.Prism(new Vector3(0f, 1.3f - i * 0.02f, -0.05f - i * 0.2f), 0.26f, 0.09f, 0f, 4, Quaternion.Euler(-25f, 0f, 0f));

            b.Paint(HuskBody).Limb(MeshBuilder.Arm, -1f, shoulderL).Box(new Vector3(-0.52f, 0.72f, 0.12f), new Vector3(0.22f, 0.74f, 0.24f));
            b.Paint(EyeRed, 0.35f).Box(new Vector3(-0.52f, 0.3f, 0.26f), new Vector3(0.26f, 0.12f, 0.34f));
            b.Paint(HuskBody).Limb(MeshBuilder.Arm, 1f, shoulderR).Box(new Vector3(0.52f, 0.72f, 0.12f), new Vector3(0.22f, 0.74f, 0.24f));
            b.Paint(EyeRed, 0.35f).Box(new Vector3(0.52f, 0.3f, 0.26f), new Vector3(0.26f, 0.12f, 0.34f));

            if (boss)
            {
                b.NoLimb().Paint(Gold, 0.25f);
                b.Prism(new Vector3(-0.2f, 1.36f, 0.42f), 0.42f, 0.08f, 0f, 4, Quaternion.Euler(0f, 0f, 28f));
                b.Prism(new Vector3(0.2f, 1.36f, 0.42f), 0.42f, 0.08f, 0f, 4, Quaternion.Euler(0f, 0f, -28f));
                b.Paint(StoneDark).Box(new Vector3(0f, 1.0f, 0.36f), new Vector3(0.7f, 0.3f, 0.08f));
                b.Paint(EyeRed, 1f).Box(new Vector3(0f, 1.0f, 0.41f), new Vector3(0.2f, 0.12f, 0.03f));
            }

            return b.Build(name);
        }

        private static Mesh Spitter()
        {
            var b = new MeshBuilder();
            Vector3 hipL = new Vector3(-0.13f, 0.7f, 0f), hipR = new Vector3(0.13f, 0.7f, 0f);
            Vector3 shoulderL = new Vector3(-0.32f, 1.36f, 0f), shoulderR = new Vector3(0.32f, 1.36f, 0f);

            b.Paint(SpitterDark).Limb(MeshBuilder.Leg, -1f, hipL).Box(new Vector3(-0.13f, 0.35f, 0f), new Vector3(0.16f, 0.7f, 0.18f));
            b.Limb(MeshBuilder.Leg, 1f, hipR).Box(new Vector3(0.13f, 0.35f, 0f), new Vector3(0.16f, 0.7f, 0.18f));

            b.NoLimb().Paint(SpitterBody).Prism(new Vector3(0f, 0.55f, 0f), 0.95f, 0.36f, 0.22f, 6);
            b.Paint(SpitterDark).Prism(new Vector3(0f, 1.46f, 0f), 0.42f, 0.26f, 0.06f, 6);
            b.Paint(Magenta, 1f).Box(new Vector3(0f, 1.58f, 0.2f), new Vector3(0.22f, 0.06f, 0.03f));

            b.Paint(SpitterBody).Limb(MeshBuilder.Arm, -1f, shoulderL).Box(new Vector3(-0.32f, 1.08f, 0f), new Vector3(0.12f, 0.58f, 0.14f));
            b.Limb(MeshBuilder.Arm, 1f, shoulderR).Box(new Vector3(0.32f, 1.08f, 0f), new Vector3(0.12f, 0.58f, 0.14f));
            b.Paint(Wood).Box(new Vector3(0.32f, 1.0f, 0.2f), new Vector3(0.06f, 1.5f, 0.06f));
            b.Paint(Magenta, 1f).Crystal(new Vector3(0.32f, 1.74f, 0.2f), 0.36f, 0.14f, 4, Quaternion.identity, 0.5f);
            return b.Build("EnemyRanged");
        }

        private static Mesh Pickup()
        {
            var b = new MeshBuilder();
            b.Paint(Cyan, 0.9f).Crystal(new Vector3(0f, -0.22f, 0f), 0.5f, 0.17f, 5, Quaternion.identity, 0.45f);
            return b.Build("Crystal");
        }

        private static Mesh Bolt()
        {
            var b = new MeshBuilder();
            b.Paint(Gold, 1f).Box(new Vector3(0f, 0f, 0f), new Vector3(0.1f, 0.1f, 0.9f));
            b.Paint(Orange, 1f).Prism(new Vector3(0f, 0f, 0.45f), 0.24f, 0.11f, 0f, 4, Quaternion.Euler(90f, 0f, 0f));
            return b.Build("Bolt");
        }

        private static Mesh Shell()
        {
            var b = new MeshBuilder();
            b.Paint(Cyan, 1f).Crystal(new Vector3(0f, 0f, -0.32f), 0.64f, 0.24f, 6, Quaternion.Euler(90f, 0f, 0f), 0.5f);
            return b.Build("Shell");
        }

        private static Mesh Orb()
        {
            var b = new MeshBuilder();
            b.Paint(Magenta, 1f).Crystal(new Vector3(0f, -0.2f, 0f), 0.4f, 0.2f, 4, Quaternion.identity, 0.5f);
            return b.Build("EnemyOrb");
        }

        private static Mesh GroundQuad()
        {
            var mesh = new Mesh { name = "Quad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f),
            });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
            mesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Core()
        {
            var b = new MeshBuilder();
            b.Paint(StoneDark).Prism(new Vector3(0f, 0f, 0f), 0.35f, 1.9f, 1.7f, 8);
            b.Paint(Stone).Prism(new Vector3(0f, 0.35f, 0f), 0.45f, 1.45f, 1.15f, 8);
            b.Paint(Cyan, 0.8f).Prism(new Vector3(0f, 0.8f, 0f), 0.08f, 1.0f, 1.0f, 8);
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a) * 1.25f, 0.35f, Mathf.Sin(a) * 1.25f);
                b.Paint(Stone).Prism(p, 1.1f, 0.2f, 0.12f, 4);
                b.Paint(Cyan, 1f).Crystal(p + Vector3.up * 1.1f, 0.4f, 0.13f, 4, Quaternion.identity, 0.5f);
            }

            return b.Build("Core");
        }

        private static Mesh CoreCrystal()
        {
            var b = new MeshBuilder();
            b.Paint(Cyan, 1f).Crystal(new Vector3(0f, -0.9f, 0f), 2.1f, 0.62f, 6, Quaternion.identity, 0.4f);
            b.Paint(new Color32(190, 250, 255, 255), 1f).Crystal(new Vector3(0.42f, -0.5f, 0.1f), 0.9f, 0.2f, 5, Quaternion.Euler(0f, 0f, -18f));
            b.Crystal(new Vector3(-0.38f, -0.4f, -0.14f), 0.8f, 0.18f, 5, Quaternion.Euler(0f, 0f, 20f));
            return b.Build("CoreCrystal");
        }

        private static Mesh TurretBase()
        {
            var b = new MeshBuilder();
            b.Paint(StoneDark).Prism(new Vector3(0f, 0f, 0f), 0.3f, 0.85f, 0.7f, 6);
            b.Paint(Wood).Prism(new Vector3(0f, 0.3f, 0f), 0.9f, 0.5f, 0.38f, 6);
            b.Paint(Steel).Prism(new Vector3(0f, 1.2f, 0f), 0.14f, 0.5f, 0.5f, 6);
            return b.Build("TurretBase");
        }

        private static Mesh TurretHead()
        {
            var b = new MeshBuilder();
            b.Paint(Steel).Box(new Vector3(0f, 0.2f, 0f), new Vector3(0.52f, 0.32f, 0.7f));
            b.Paint(SteelDark).Box(new Vector3(0f, 0.2f, 0.62f), new Vector3(0.2f, 0.18f, 0.9f));
            b.Paint(Wood).Box(new Vector3(0f, 0.2f, 0.5f), new Vector3(1.3f, 0.1f, 0.12f));
            b.Paint(Cyan, 1f).Box(new Vector3(0f, 0.4f, -0.1f), new Vector3(0.2f, 0.1f, 0.3f));
            b.Paint(Gold, 1f).Box(new Vector3(0f, 0.2f, 1.1f), new Vector3(0.1f, 0.1f, 0.1f));
            return b.Build("TurretHead");
        }

        private static Mesh Rock()
        {
            var b = new MeshBuilder();
            b.Paint(Stone).Prism(new Vector3(0f, 0f, 0f), 0.7f, 0.85f, 0.5f, 5, Quaternion.Euler(0f, 0f, 6f), 0.3f);
            b.Paint(StoneDark).Prism(new Vector3(0.55f, 0f, 0.3f), 0.42f, 0.5f, 0.28f, 5, Quaternion.Euler(0f, 0f, -8f), 1.1f);
            b.Paint(Stone).Prism(new Vector3(-0.5f, 0f, -0.35f), 0.3f, 0.36f, 0.2f, 4, Quaternion.identity, 0.6f);
            return b.Build("Rock");
        }

        private static Mesh CrystalVein()
        {
            var b = new MeshBuilder();
            b.Paint(StoneDark).Prism(new Vector3(0f, 0f, 0f), 0.25f, 0.95f, 0.7f, 6);
            b.Paint(Cyan, 0.85f).Crystal(new Vector3(0f, 0.2f, 0f), 1.5f, 0.3f, 5, Quaternion.identity);
            b.Crystal(new Vector3(0.38f, 0.2f, 0.12f), 0.95f, 0.2f, 5, Quaternion.Euler(0f, 0f, -24f));
            b.Crystal(new Vector3(-0.34f, 0.2f, -0.16f), 1.05f, 0.22f, 5, Quaternion.Euler(10f, 0f, 22f));
            b.Crystal(new Vector3(0.05f, 0.2f, 0.42f), 0.7f, 0.16f, 4, Quaternion.Euler(26f, 0f, 0f));
            return b.Build("CrystalVein");
        }

        private static Mesh DeadTree()
        {
            var b = new MeshBuilder();
            Color32 bark = new Color32(58, 46, 60, 255);
            b.Paint(bark).Prism(new Vector3(0f, 0f, 0f), 2.2f, 0.26f, 0.12f, 5);
            b.Prism(new Vector3(0f, 1.3f, 0f), 1.1f, 0.1f, 0.03f, 4, Quaternion.Euler(0f, 0f, 48f));
            b.Prism(new Vector3(0f, 1.6f, 0f), 0.9f, 0.09f, 0.03f, 4, Quaternion.Euler(0f, 0f, -42f));
            b.Prism(new Vector3(0f, 2.1f, 0f), 0.8f, 0.1f, 0.02f, 4, Quaternion.Euler(20f, 0f, 10f));
            return b.Build("DeadTree");
        }

        public static Mesh Ground(int width, int height, float cellSize, Vector2 origin, System.Func<Vector2, float> roadness)
        {
            var b = new MeshBuilder();
            Color baseColor = new Color32(40, 44, 70, 255);
            Color roadColor = new Color32(96, 84, 96, 255);
            Vector2 size = new Vector2(width, height) * cellSize;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 center = origin + new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                    float road = Mathf.Clamp01(roadness(center));
                    float noise = Mathf.PerlinNoise(x * 0.21f + 11f, y * 0.21f + 5f) * 0.16f;
                    Color color = Color.Lerp(baseColor * (0.82f + noise), roadColor * (0.9f + noise * 0.4f), road);

                    Vector2 uv = (center - origin) / size;
                    float edge = Mathf.Min(Mathf.Min(uv.x, 1f - uv.x) * size.x, Mathf.Min(uv.y, 1f - uv.y) * size.y);
                    color *= Mathf.Clamp01(edge / 5f) * 0.85f + 0.15f;
                    color.a = 0f;

                    Vector3 min = new Vector3(origin.x + x * cellSize, 0f, origin.y + y * cellSize);
                    b.Paint(color);
                    b.Quad(min, min + new Vector3(0f, 0f, cellSize), min + new Vector3(cellSize, 0f, cellSize), min + new Vector3(cellSize, 0f, 0f));
                }
            }

            b.Paint(new Color32(6, 6, 12, 255));
            float m = 60f;
            Vector3 a0 = new Vector3(origin.x - m, -0.02f, origin.y - m);
            Vector3 a1 = new Vector3(origin.x - m, -0.02f, origin.y + size.y + m);
            Vector3 a2 = new Vector3(origin.x + size.x + m, -0.02f, origin.y + size.y + m);
            Vector3 a3 = new Vector3(origin.x + size.x + m, -0.02f, origin.y - m);
            b.Quad(a0, a1, a2, a3);
            return b.Build("Ground");
        }
    }
}
