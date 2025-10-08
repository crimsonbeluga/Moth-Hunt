// Runtime/Player/Death/PlayerDeathManager.cs
using System.Collections;
using System.Linq;
using UnityEngine;
using MothHunt.Systems; // RespawnManager
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MothHunt.Player
{
    [DisallowMultipleComponent]
    public class PlayerDeathManager : MonoBehaviour
    {
        [Header("Sequence")]
        [Min(0f)] public float deathHoldSeconds = 1.0f;
        [Min(0f)] public float postRespawnHoldSeconds = 0.2f;

        [Header("Debug")]
        public bool debug = true;

        // hidden, auto-wired
        private Animator _animator;
        private MonoBehaviour _playerBrain;               // "PlayerBrain"
        private MonoBehaviour _playerMotor;               // "PlayerMotor"
        private CharacterController _characterController;
        private GameObject _playerVisualRoot;
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        private Vector3 _initialPos;
        private Quaternion _initialRot;

        private const string DeathTriggerName = "Death";
        private const string DeathStateName = "Death";

        // Post-respawn invulnerability window
        private float _invulnUntil = -999f;
        public bool IsInvulnerable => Time.time < _invulnUntil;

        private void Awake()
        {
            _initialPos = transform.position;
            _initialRot = transform.rotation;
            AutoWireIfMissing();

            D($"Awake. initPos={_initialPos} initRot={_initialRot.eulerAngles} brain?={_playerBrain} motor?={_playerMotor} cc?={_characterController} anim?={_animator} playerInput?={_playerInput}");
        }

        public void BeginDeathSequence(string cause, Transform killer, System.Action onDone)
        {
            D($"BeginDeathSequence cause='{cause}' killer={(killer ? killer.name : "null")}");
            StartCoroutine(Co_DeathSequence(onDone));
        }

        private IEnumerator Co_DeathSequence(System.Action onDone)
        {
            D("Co_DeathSequence ENTER");

            // --- LOCK INPUT + CONTROL ---
            RouterInput_Enable(false); // Brain owns router binding; this only toggles InputSystem asset if present
            D($"Actions disabled. brain.enabled={_playerBrain?.enabled} motor.enabled={_playerMotor?.enabled}");
            SetControlEnabled(false);
            D($"Control disabled. brain.enabled={_playerBrain?.enabled} motor.enabled={_playerMotor?.enabled}");

            bool animatorFrozen = false;

            try
            {
                // ---- ANIMATOR (robust) ----
                try
                {
                    if (_animator != null)
                    {
                        const int layer = 0;
                        if (HasAnimatorTrigger(_animator, DeathTriggerName))
                        {
                            _animator.ResetTrigger(DeathTriggerName);
                            _animator.SetTrigger(DeathTriggerName);
                            D($"Animator: SetTrigger('{DeathTriggerName}')");
                        }
                        else
                        {
                            int deathHash = Animator.StringToHash(DeathStateName);
                            if (_animator.HasState(layer, deathHash))
                            {
                                _animator.Play(deathHash, layer, 0f);
                                D($"Animator: Play('{DeathStateName}')");
                            }
                            else
                            {
                                _animator.speed = 0f;
                                animatorFrozen = true;
                                W($"Animator has no Trigger '{DeathTriggerName}' and no State '{DeathStateName}'. Freezing briefly.");
                            }
                        }
                    }
                    else
                    {
                        D("Animator: none (skipping).");
                    }
                }
                catch (System.Exception e)
                {
                    W($"Animator step failed but continuing: {e.Message}");
                }

                if (deathHoldSeconds > 0f)
                {
                    D($"Hold before teleport: {deathHoldSeconds:0.###}s");
                    yield return new WaitForSeconds(deathHoldSeconds);
                }

                // ---- TELEPORT ----
                Vector3 targetPos; Quaternion targetRot;
                if (RespawnManager.Instance != null)
                {
                    RespawnManager.Instance.GetRespawn(out targetPos, out targetRot);
                    D($"RespawnManager => pos={targetPos} rot={targetRot.eulerAngles}");
                }
                else
                {
                    targetPos = _initialPos; targetRot = _initialRot;
                    W("RespawnManager NOT found. Using initial spawn.");
                }

                Teleport(targetPos, targetRot);
                D($"Teleported. cc.enabled={_characterController?.enabled} visualRoot?={_playerVisualRoot != null}");

                // Optional motor reset if present
                TryCallMethod(_playerMotor, "ResetForRespawn");
                TryCallMethod(_playerMotor, "ZeroVelocity");
                TrySetField(_playerMotor, "velocity", Vector3.zero);

                // Brief post-respawn invulnerability so hazards can't immediately re-kill
                _invulnUntil = Time.time + 0.5f;

                if (postRespawnHoldSeconds > 0f)
                {
                    D($"Post-respawn hold: {postRespawnHoldSeconds:0.###}s");
                    yield return new WaitForSeconds(postRespawnHoldSeconds);
                }

                if (_animator != null && animatorFrozen) _animator.speed = 1f;
            }
            finally
            {
                // ---- ALWAYS RE-ENABLE, EVEN IF SOMETHING ABOVE THREW ----
                RouterInput_Enable(true);
                SetControlEnabled(true);
                LogStatus("Re-enabled");

                onDone?.Invoke();
                D("Co_DeathSequence EXIT");
            }
        }

        // ----------------- helpers -----------------
        private void SetControlEnabled(bool enabled)
        {
            if (_playerBrain) _playerBrain.enabled = enabled;
            if (_playerMotor) _playerMotor.enabled = enabled;
            if (_characterController) _characterController.enabled = true; // ensure CC is on
        }

        private void Teleport(Vector3 pos, Quaternion rot)
        {
            if (_playerVisualRoot) _playerVisualRoot.SetActive(false);

            if (_characterController)
            {
                _characterController.enabled = false;
                transform.SetPositionAndRotation(pos, rot);
                _characterController.enabled = true;
            }
            else
            {
                transform.SetPositionAndRotation(pos, rot);
            }

            if (_playerVisualRoot) _playerVisualRoot.SetActive(true);
        }

        private static bool HasAnimatorTrigger(Animator anim, string triggerName)
        {
            if (!anim || string.IsNullOrEmpty(triggerName)) return false;
            var ps = anim.parameters;
            for (int i = 0; i < ps.Length; i++)
                if (ps[i].type == AnimatorControllerParameterType.Trigger && ps[i].name == triggerName)
                    return true;
            return false;
        }

        private void AutoWireIfMissing()
        {
            if (!_characterController)
            {
                TryGetComponent(out _characterController);
                if (!_characterController) _characterController = GetComponentInParent<CharacterController>();
            }
            if (!_animator)
            {
                TryGetComponent(out _animator);
                if (!_animator) _animator = GetComponentInChildren<Animator>(true);
            }
            if (_playerBrain == null) _playerBrain = FindBehaviourByTypeName("PlayerBrain");
            if (_playerMotor == null) _playerMotor = FindBehaviourByTypeName("PlayerMotor");
#if ENABLE_INPUT_SYSTEM
            if (_playerInput == null)
            {
                TryGetComponent(out _playerInput);
                if (_playerInput == null) _playerInput = GetComponentInParent<PlayerInput>();
                if (_playerInput == null) _playerInput = GetComponentInChildren<PlayerInput>(true);
            }
#endif
            if (_playerVisualRoot == null)
            {
                var r = GetComponentsInChildren<Renderer>(true);
                _playerVisualRoot = (r != null && r.Length > 0) ? r[0].gameObject
                                : (transform.childCount > 0 ? transform.GetChild(0).gameObject : gameObject);
            }
        }

        private MonoBehaviour FindBehaviourByTypeName(string typeName)
        {
            var self = GetComponents<MonoBehaviour>().FirstOrDefault(c => c && c.GetType().Name == typeName);
            if (self) return self;

            var parent = GetComponentsInParent<MonoBehaviour>(true).FirstOrDefault(c => c && c.GetType().Name == typeName);
            if (parent) return parent;

            var child = GetComponentsInChildren<MonoBehaviour>(true).FirstOrDefault(c => c && c.GetType().Name == typeName);
            if (child) return child;

            var global = FindObjectsOfType<MonoBehaviour>().FirstOrDefault(c => c && c.GetType().Name == typeName);
#if UNITY_EDITOR
            if (global == null) W($"Could not auto-find {typeName}. (Optional)");
#endif
            return global;
        }

        // Only toggle the Input System actions; PlayerBrain owns router binding in OnEnable
        private void RouterInput_Enable(bool enabled)
        {
#if ENABLE_INPUT_SYSTEM
            if (_playerInput && _playerInput.actions != null)
            {
                if (enabled) _playerInput.actions.Enable();
                else _playerInput.actions.Disable();
                D($"PlayerInput.actions {(enabled ? "ENABLED" : "DISABLED")} (asset='{_playerInput.actions?.name}', currentMap='{_playerInput.currentActionMap?.name ?? "null"}')");
            }
            else D("PlayerInput.actions=null (skip toggle).");
#endif
            if (enabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LogStatus(string tag)
        {
#if ENABLE_INPUT_SYSTEM
            var actionsEnabled = _playerInput && _playerInput.actions != null && _playerInput.actions.enabled;
            var mapName = _playerInput ? (_playerInput.currentActionMap?.name ?? "(null)") : "(no PlayerInput)";
#else
            var actionsEnabled = false; var mapName = "(NIS off)";
#endif
            D($"{tag} status: brain.enabled={_playerBrain?.enabled} motor.enabled={_playerMotor?.enabled} cc.enabled={_characterController?.enabled} actions.enabled={actionsEnabled} map='{mapName}' pos={transform.position}");
        }

        // reflection helpers (safe no-ops)
        private static void TryCallMethod(object obj, string methodName)
        {
            if (obj == null) return;
            var m = obj.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (m != null) { try { m.Invoke(obj, null); } catch { } }
        }
        private static void TrySetField(object obj, string fieldName, object value)
        {
            if (obj == null) return;
            var f = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f != null && (value == null || f.FieldType.IsAssignableFrom(value.GetType())))
            {
                try { f.SetValue(obj, value); } catch { }
            }
        }

        // ---------- tiny debug wrappers ----------
        private void D(string msg)
        {
            if (!debug) return;
            Debug.Log($"[PDM f{Time.frameCount} t{Time.time:0.000}] {msg}", this);
        }
        private void W(string msg)
        {
            if (!debug) return;
            Debug.LogWarning($"[PDM f{Time.frameCount} t{Time.time:0.000}] {msg}", this);
        }
    }
}
