using System.Text;
using CrystalOutpost.Actors;
using CrystalOutpost.Dev;
using CrystalOutpost.UI;
using CrystalOutpost.World;
using UnityEditor;
using UnityEngine;

namespace CrystalOutpost.EditorTools
{
    public static class DevTools
    {
        [MenuItem("CrystalOutpost/Dev/Attach Auto-Pilot (3x speed)")]
        public static void AttachAutoPilotFast() => Debug.Log(AttachAutoPilot(3f));

        public static string AttachAutoPilot(float timeScale)
        {
            if (!Application.isPlaying) return "not playing";
            GameWorld world = GameWorld.Instance;
            if (world == null || world.Sim == null) return "world not ready";
            Object.FindAnyObjectByType<GameSession>().Begin();

            if (timeScale > 3f)
            {
                Time.captureFramerate = 20;
                Application.targetFrameRate = -1;
                EditorApplication.playModeStateChanged -= ResetTime;
                EditorApplication.playModeStateChanged += ResetTime;
            }
            else
            {
                Time.timeScale = timeScale;
            }

            if (world.GetComponent<DebugAutoPilot>() != null) return "already attached";

            var hero = Object.FindAnyObjectByType<Hero>();
            var joystick = Object.FindAnyObjectByType<Joystick>();
            world.gameObject.AddComponent<DebugAutoPilot>().Bind(world, joystick, hero.transform);

            Log.Clear();
            Object.FindAnyObjectByType<WaveSpawner>().WaveCleared += wave => Log.AppendLine(Status());
            Object.FindAnyObjectByType<GameSession>().Ended += won => Log.AppendLine((won ? "WON  " : "LOST ") + Status());
            return "attached";
        }

        private static readonly StringBuilder Log = new StringBuilder();

        public static string Report() => Log + "now: " + Status();

        private static void ResetTime(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            EditorApplication.playModeStateChanged -= ResetTime;
        }

        [MenuItem("CrystalOutpost/Dev/Attach Auto-Pilot (soak test, max speed)")]
        public static void AttachAutoPilotSoak() => Debug.Log(AttachAutoPilot(100f));

        public static string CapturePortrait(string path, int width = 1080, int height = 1920)
        {
            Camera cam = Camera.main;
            if (cam == null) return "no camera";

            var target = new RenderTexture(width, height, 24);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>();
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = cam.nearClipPlane + 0.1f;
            }

            cam.targetTexture = target;
            var rig = cam.GetComponent<View.CameraRig>();
            if (rig != null) rig.SnapToTarget();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            cam.targetTexture = null;
            if (rig != null) rig.SnapToTarget();
            for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            return path;
        }

        public static string StartBenchmark(int enemies, float seconds = 4f)
        {
            if (!Application.isPlaying) return "not playing";
            GameWorld world = GameWorld.Instance;
            Object.FindAnyObjectByType<GameSession>().Begin();
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            Application.targetFrameRate = -1;

            var core = Object.FindAnyObjectByType<CoreCrystal>();
            core.Fortify(1000000f);

            var sampler = world.GetComponent<BenchmarkSampler>();
            if (sampler == null) sampler = world.gameObject.AddComponent<BenchmarkSampler>();
            sampler.Begin(world, enemies, seconds);
            return "benchmark started";
        }

        public static string BenchmarkReport()
        {
            var sampler = Object.FindAnyObjectByType<BenchmarkSampler>();
            return sampler == null ? "no benchmark" : sampler.Report;
        }

        [MenuItem("CrystalOutpost/Dev/Log Status")]
        public static void LogStatus() => Debug.Log(Status());

        public static string Status()
        {
            if (!Application.isPlaying) return "not playing";
            GameWorld world = GameWorld.Instance;
            if (world == null || world.Sim == null) return "world not ready";

            var waves = Object.FindAnyObjectByType<WaveSpawner>();
            var core = Object.FindAnyObjectByType<CoreCrystal>();
            var session = Object.FindAnyObjectByType<GameSession>();
            var hero = Object.FindAnyObjectByType<Hero>();

            var text = new StringBuilder();
            text.Append($"t={Time.time:F0} wave={waves.WaveNumber}/{waves.WaveCount} enemies={world.Sim.Horde.Count} ");
            text.Append($"kills={world.Sim.KillCount} crystals={world.Crystals} core={core.Health:F0}/{core.MaxHealth:F0} ");
            text.Append($"hero={hero.Health:F0} dmg={hero.AttackDamage:F0} state={session.Current} fps={1f / Time.unscaledDeltaTime:F0} pads:");
            foreach (BuildPad pad in Object.FindObjectsByType<BuildPad>())
                text.Append($" {pad.name.Replace("Pad_", "")}={(pad.Available ? pad.RemainingCost.ToString() : "max")}");
            return text.ToString();
        }
    }

    public sealed class BenchmarkSampler : MonoBehaviour
    {
        private GameWorld _world;
        private View.HordeRenderer _renderer;
        private Transform[] _spawns;
        private int _target;
        private float _warmup;
        private float _remaining;
        private int _frames;
        private double _frameMs, _tickMs, _worstFrameMs;
        private long _gcStart;
        private int _gcCollectionsStart;

        public string Report { get; private set; } = "running";

        public void Begin(GameWorld world, int enemies, float seconds)
        {
            _world = world;
            _renderer = Object.FindAnyObjectByType<View.HordeRenderer>();
            var root = GameObject.Find("SpawnPoints").transform;
            _spawns = new Transform[root.childCount];
            for (int i = 0; i < _spawns.Length; i++) _spawns[i] = root.GetChild(i);
            _target = enemies;
            _warmup = 2f;
            _remaining = seconds;
            _frames = 0;
            _frameMs = _tickMs = _worstFrameMs = 0;
            Report = "running";
            enabled = true;
        }

        private void Update()
        {
            var sim = _world.Sim;
            for (int i = sim.Horde.Count; i < _target; i++)
            {
                Vector3 p = _spawns[Random.Range(0, _spawns.Length)].position;
                Vector2 jitter = Random.insideUnitCircle * 2.5f;
                sim.SpawnEnemy(Random.value < 0.8f ? 0 : 1, new Unity.Mathematics.float2(p.x + jitter.x, p.z + jitter.y));
            }

            if (_warmup > 0f)
            {
                _warmup -= Time.unscaledDeltaTime;
                if (_warmup <= 0f)
                {
                    _gcStart = System.GC.GetTotalMemory(false);
                    _gcCollectionsStart = System.GC.CollectionCount(0);
                }
                return;
            }

            double frame = Time.unscaledDeltaTime * 1000.0;
            _frames++;
            _frameMs += frame;
            _worstFrameMs = System.Math.Max(_worstFrameMs, frame);
            _tickMs += _world.LastTickMilliseconds;

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining > 0f) return;

            long gc = System.GC.GetTotalMemory(false) - _gcStart;
            Report = $"enemies~{_target} alive={sim.Horde.Count} frames={_frames} avgFrame={_frameMs / _frames:F2}ms " +
                     $"(~{1000.0 / (_frameMs / _frames):F0} fps) worst={_worstFrameMs:F1}ms simTick={_tickMs / _frames:F2}ms " +
                     $"drawCalls={(_renderer != null ? _renderer.DrawCalls : -1)} gcDelta={gc / 1024}KB gen0={System.GC.CollectionCount(0) - _gcCollectionsStart}";
            enabled = false;
        }
    }
}
