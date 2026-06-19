#if UNITY_EDITOR
using MovementMD.Core;
using MovementMD.Dev;
using MovementMD.UI.DevMenu;
using MovementMD.UI.HUD;
using MovementMD.UI.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace MovementMD.Editor
{
    /// <summary>
    /// One-time shell setup. Generates the Boot + additive placeholder scenes, creates the
    /// PanelSettings and Tunables assets, wires all serialized references, and configures
    /// Build Settings. Idempotent — safe to re-run.
    /// Run via menu: MovementMD ▸ Setup ▸ Create Shell Scenes &amp; Assets.
    /// </summary>
    public static class ShellSetup
    {
        private const string MenuRoot = "MovementMD/Setup/Create Shell Scenes & Assets";

        private const string BootPath = "Assets/Scenes/Boot.unity";
        private const string MatchPath = "Assets/Scenes/Match.unity";
        private const string SandboxPath = "Assets/Scenes/Sandbox.unity";
        private const string TrainingPath = "Assets/Scenes/Training.unity";

        private const string PanelPath = "Assets/_Project/Settings/UIPanelSettings.asset";
        private const string TunablesPath = "Assets/_Project/Dev/Tunables.asset";

        [MenuItem(MenuRoot, priority = 0)]
        public static void Run()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets/_Project", "Settings");
            EnsureFolder("Assets/_Project", "Dev");

            var panel = GetOrCreateAsset<PanelSettings>(PanelPath);
            var tunables = GetOrCreateAsset<Tunables>(TunablesPath);

            CreatePlaceholderScene(MatchPath, new Color(0.18f, 0.20f, 0.24f), "Match (count-agnostic: 1v1 / 2v2)");
            CreatePlaceholderScene(SandboxPath, new Color(0.14f, 0.22f, 0.18f), "Sandbox");
            CreatePlaceholderScene(TrainingPath, new Color(0.22f, 0.18f, 0.14f), "Training");
            CreateBootScene(panel, tunables);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootPath, true),
                new EditorBuildSettingsScene(MatchPath, true),
                new EditorBuildSettingsScene(SandboxPath, true),
                new EditorBuildSettingsScene(TrainingPath, true),
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);
            Debug.Log("[MovementMD] Shell setup complete. Press Play to land in the Main Menu; F1 opens the Developer Menu.");
        }

        // ---- scenes -----------------------------------------------------------------------

        private static void CreateBootScene(PanelSettings panel, Tunables tunables)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            cam.depth = -100; // render below additive mode-scene cameras so arenas show on top
            cam.transform.position = new Vector3(0, 3, -8);

            var flowGo = new GameObject("[AppFlow]");
            flowGo.AddComponent<AppFlow>();

            var esGo = new GameObject("[EventSystem]");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            var menuGo = new GameObject("[MainMenu]");
            var menuDoc = menuGo.AddComponent<UIDocument>();
            menuDoc.panelSettings = panel;
            menuGo.AddComponent<MainMenuController>();

            var devGo = new GameObject("[DevMenu]");
            var devDoc = devGo.AddComponent<UIDocument>();
            devDoc.panelSettings = panel;
            devGo.AddComponent<DevMenuController>();

            var toolsGo = new GameObject("[DevTools]");
            toolsGo.transform.SetParent(devGo.transform, false);
            AssignSerialized(toolsGo.AddComponent<ParamTweakTool>(), "tunables", tunables);
            toolsGo.AddComponent<SceneLoaderTool>();
            toolsGo.AddComponent<DeterminismTestRunnerTool>();
            toolsGo.AddComponent<SpawnDummyTool>();
            toolsGo.AddComponent<OverlayToggleTool>();
            toolsGo.AddComponent<InputRecordReplayTool>();

            BuildHud();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootPath);
        }

        private static void BuildHud()
        {
            var hudGo = new GameObject("[HUD]");
            var canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            hudGo.AddComponent<CanvasScaler>();
            hudGo.AddComponent<GraphicRaycaster>();

            var labelGo = new GameObject("ModeLabel");
            labelGo.transform.SetParent(hudGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -16f);
            rt.sizeDelta = new Vector2(600f, 40f);

            var hud = hudGo.AddComponent<HUDController>();
            AssignSerialized(hud, "modeLabel", text);
        }

        private static void CreatePlaceholderScene(string path, Color ground, string marker)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            cam.transform.position = new Vector3(0f, 12f, -18f);
            cam.transform.localEulerAngles = new Vector3(45f, 0f, 0f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.localEulerAngles = new Vector3(50f, -30f, 0f);

            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.transform.localScale = new Vector3(4f, 1f, 4f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = ground;
            groundGo.GetComponent<Renderer>().sharedMaterial = mat;

            var markerGo = new GameObject(marker);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        // ---- assets / helpers -------------------------------------------------------------

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void AssignSerialized(Object component, string fieldName, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[ShellSetup] Serialized field '{fieldName}' not found on {component.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
