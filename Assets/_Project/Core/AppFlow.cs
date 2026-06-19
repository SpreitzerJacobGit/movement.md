using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MovementMD.Core
{
    /// <summary>
    /// Mode state machine + additive scene orchestration. Lives exactly once in the Boot scene,
    /// which is always loaded; mode scenes (Match/Sandbox/Training) load additively on top.
    /// Singles and Doubles share one Match scene — only player count differs, so switching between
    /// them updates state without reloading the scene.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class AppFlow : MonoBehaviour
    {
        public static AppFlow Instance { get; private set; }

        public GameMode CurrentMode { get; private set; } = GameMode.Boot;
        public int CurrentPlayerCount => ModeConfig.Get(CurrentMode).PlayerCount;
        public bool IsInMatch => ModeConfig.Get(CurrentMode).IsMatch;

        public event System.Action<GameMode> ModeChanged;

        private string _loadedModeScene;
        private bool _transitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[AppFlow] Duplicate AppFlow — Boot must contain exactly one. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current[Key.Escape].wasPressedThisFrame && CurrentMode != GameMode.Boot)
                RequestMode(GameMode.Boot);
        }

        public void RequestMode(GameMode mode)
        {
            if (mode == CurrentMode) return;
            if (_transitioning)
            {
                Debug.LogWarning($"[AppFlow] Ignoring request to {mode}; a transition is in progress.", this);
                return;
            }
            StartCoroutine(TransitionTo(mode));
        }

        public void ReloadCurrentMode()
        {
            if (CurrentMode == GameMode.Boot)
            {
                Debug.LogWarning("[AppFlow] Nothing to reload while in Boot.", this);
                return;
            }
            if (_transitioning) return;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator TransitionTo(GameMode mode)
        {
            _transitioning = true;
            var def = ModeConfig.Get(mode);

            if (_loadedModeScene != null && _loadedModeScene != def.SceneName)
            {
                yield return SceneManager.UnloadSceneAsync(_loadedModeScene);
                _loadedModeScene = null;
            }

            if (def.SceneName != null && _loadedModeScene != def.SceneName)
            {
                yield return SceneManager.LoadSceneAsync(def.SceneName, LoadSceneMode.Additive);
                _loadedModeScene = def.SceneName;
            }

            CurrentMode = mode;
            _transitioning = false;
            ModeChanged?.Invoke(mode);
        }

        private IEnumerator ReloadRoutine()
        {
            _transitioning = true;
            var scene = _loadedModeScene;
            if (scene != null)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
                _loadedModeScene = null;
            }
            var def = ModeConfig.Get(CurrentMode);
            if (def.SceneName != null)
            {
                yield return SceneManager.LoadSceneAsync(def.SceneName, LoadSceneMode.Additive);
                _loadedModeScene = def.SceneName;
            }
            _transitioning = false;
            ModeChanged?.Invoke(CurrentMode);
        }
    }
}
