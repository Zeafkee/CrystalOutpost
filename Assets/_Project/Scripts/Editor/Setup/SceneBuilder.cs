using System.Collections.Generic;
using System.IO;
using CrystalOutpost.Actors;
using CrystalOutpost.Audio;
using CrystalOutpost.Data;
using CrystalOutpost.UI;
using CrystalOutpost.View;
using CrystalOutpost.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrystalOutpost.EditorTools
{
    public static class SceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Main.unity";
        private const string DataFolder = Root + "/Data";
        private const string MaterialFolder = Root + "/Art/Materials";

        private const int MapWidth = 44;
        private const int MapHeight = 60;
        private static readonly Vector2 MapOrigin = new Vector2(-22f, -30f);
        private static readonly Vector3 CorePosition = new Vector3(0f, 0f, -8f);
        private const float RoadHalfWidth = 1.7f;

        private static readonly Vector2[][] Roads =
        {
            new[] { new Vector2(-13f, 28f), new Vector2(-13f, 15f), new Vector2(-6f, 7f), new Vector2(-1f, -1f), new Vector2(0f, -6f) },
            new[] { new Vector2(0f, 29f), new Vector2(4f, 19f), new Vector2(-3f, 10f), new Vector2(0f, 2f), new Vector2(0f, -6f) },
            new[] { new Vector2(13f, 28f), new Vector2(13f, 13f), new Vector2(7f, 5f), new Vector2(1f, -1f), new Vector2(0f, -6f) },
        };

        private static Material _toon;
        private static Material _shadow;
        private static Material _glow;
        private static readonly List<BuildPad> Pads = new List<BuildPad>();
        private static readonly List<Turret> Turrets = new List<Turret>();

        private static TMP_FontAsset _font;
        private static Material _fontOutline;

        [MenuItem("CrystalOutpost/Setup/Build Everything")]
        public static void BuildEverything()
        {
            ApplyProjectSettings();
            Mesh[] meshes = MeshFactory.BuildAll();
            BuildMaterials();
            BuildMeshLibrary(meshes);
            BuildEnemyTypes();
            BuildWaves();
            AssetDatabase.SaveAssets();
            BuildScene();
            AssetDatabase.SaveAssets();
            Debug.Log("CrystalOutpost: setup finished.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material Material(string name, string shaderName)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (shader == null) Debug.LogError($"Shader not found: {shaderName}");

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildMaterials()
        {
            Material toon = Material("Toon", "CrystalOutpost/Toon");
            toon.SetFloat("_EmissionBoost", 1.6f);

            Material shadow = Material("BlobShadow", "CrystalOutpost/Blob");
            shadow.SetFloat("_SrcBlend", (float)BlendMode.One);
            shadow.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            shadow.SetFloat("_Power", 1.2f);
            shadow.SetColor("_Color", new Color(0f, 0f, 0f, 0.45f));
            shadow.renderQueue = 2900;

            Material glow = Material("Glow", "CrystalOutpost/Blob");
            glow.SetFloat("_SrcBlend", (float)BlendMode.One);
            glow.SetFloat("_DstBlend", (float)BlendMode.One);
            glow.SetFloat("_Power", 2f);
            glow.SetColor("_Color", new Color(0.2f, 0.9f, 1f, 0.4f));
            glow.renderQueue = 3000;

            Material sparks = Material("ParticleSparks", "CrystalOutpost/Particle");
            sparks.SetFloat("_SrcBlend", (float)BlendMode.One);
            sparks.SetFloat("_DstBlend", (float)BlendMode.One);
            sparks.SetFloat("_Soft", 1f);
            sparks.SetFloat("_Boost", 1.6f);

            Material debris = Material("ParticleDebris", "CrystalOutpost/Particle");
            debris.SetFloat("_SrcBlend", (float)BlendMode.One);
            debris.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            debris.SetFloat("_Soft", 0f);
            debris.SetFloat("_Boost", 1f);
        }

        private static void BuildMeshLibrary(Mesh[] meshes)
        {
            var library = LoadOrCreate<MeshLibrary>(DataFolder + "/MeshLibrary.asset");
            library.meshes = meshes;
            library.toon = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Toon.mat");
            library.shadow = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/BlobShadow.mat");
            library.glow = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Glow.mat");
            EditorUtility.SetDirty(library);
        }

        private static EnemyTypeAsset EnemyType(string name) =>
            LoadOrCreate<EnemyTypeAsset>($"{DataFolder}/Enemies/Enemy_{name}.asset");

        private static void BuildEnemyTypes()
        {
            EnemyTypeAsset husk = EnemyType("Husk");
            husk.displayName = "Husk";
            husk.mesh = MeshId.EnemyMelee;
            husk.maxHealth = 24f; husk.speed = 2.6f; husk.radius = 0.42f; husk.scale = 1f;
            husk.attackRange = 0.35f; husk.attackDamage = 3f; husk.attackInterval = 1.1f; husk.projectileSpeed = 0f;
            husk.crystalDrop = 1; husk.deathColor = new Color32(120, 70, 150, 255);
            EditorUtility.SetDirty(husk);

            EnemyTypeAsset spitter = EnemyType("Spitter");
            spitter.displayName = "Spitter";
            spitter.mesh = MeshId.EnemyRanged;
            spitter.maxHealth = 18f; spitter.speed = 2.9f; spitter.radius = 0.34f; spitter.scale = 1f;
            spitter.attackRange = 6.5f; spitter.attackDamage = 4f; spitter.attackInterval = 1.8f; spitter.projectileSpeed = 9f;
            spitter.crystalDrop = 2; spitter.deathColor = new Color32(90, 150, 120, 255);
            EditorUtility.SetDirty(spitter);

            EnemyTypeAsset boss = EnemyType("Behemoth");
            boss.displayName = "Behemoth";
            boss.mesh = MeshId.EnemyBoss;
            boss.maxHealth = 1200f; boss.speed = 1.5f; boss.radius = 0.95f; boss.scale = 2.3f;
            boss.attackRange = 0.5f; boss.attackDamage = 45f; boss.attackInterval = 1.5f; boss.projectileSpeed = 0f;
            boss.maxHealth = 700f; boss.scale = 1.7f; boss.radius = 0.75f; boss.attackDamage = 30f;
            boss.crystalDrop = 15; boss.deathColor = new Color32(255, 200, 80, 255);
            EditorUtility.SetDirty(boss);

            EnemyTypeAsset overlord = EnemyType("Overlord");
            overlord.displayName = "Overlord";
            overlord.mesh = MeshId.EnemyBoss;
            overlord.maxHealth = 5000f; overlord.speed = 1.25f; overlord.radius = 1.1f; overlord.scale = 2.7f;
            overlord.attackRange = 0.6f; overlord.attackDamage = 70f; overlord.attackInterval = 1.6f; overlord.projectileSpeed = 0f;
            overlord.crystalDrop = 60; overlord.deathColor = new Color32(255, 120, 60, 255);
            EditorUtility.SetDirty(overlord);
        }

        private static void BuildWaves()
        {
            EnemyTypeAsset husk = EnemyType("Husk");
            EnemyTypeAsset spitter = EnemyType("Spitter");
            EnemyTypeAsset behemoth = EnemyType("Behemoth");
            EnemyTypeAsset overlord = EnemyType("Overlord");

            Wave(1, 5f, 18f, (husk, 20));
            Wave(2, 5f, 22f, (husk, 34));
            Wave(3, 5f, 24f, (husk, 44), (spitter, 6));
            Wave(4, 5f, 26f, (husk, 58), (spitter, 10));
            Wave(5, 5f, 30f, (husk, 70), (spitter, 14), (behemoth, 1));
            Wave(6, 5f, 30f, (husk, 90), (spitter, 20));
            Wave(7, 5f, 32f, (husk, 110), (spitter, 26), (behemoth, 1));
            Wave(8, 5f, 34f, (husk, 130), (spitter, 32), (behemoth, 2));
            Wave(9, 5f, 36f, (husk, 155), (spitter, 40), (behemoth, 2));
            Wave(10, 5f, 40f, (husk, 170), (spitter, 45), (behemoth, 3), (overlord, 1));
        }

        private static void Wave(int number, float prepare, float duration, params (EnemyTypeAsset type, int count)[] groups)
        {
            var wave = LoadOrCreate<WaveAsset>($"{DataFolder}/Waves/Wave_{number:00}.asset");
            wave.prepareTime = prepare;
            wave.spawnDuration = duration;
            wave.groups = new WaveGroup[groups.Length];
            for (int i = 0; i < groups.Length; i++)
                wave.groups[i] = new WaveGroup { type = groups[i].type, count = groups[i].count };
            EditorUtility.SetDirty(wave);
        }

        [MenuItem("CrystalOutpost/Setup/Rebuild Scene Only")]
        public static void BuildScene()
        {
            EnsureFolder(Root + "/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            _toon = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Toon.mat");
            _shadow = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/BlobShadow.mat");
            _glow = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Glow.mat");
            var library = AssetDatabase.LoadAssetAtPath<MeshLibrary>(DataFolder + "/MeshLibrary.asset");
            SetupFont();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            var worldGo = new GameObject("World");
            var world = worldGo.AddComponent<GameWorld>();

            GameObject coreGo = BuildCore(world);
            BuildGround(worldGo.transform);
            BuildScenery(worldGo.transform);

            Hero hero = BuildHero(world, coreGo.transform);
            Camera camera = BuildCamera(hero.transform);
            Transform[] spawns = BuildSpawnPoints(worldGo.transform);
            Pads.Clear();
            Turrets.Clear();
            BuildPads(world, hero, coreGo.GetComponent<CoreCrystal>());

            WireWorld(world, coreGo.transform);

            var systems = new GameObject("Systems");
            var renderer = systems.AddComponent<HordeRenderer>();
            Set(renderer, "world", world);
            Set(renderer, "library", library);

            var waves = systems.AddComponent<WaveSpawner>();
            Set(waves, "world", world);
            Set(waves, "waves", LoadWaves());
            Set(waves, "spawnPoints", spawns);

            var session = systems.AddComponent<GameSession>();
            Set(session, "world", world);
            Set(session, "waves", waves);
            Set(session, "core", coreGo.GetComponent<CoreCrystal>());

            BuildFx(systems.transform, world, hero);
            BuildAudio(systems.transform, world, waves, session, coreGo.GetComponent<CoreCrystal>(), hero);

            BuildEventSystem();
            BuildHud(world, waves, coreGo.GetComponent<CoreCrystal>(), hero, session, out Joystick joystick);
            Set(hero, "joystick", joystick);

            camera.GetComponent<CameraRig>().SnapToTarget();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static Object[] LoadWaves()
        {
            var list = new List<Object>();
            foreach (string guid in AssetDatabase.FindAssets("t:WaveAsset", new[] { DataFolder + "/Waves" }))
                list.Add(AssetDatabase.LoadAssetAtPath<WaveAsset>(AssetDatabase.GUIDToAssetPath(guid)));
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.ToArray();
        }

        private static void WireWorld(GameWorld world, Transform core)
        {
            var so = new SerializedObject(world);
            so.FindProperty("width").intValue = MapWidth;
            so.FindProperty("height").intValue = MapHeight;
            so.FindProperty("cellSize").floatValue = 1f;
            so.FindProperty("origin").vector2Value = MapOrigin;
            so.FindProperty("core").objectReferenceValue = core;
            so.FindProperty("coreRadius").floatValue = 2.1f;

            SerializedProperty paths = so.FindProperty("paths");
            paths.arraySize = Roads.Length;
            for (int r = 0; r < Roads.Length; r++)
            {
                SerializedProperty path = paths.GetArrayElementAtIndex(r);
                path.FindPropertyRelative("halfWidth").floatValue = RoadHalfWidth;
                SerializedProperty points = path.FindPropertyRelative("points");
                points.arraySize = Roads[r].Length;
                for (int i = 0; i < Roads[r].Length; i++)
                    points.GetArrayElementAtIndex(i).vector2Value = Roads[r][i];
            }

            Object[] types =
            {
                EnemyType("Husk"), EnemyType("Spitter"), EnemyType("Behemoth"), EnemyType("Overlord"),
            };
            SerializedProperty enemyTypes = so.FindProperty("enemyTypes");
            enemyTypes.arraySize = types.Length;
            for (int i = 0; i < types.Length; i++)
                enemyTypes.GetArrayElementAtIndex(i).objectReferenceValue = types[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject MeshObject(string name, Transform parent, string meshName, Vector3 position, Material material = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Load(meshName);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material != null ? material : _toon;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        private static void Blob(Transform parent, Material material, float size, Color color, float height = 0.03f)
        {
            GameObject go = MeshObject(material == _glow ? "Glow" : "Shadow", parent, "Quad", new Vector3(0f, height, 0f), material);
            go.transform.localScale = new Vector3(size, 1f, size);
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            go.GetComponent<MeshRenderer>().SetPropertyBlock(block);
            go.AddComponent<BlobColor>().Set(color);
        }

        private static GameObject BuildCore(GameWorld world)
        {
            GameObject core = MeshObject("Core", world.transform, "Core", CorePosition);
            GameObject crystal = MeshObject("Crystal", core.transform, "CoreCrystal", new Vector3(0f, 2.1f, 0f));
            Blob(core.transform, _glow, 12f, new Color(0.1f, 0.55f, 0.9f, 0.3f), 0.05f);

            var component = core.AddComponent<CoreCrystal>();
            Set(component, "world", world);
            Set(component, "crystal", crystal.transform);
            Set(component, "radius", 2.1f);
            return core;
        }

        private static void BuildGround(Transform parent)
        {
            Mesh mesh = MeshFactory.Save(MeshFactory.Ground(MapWidth, MapHeight, 1f, MapOrigin, Roadness));
            var go = new GameObject("Ground");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _toon;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static float Roadness(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (Vector2[] road in Roads)
            {
                for (int i = 0; i + 1 < road.Length; i++)
                {
                    Vector2 a = road[i], b = road[i + 1];
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
                    best = Mathf.Min(best, Vector2.Distance(p, a + (b - a) * t));
                }
            }

            best = Mathf.Min(best, Vector2.Distance(p, new Vector2(CorePosition.x, CorePosition.z)) - 3.2f);
            return 1f - Mathf.InverseLerp(RoadHalfWidth - 0.4f, RoadHalfWidth + 1.0f, best);
        }

        private static void BuildScenery(Transform parent)
        {
            var root = new GameObject("Scenery").transform;
            root.SetParent(parent, false);
            var random = new System.Random(12345);

            string[] meshes = { "Rock", "Rock", "DeadTree", "DeadTree", "CrystalVein" };
            float[] radii = { 0.9f, 0.9f, 0.4f, 0.4f, 0.9f };
            int placed = 0;

            for (int attempt = 0; attempt < 600 && placed < 46; attempt++)
            {
                float x = MapOrigin.x + 2f + (float)random.NextDouble() * (MapWidth - 4f);
                float z = MapOrigin.y + 2f + (float)random.NextDouble() * (MapHeight - 4f);
                var p = new Vector2(x, z);

                if (Roadness(p) > 0.02f) continue;
                if (Vector2.Distance(p, new Vector2(CorePosition.x, CorePosition.z)) < 10f) continue;
                if (z > 24f && Mathf.Abs(x) < 17f) continue;
                if (NearBuildSite(p, 4f)) continue;

                int kind = random.Next(meshes.Length);
                float scale = 0.8f + (float)random.NextDouble() * 0.7f;
                GameObject go = MeshObject(meshes[kind], root, meshes[kind], new Vector3(x, 0f, z));
                go.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * scale;
                go.isStatic = true;

                var obstacle = go.AddComponent<NavObstacle>();
                Set(obstacle, "radius", radii[kind] * scale);

                if (meshes[kind] == "CrystalVein")
                {
                    Blob(go.transform, _glow, 5.5f / scale, new Color(0.1f, 0.6f, 0.9f, 0.22f), 0.05f / scale);
                    go.isStatic = false;
                    var vein = go.AddComponent<CrystalVein>();
                    Set(vein, "world", parent.GetComponent<GameWorld>());
                    Set(vein, "range", radii[kind] * scale + 1.7f);
                }
                else
                    Blob(go.transform, _shadow, 2.6f, new Color(0f, 0f, 0f, 0.4f), 0.03f / scale);

                placed++;
            }
        }

        private static Hero BuildHero(GameWorld world, Transform core)
        {
            GameObject go = MeshObject("Hero", null, "Hero", CorePosition + new Vector3(0f, 0f, -4.5f));
            Blob(go.transform, _shadow, 1.5f, new Color(0f, 0f, 0f, 0.5f));
            Blob(go.transform, _glow, 8f, new Color(0.3f, 0.5f, 1f, 0.14f), 0.045f);

            var respawn = new GameObject("HeroRespawn").transform;
            respawn.SetParent(core, false);
            respawn.localPosition = new Vector3(0f, 0f, -4.5f);

            var hero = go.AddComponent<Hero>();
            Set(hero, "world", world);
            Set(hero, "body", go.GetComponent<MeshRenderer>());
            Set(hero, "respawnPoint", respawn);
            return hero;
        }

        private static Camera BuildCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 1f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(6, 6, 12, 255);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            go.AddComponent<AudioListener>();

            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;

            var rig = go.AddComponent<CameraRig>();
            Set(rig, "cam", camera);
            Set(rig, "target", target);
            return camera;
        }

        private static Transform[] BuildSpawnPoints(Transform parent)
        {
            var root = new GameObject("SpawnPoints").transform;
            root.SetParent(parent, false);
            var points = new Transform[Roads.Length];
            for (int i = 0; i < Roads.Length; i++)
            {
                var point = new GameObject("Spawn" + i).transform;
                point.SetParent(root, false);
                point.position = new Vector3(Roads[i][0].x, 0f, Roads[i][0].y);
                points[i] = point;
            }

            return points;
        }

        private static readonly Vector3[] TurretSites = { new Vector3(-4.5f, 0f, -3.5f), new Vector3(4.5f, 0f, -3.5f) };
        private static readonly Vector3[] CannonSites = { new Vector3(-7.5f, 0f, 1.5f), new Vector3(8.5f, 0f, 1f) };
        private static readonly Vector4[] BarricadeSites =
        {
            new Vector4(-4.4f, 0f, 4.5f, -32f), new Vector4(6.5f, 0f, 4.5f, 45f),
        };
        private static readonly Vector3 PadOffset = new Vector3(0f, 0f, -2.5f);
        private static readonly Vector3 DamagePadPosition = new Vector3(-5f, 0f, -13.5f);
        private static readonly Vector3 RepairPadPosition = new Vector3(5f, 0f, -13.5f);
        private static readonly Vector3 SpeedPadPosition = new Vector3(-2.6f, 0f, -17.5f);
        private static readonly Vector3 FortifyPadPosition = new Vector3(2.6f, 0f, -17.5f);

        private static bool NearBuildSite(Vector2 p, float distance)
        {
            foreach (Vector3 site in TurretSites) if (Near(p, site, distance)) return true;
            foreach (Vector3 site in CannonSites) if (Near(p, site, distance)) return true;
            foreach (Vector4 site in BarricadeSites) if (Near(p, site, distance)) return true;
            return Near(p, DamagePadPosition, distance) || Near(p, RepairPadPosition, distance) || Near(p, SpeedPadPosition, distance) || Near(p, FortifyPadPosition, distance);
        }

        private static bool Near(Vector2 p, Vector3 site, float distance) =>
            Vector2.Distance(p, new Vector2(site.x, site.z)) < distance || Vector2.Distance(p, new Vector2(site.x, site.z + PadOffset.z)) < distance;

        private static void BuildPads(GameWorld world, Hero hero, CoreCrystal core)
        {
            var root = new GameObject("BuildPads").transform;
            root.SetParent(world.transform, false);

            foreach (Vector3 site in TurretSites)
            {
                Turret turret = BuildTurret(world, site);
                BuildPad pad = Pad(root, world, site + PadOffset, PadKind.Structure, "TURRET", "UPGRADE", 30, 50, 90);
                Set(pad, "structure", turret.GetComponent<Structure>());
                Set(pad, "turret", turret);
                Set(pad, "endlessGrowth", 1.5f);
            }

            foreach (Vector3 site in CannonSites)
            {
                Turret cannon = BuildTurret(world, site, cannon: true);
                BuildPad pad = Pad(root, world, site + PadOffset, PadKind.Structure, "CANNON", "UPGRADE", 80, 130, 200);
                Set(pad, "structure", cannon.GetComponent<Structure>());
                Set(pad, "turret", cannon);
                Set(pad, "endlessGrowth", 1.5f);
            }

            foreach (Vector4 site in BarricadeSites)
            {
                var position = new Vector3(site.x, 0f, site.z);
                GameObject wall = MeshObject("Barricade", world.transform, "Barricade", position);
                wall.transform.localRotation = Quaternion.Euler(0f, site.w, 0f);
                Blob(wall.transform, _shadow, 2.4f, new Color(0f, 0f, 0f, 0.4f));

                var structure = wall.AddComponent<Structure>();
                Set(structure, "world", world);
                Set(structure, "maxHealth", 350f);
                Set(structure, "radius", 1.1f);
                Set(structure, "blockRadius", 1f);
                Set(structure, "renderers", new Object[] { wall.GetComponent<MeshRenderer>() });
                wall.SetActive(false);

                BuildPad pad = Pad(root, world, position + PadOffset, PadKind.Structure, "WALL", "WALL", 25);
                Set(pad, "structure", structure);
            }

            BuildPad damage = Pad(root, world, DamagePadPosition, PadKind.HeroDamage, "DAMAGE", "DAMAGE", 40, 80, 160, 300);
            Set(damage, "hero", hero);
            Set(damage, "amountPerLevel", 0.35f);
            Set(damage, "endlessGrowth", 1.5f);

            BuildPad speed = Pad(root, world, SpeedPadPosition, PadKind.HeroSpeed, "SPEED", "SPEED", 35, 70);
            Set(speed, "hero", hero);
            Set(speed, "amountPerLevel", 0.12f);

            BuildPad repair = Pad(root, world, RepairPadPosition, PadKind.RepairCore, "REPAIR", "REPAIR", 60);
            Set(repair, "core", core);
            Set(repair, "amountPerLevel", 200f);

            BuildPad fortify = Pad(root, world, FortifyPadPosition, PadKind.FortifyCore, "FORTIFY", "FORTIFY", 80, 120);
            Set(fortify, "core", core);
            Set(fortify, "amountPerLevel", 400f);
            Set(fortify, "endlessGrowth", 1.4f);
        }

        private static BuildPad Pad(Transform parent, GameWorld world, Vector3 position, PadKind kind, string title, string upgradeTitle, params int[] costs)
        {
            var go = new GameObject("Pad_" + title);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            MeshObject("Square", visual, "BuildPad", Vector3.zero);
            Blob(visual, _glow, 5f, new Color(0.1f, 0.6f, 0.9f, 0.16f), 0.06f);

            var label = new GameObject("Label").transform;
            label.SetParent(visual, false);
            label.localPosition = new Vector3(0f, 0.55f, 0f);
            label.localRotation = Quaternion.Euler(58f, 0f, 0f);

            TMP_Text titleLabel = WorldText("Title", label, title, 4.6f, new Color32(190, 235, 255, 255), new Vector3(0f, 1.3f, 0f));
            TMP_Text costLabel = WorldText("Cost", label, costs[0].ToString(), 8.5f, Color.white, new Vector3(0.38f, 0f, 0f));

            var icon = new GameObject("Icon");
            icon.transform.SetParent(label, false);
            icon.transform.localPosition = new Vector3(-0.85f, 0f, 0f);
            var sprite = icon.AddComponent<SpriteRenderer>();
            sprite.sprite = UiSprite("CrystalIcon");
            if (sprite.sprite != null) icon.transform.localScale = Vector3.one * (0.85f / sprite.sprite.bounds.size.y);

            var pad = go.AddComponent<BuildPad>();
            Pads.Add(pad);
            Set(pad, "world", world);
            Set(pad, "kind", kind);
            Set(pad, "costs", costs);
            Set(pad, "visual", visual.gameObject);
            Set(pad, "titleLabel", titleLabel);
            Set(pad, "costLabel", costLabel);
            Set(pad, "title", title);
            Set(pad, "upgradeTitle", upgradeTitle);
            return pad;
        }

        private static TMP_Text WorldText(string name, Transform parent, string text, float size, Color color, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshPro>();
            label.rectTransform.localPosition = localPosition;
            label.rectTransform.sizeDelta = new Vector2(6f, 1.2f);
            label.font = _font;
            if (_fontOutline != null) label.fontSharedMaterial = _fontOutline;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.richText = false;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return label;
        }

        private static Turret BuildTurret(GameWorld world, Vector3 position, bool cannon = false)
        {
            string baseMesh = cannon ? "CannonBase" : "TurretBase";
            string headMesh = cannon ? "CannonHead" : "TurretHead";
            float headHeight = cannon ? 1.02f : 1.62f;
            Vector3 muzzleLocal = cannon ? new Vector3(0f, 0.42f, 0.8f) : new Vector3(0f, 0.38f, 1.05f);

            GameObject turret = MeshObject(cannon ? "CrystalCannon" : "BoltTurret", world.transform, baseMesh, position);
            GameObject head = MeshObject("Head", turret.transform, headMesh, new Vector3(0f, headHeight, 0f));
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(head.transform, false);
            muzzle.localPosition = muzzleLocal;
            Blob(turret.transform, _shadow, cannon ? 3.2f : 2.8f, new Color(0f, 0f, 0f, 0.5f));

            var component = turret.AddComponent<Turret>();
            Turrets.Add(component);
            Set(component, "world", world);
            Set(component, "head", head.transform);
            Set(component, "muzzle", muzzle);
            if (cannon)
            {
                Set(component, "range", 10f);
                Set(component, "interval", 1.7f);
                Set(component, "damage", 26f);
                Set(component, "shotSpeed", 15f);
                Set(component, "splashRadius", 2.4f);
                Set(component, "shotKind", 1);
            }

            var structure = turret.AddComponent<Structure>();
            Set(structure, "world", world);
            Set(structure, "maxHealth", cannon ? 500f : 400f);
            Set(structure, "radius", cannon ? 1.05f : 0.9f);
            Set(structure, "blockRadius", cannon ? 0.85f : 0.72f);
            Set(structure, "renderers", new Object[] { turret.GetComponent<MeshRenderer>(), head.GetComponent<MeshRenderer>() });
            turret.SetActive(false);

            return component;
        }

        private static GameObject Prop(Transform parent, string mesh, Vector3 position, float yaw, float blockRadius, float shadowSize)
        {
            GameObject go = MeshObject(mesh, parent, mesh, position);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (shadowSize > 0f) Blob(go.transform, _shadow, shadowSize, new Color(0f, 0f, 0f, 0.4f));
            if (blockRadius > 0f) Set(go.AddComponent<NavObstacle>(), "radius", blockRadius);
            return go;
        }

        private static void BuildFx(Transform parent, GameWorld world, Hero hero)
        {
            ParticleSystem debris = Particles("Debris", parent, MaterialFolder + "/ParticleDebris.mat", 600);
            ParticleSystem.MainModule main = debris.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1.6f;
            Shape(debris, ParticleSystemShapeType.Hemisphere, 0.3f);
            Shrink(debris);

            ParticleSystem sparks = Particles("Sparks", parent, MaterialFolder + "/ParticleSparks.mat", 400);
            main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            Shape(sparks, ParticleSystemShapeType.Sphere, 0.4f);
            Shrink(sparks);

            var fx = parent.gameObject.AddComponent<WorldFx>();
            Set(fx, "world", world);
            Set(fx, "hero", hero);
            Set(fx, "debris", debris);
            Set(fx, "sparks", sparks);
            Set(fx, "pads", Pads.ToArray());
        }

        private static ParticleSystem Particles(string name, Transform parent, string materialPath, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        private static void Shape(ParticleSystem system, ParticleSystemShapeType type, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = type;
            shape.radius = radius;
            shape.rotation = new Vector3(-90f, 0f, 0f);
        }

        private static void Shrink(ParticleSystem system)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        private static void SetupFont()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (_font == null)
            {
                Debug.LogError("TMP font missing: extract 'TMP Essential Resources' first.");
                return;
            }

            string path = MaterialFolder + "/UIText_Outline.mat";
            _fontOutline = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (_fontOutline == null)
            {
                _fontOutline = new Material(_font.material) { name = "UIText_Outline" };
                AssetDatabase.CreateAsset(_fontOutline, path);
            }

            _fontOutline.shader = _font.material.shader;
            _fontOutline.CopyPropertiesFromMaterial(_font.material);
            _fontOutline.SetTexture(ShaderUtilities.ID_MainTex, _font.atlasTexture);
            _fontOutline.EnableKeyword("OUTLINE_ON");
            _fontOutline.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            _fontOutline.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            _fontOutline.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(10, 10, 30, 255));
            EditorUtility.SetDirty(_fontOutline);
        }

        private static Sprite UiSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/UI/{name}.png");

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Image(string name, Transform parent, string sprite, Color color)
        {
            RectTransform rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? UiSprite(sprite) : null;
            image.color = color;
            image.raycastTarget = false;
            if (image.sprite != null && image.sprite.border.sqrMagnitude > 0f) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            RectTransform rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            if (_fontOutline != null) label.fontSharedMaterial = _fontOutline;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.richText = false;
            return label;
        }

        private static Image Bar(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color fillColor, out Image fill)
        {
            Image back = Image(name, parent, "Pill_9s", new Color(0.02f, 0.02f, 0.08f, 0.75f));
            Place(back.rectTransform, anchor, new Vector2(0.5f, 1f), position, size);

            fill = Image("Fill", back.transform, "Pill_9s", fillColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(5f, 5f);
            fill.rectTransform.offsetMax = new Vector2(-5f, -5f);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            back.pixelsPerUnitMultiplier = 64f / size.y;
            fill.pixelsPerUnitMultiplier = 64f / (size.y - 10f);
            return back;
        }

        private static void BuildAudio(Transform parent, GameWorld world, WaveSpawner waves, GameSession session, CoreCrystal core, Hero hero)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);

            var effects = new List<SfxEntry>
            {
                Effect(Sfx.Shoot, 0.35f, 0.06f, 0.08f),
                Effect(Sfx.Turret, 0.3f, 0.08f, 0.1f),
                Effect(Sfx.Cannon, 0.55f, 0.15f, 0.06f),
                Effect(Sfx.EnemyDie, 0.45f, 0.05f, 0.15f),
                Effect(Sfx.Pickup, 0.35f, 0.05f, 0.12f),
                Effect(Sfx.Drain, 0.3f, 0.04f, 0.05f),
                Effect(Sfx.Build, 0.8f, 0.1f, 0f),
                Effect(Sfx.Upgrade, 0.7f, 0.1f, 0f),
                Effect(Sfx.WaveStart, 0.8f, 1f, 0f),
                Effect(Sfx.CoreHit, 0.5f, 0.3f, 0.1f),
                Effect(Sfx.Click, 0.6f, 0.05f, 0f),
                Effect(Sfx.Win, 0.9f, 1f, 0f),
                Effect(Sfx.Lose, 0.9f, 1f, 0f),
            };

            var manager = go.AddComponent<AudioManager>();
            var so = new SerializedObject(manager);
            SerializedProperty list = so.FindProperty("effects");
            list.arraySize = effects.Count;
            for (int i = 0; i < effects.Count; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").intValue = (int)effects[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = effects[i].clip;
                e.FindPropertyRelative("volume").floatValue = effects[i].volume;
                e.FindPropertyRelative("minInterval").floatValue = effects[i].minInterval;
                e.FindPropertyRelative("pitchJitter").floatValue = effects[i].pitchJitter;
            }

            so.FindProperty("music").objectReferenceValue = Clip("Music/Ambient", music: true);
            so.ApplyModifiedPropertiesWithoutUndo();

            var director = go.AddComponent<AudioDirector>();
            Set(director, "world", world);
            Set(director, "waves", waves);
            Set(director, "session", session);
            Set(director, "core", core);
            Set(director, "hero", hero);
            Set(director, "turrets", Turrets.ToArray());
            Set(director, "pads", Pads.ToArray());
        }

        private static SfxEntry Effect(Sfx id, float volume, float minInterval, float pitchJitter) => new SfxEntry
        {
            id = id, clip = Clip("SFX/" + id, music: false), volume = volume, minInterval = minInterval, pitchJitter = pitchJitter,
        };

        private static AudioClip Clip(string name, bool music)
        {
            string path = $"{Root}/Audio/{name}.wav";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogError($"Audio clip missing: {path} (run Tools/generate_audio.py)");
                return null;
            }

            importer.forceToMono = true;
            importer.loadInBackground = music;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.5f : 0.7f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static Button PillButton(string name, Transform parent, string label, Color color, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, out TMP_Text text)
        {
            Image image = Image(name, parent, "Pill_9s", color);
            Place(image.rectTransform, anchor, new Vector2(0.5f, 0.5f), position, size);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            text = Text("Label", image.transform, label, fontSize, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)text.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(20f, 20f));
            return button;
        }

        private static RectTransform FullScreen(string name, Transform parent, Color dim)
        {
            RectTransform root = Rect(name, parent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            Image back = Image("Dim", root, null, dim);
            back.rectTransform.anchorMin = Vector2.zero;
            back.rectTransform.anchorMax = Vector2.one;
            back.rectTransform.offsetMin = back.rectTransform.offsetMax = Vector2.zero;
            back.raycastTarget = true;
            return root;
        }

        private static void BuildMenuPanel(RectTransform canvasRect, GameSession session)
        {
            RectTransform root = FullScreen("Menu", canvasRect, new Color(0.01f, 0.01f, 0.05f, 0.8f));
            var group = root.gameObject.AddComponent<CanvasGroup>();

            TMP_Text title = Text("Title", root, "CRYSTAL\nOUTPOST", 150f, new Color32(120, 235, 255, 255), TextAlignmentOptions.Center);
            Place((RectTransform)title.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 480f), new Vector2(1000f, 400f));
            title.richText = false;

            TMP_Text best = Text("Best", root, "DEFEND THE CORE", 56f, new Color32(200, 215, 240, 255), TextAlignmentOptions.Center);
            Place((RectTransform)best.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(900f, 80f));

            Button play = PillButton("PlayButton", root, "PLAY", new Color32(60, 200, 235, 255), new Vector2(0.5f, 0.5f), new Vector2(0f, -420f), new Vector2(560f, 180f), 84f, out _);

            TMP_Text hint = Text("Hint", root, "DRAG TO MOVE  -  STAND STILL TO SHOOT\nSTAND ON A PAD TO BUILD", 40f, new Color32(170, 185, 215, 255), TextAlignmentOptions.Center);
            Place((RectTransform)hint.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -640f), new Vector2(1000f, 140f));

            var panel = canvasRect.gameObject.AddComponent<MenuPanel>();
            Set(panel, "session", session);
            Set(panel, "root", root.gameObject);
            Set(panel, "group", group);
            Set(panel, "bestLabel", best);
            Set(panel, "playButton", play);
            Set(panel, "playRect", play.transform);
        }

        private static void BuildPausePanel(RectTransform canvasRect, RectTransform safe, GameSession session, Joystick joystick)
        {
            Button pause = PillButton("PauseButton", safe, "II", new Color(0.02f, 0.02f, 0.1f, 0.7f), new Vector2(1f, 1f), new Vector2(-96f, -200f), new Vector2(120f, 110f), 56f, out _);

            RectTransform root = FullScreen("Pause", canvasRect, new Color(0f, 0f, 0.03f, 0.72f));
            Image window = Image("Window", root, "Rounded_9s", new Color(0.07f, 0.07f, 0.16f, 0.97f));
            Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 980f));

            TMP_Text title = Text("Title", window.transform, "PAUSED", 96f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(700f, 120f));

            var size = new Vector2(540f, 140f);
            Button resume = PillButton("Resume", window.transform, "RESUME", new Color32(60, 200, 235, 255), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), size, 60f, out _);
            Button sound = PillButton("Sound", window.transform, "SOUND: ON", new Color32(70, 80, 120, 255), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), size, 52f, out TMP_Text soundLabel);
            Button music = PillButton("Music", window.transform, "MUSIC: ON", new Color32(70, 80, 120, 255), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), size, 52f, out TMP_Text musicLabel);
            Button restart = PillButton("Restart", window.transform, "RESTART", new Color32(230, 95, 90, 255), new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), size, 56f, out _);

            var panel = canvasRect.gameObject.AddComponent<PausePanel>();
            Set(panel, "session", session);
            Set(panel, "joystick", joystick);
            Set(panel, "pauseButton", pause);
            Set(panel, "root", root.gameObject);
            Set(panel, "window", window.rectTransform);
            Set(panel, "resumeButton", resume);
            Set(panel, "restartButton", restart);
            Set(panel, "soundButton", sound);
            Set(panel, "musicButton", music);
            Set(panel, "soundLabel", soundLabel);
            Set(panel, "musicLabel", musicLabel);
            root.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
        }

        private static void BuildResultPanel(RectTransform canvasRect, GameSession session, Joystick joystick)
        {
            RectTransform root = Rect("Result", canvasRect);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var group = root.gameObject.AddComponent<CanvasGroup>();

            Image dim = Image("Dim", root, null, new Color(0f, 0f, 0.03f, 0.72f));
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            dim.raycastTarget = true;

            Image window = Image("Window", root, "Rounded_9s", new Color(0.07f, 0.07f, 0.16f, 0.97f));
            Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(840f, 980f));

            TMP_Text title = Text("Title", window.transform, "OUTPOST HELD", 92f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(780f, 120f));

            TMP_Text stats = Text("Stats", window.transform, "WAVE 10\n0 KILLS\nBEST WAVE 10", 56f, new Color32(200, 215, 240, 255), TextAlignmentOptions.Center);
            Place((RectTransform)stats.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(780f, 260f));

            Image buttonImage = Image("RestartButton", window.transform, "Pill_9s", new Color32(60, 200, 235, 255));
            Place(buttonImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(540f, 150f));
            buttonImage.raycastTarget = true;
            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            TMP_Text buttonLabel = Text("Label", buttonImage.transform, "PLAY AGAIN", 60f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)buttonLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 120f));

            var panel = canvasRect.gameObject.AddComponent<ResultPanel>();
            Set(panel, "session", session);
            Set(panel, "joystick", joystick);
            Set(panel, "root", root.gameObject);
            Set(panel, "group", group);
            Set(panel, "window", window.rectTransform);
            Set(panel, "titleLabel", title);
            Set(panel, "statsLabel", stats);
            Set(panel, "restartButton", button);

            Image endlessImage = Image("EndlessButton", window.transform, "Pill_9s", new Color32(150, 110, 240, 255));
            Place(endlessImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(540f, 150f));
            endlessImage.raycastTarget = true;
            var endlessButton = endlessImage.gameObject.AddComponent<Button>();
            endlessButton.targetGraphic = endlessImage;
            TMP_Text endlessLabel = Text("Label", endlessImage.transform, "ENDLESS MODE", 52f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)endlessLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 120f));
            Set(panel, "endlessButton", endlessButton);

            root.gameObject.SetActive(false);
        }

        private static void BuildHud(GameWorld world, WaveSpawner waves, CoreCrystal core, Hero hero, GameSession session, out Joystick joystick)
        {
            var canvasGo = new GameObject("Canvas_HUD", typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();
            var canvasRect = (RectTransform)canvasGo.transform;

            Image vignette = Image("Vignette", canvasRect, "Vignette", Color.white);
            vignette.rectTransform.anchorMin = Vector2.zero;
            vignette.rectTransform.anchorMax = Vector2.one;
            vignette.rectTransform.offsetMin = vignette.rectTransform.offsetMax = Vector2.zero;

            RectTransform safe = Rect("SafeArea", canvasRect);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<CrystalOutpost.UI.SafeArea>();

            Image crystalPill = Image("Crystals", safe, "Pill_9s", new Color(0.02f, 0.02f, 0.1f, 0.7f));
            Place(crystalPill.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -36f), new Vector2(280f, 96f));
            Image crystalIcon = Image("Icon", crystalPill.transform, "CrystalIcon", Color.white);
            Place(crystalIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(88f, 88f));
            TMP_Text crystalLabel = Text("Count", crystalPill.transform, "0", 54f, Color.white, TextAlignmentOptions.Left);
            Place((RectTransform)crystalLabel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, 0f), new Vector2(170f, 80f));

            Image enemyPill = Image("Enemies", safe, "Pill_9s", new Color(0.02f, 0.02f, 0.1f, 0.7f));
            Place(enemyPill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -36f), new Vector2(230f, 96f));
            Image enemyIcon = Image("Icon", enemyPill.transform, "EnemyIcon", new Color32(255, 110, 100, 255));
            Place(enemyIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(72f, 72f));
            TMP_Text enemyLabel = Text("Count", enemyPill.transform, "0", 54f, Color.white, TextAlignmentOptions.Left);
            Place((RectTransform)enemyLabel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(98f, 0f), new Vector2(130f, 80f));

            TMP_Text waveLabel = Text("Wave", safe, "WAVE 1", 44f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)waveLabel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 60f));

            Bar("CoreBar", safe, new Vector2(0.5f, 1f), new Vector2(0f, -216f), new Vector2(620f, 34f), new Color32(80, 230, 255, 255), out Image coreFill);
            Bar("HeroBar", safe, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(380f, 24f), new Color32(120, 240, 130, 255), out Image heroFill);

            RectTransform stickRoot = Rect("Joystick", canvasRect);
            stickRoot.anchorMin = Vector2.zero;
            stickRoot.anchorMax = Vector2.one;
            stickRoot.offsetMin = stickRoot.offsetMax = Vector2.zero;
            Image stickBase = Image("Base", stickRoot, "Ring", new Color(1f, 1f, 1f, 0.35f));
            Place(stickBase.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -560f), new Vector2(300f, 300f));
            Image stickKnob = Image("Knob", stickBase.transform, "Circle", new Color(1f, 1f, 1f, 0.55f));
            Place(stickKnob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));

            joystick = stickRoot.gameObject.AddComponent<Joystick>();
            Set(joystick, "canvasRect", canvasRect);
            Set(joystick, "baseRect", stickBase.rectTransform);
            Set(joystick, "knobRect", stickKnob.rectTransform);
            stickBase.gameObject.SetActive(false);

            var hud = canvasGo.AddComponent<Hud>();
            Set(hud, "world", world);
            Set(hud, "waves", waves);
            Set(hud, "core", core);
            Set(hud, "hero", hero);
            Set(hud, "crystalLabel", crystalLabel);
            Set(hud, "crystalIcon", crystalIcon.rectTransform);
            Set(hud, "waveLabel", waveLabel);
            Set(hud, "enemyLabel", enemyLabel);
            Set(hud, "coreFill", coreFill);
            Set(hud, "heroFill", heroFill);

            TMP_Text banner = Text("WaveBanner", safe, "WAVE 1", 120f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)banner.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(1000f, 160f));
            banner.gameObject.SetActive(false);
            Set(hud, "waveBanner", banner);

            BuildResultPanel(canvasRect, session, joystick);
            BuildPausePanel(canvasRect, safe, session, joystick);
            BuildMenuPanel(canvasRect, session);
        }

        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'.", target);
                return;
            }

            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object obj: prop.objectReferenceValue = obj; break;
                case Object[] array:
                    prop.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                    break;
                case int[] ints:
                    prop.arraySize = ints.Length;
                    for (int i = 0; i < ints.Length; i++)
                        prop.GetArrayElementAtIndex(i).intValue = ints[i];
                    break;
                case string s: prop.stringValue = s; break;
                case System.Enum e: prop.intValue = System.Convert.ToInt32(e); break;
                case float f: prop.floatValue = f; break;
                case int i: prop.intValue = i; break;
                case bool b: prop.boolValue = b; break;
                case Color c: prop.colorValue = c; break;
                default: Debug.LogError($"Unsupported value type {value.GetType()} for '{field}'."); break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("CrystalOutpost/Setup/Apply Project Settings")]
        public static void ApplyProjectSettings()
        {
            PlayerSettings.companyName = "Zeafkee";
            PlayerSettings.productName = "CrystalOutpost";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.zeafkee.crystaloutpost");

            PlayerSettings.gcIncremental = true;
            PlayerSettings.accelerometerFrequency = 0;
            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;

            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;

            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            AssetDatabase.SaveAssets();
        }
    }
}
