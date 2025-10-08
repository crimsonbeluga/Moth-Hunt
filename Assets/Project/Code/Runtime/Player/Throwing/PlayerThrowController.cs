using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;           // New Input System
using MothHunt.Inventory;
using MothHunt.Input;                   // PlayerInputRouter + InputPointer

namespace MothHunt.Throwing
{
    /// <summary>
    /// Throw overlay for hotbar items (1/2/3).
    /// - Press the same slot again to cancel.
    /// - Auto-cancel on Jump press, Sprint (while moving), Crawl, Glide, Climb, or leaving ground.
    /// - Uses "Throw" action (LMB / RT) to actually throw.
    /// - Plane lock: set planeIsXY=true for 2D side-scroller (throw only on X/Y).
    /// - NOTE: Held-in-hand prop visuals are DISABLED by default (animation only).
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class PlayerThrowController : MonoBehaviour
    {
        [Serializable]
        public struct Option
        {
            public string label;           // debug label
            public ThrowItemDef throwDef;  // tuning + links
        }

        [Header("Config (index 0=slot1, 1=slot2, 2=slot3)")]
        public List<Option> options = new()
        {
            new Option{ label="Pebble",   throwDef=null },
            new Option{ label="Bottle",   throwDef=null },
            new Option{ label="Mushroom", throwDef=null },
        };

        [Header("Space")]
        [Tooltip("Side-scroller = true (lock to X/Y). Top-down = false (lock to X/Z).")]
        public bool planeIsXY = true;

        [Tooltip("Hand/muzzle transform. If null, an empty child named 'Hand_Spawn' will be created.")]
        public Transform hand;

        [Tooltip("Optional: a TrajectoryPreview that implements IThrowPreview.")]
        public MonoBehaviour preview;

        [Header("Refs (auto-found if empty)")]
        public Camera cam;
        public Animator anim;
        public PlayerInventory inventory;

        [Header("Animator Params")]
        public string p_IsThrowMode = "IsThrowMode";
        public string p_ThrowItemType = "ThrowItemType"; // 0 none, 1/2/3 = hotbar slot
        public string p_ThrowTrigger = "Throw";

        [Header("Visual Toggles")]
        [Tooltip("If true, spawns the holdInHandPrefab. Default OFF (animation-only).")]
        public bool showHeldInHand = false;   // <- default false per your request

        public bool IsActive { get; private set; }
        public ThrowItemDef CurrentDef { get; private set; }

        GameObject _heldVisual;
        float _cooldown;
        int _currentIndex = -1;

        void Reset() { AutoWire(); }
        void Awake() { AutoWire(); }

        void OnEnable()
        {
            // Hotbar events
            PlayerInputRouter.OnInventorySlotOnePressed += OnSlot1Pressed;
            PlayerInputRouter.OnInventorySlotTwoPressed += OnSlot2Pressed;
            PlayerInputRouter.OnInventorySlotThreePressed += OnSlot3Pressed;

            // Instant cancel on jump edge
            PlayerInputRouter.OnJumpPressed += OnJumpPressedCancel;
        }

        void OnDisable()
        {
            PlayerInputRouter.OnInventorySlotOnePressed -= OnSlot1Pressed;
            PlayerInputRouter.OnInventorySlotTwoPressed -= OnSlot2Pressed;
            PlayerInputRouter.OnInventorySlotThreePressed -= OnSlot3Pressed;

            PlayerInputRouter.OnJumpPressed -= OnJumpPressedCancel;
        }

        void AutoWire()
        {
            if (!cam) cam = Camera.main;
            if (!anim) anim = GetComponentInChildren<Animator>();
            if (!inventory) inventory = GetComponent<PlayerInventory>();
            if (!hand)
            {
                var t = transform.Find("Hand_Spawn");
                if (!t)
                {
                    var go = new GameObject("Hand_Spawn");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(0.25f, 1.2f, 0.3f);
                    hand = go.transform;
                }
                else hand = t;
            }
        }

        // --- Slot handlers with toggle behavior ---
        void OnSlot1Pressed() => ToggleOrEquip(0);
        void OnSlot2Pressed() => ToggleOrEquip(1);
        void OnSlot3Pressed() => ToggleOrEquip(2);

        void ToggleOrEquip(int index)
        {
            if (IsActive && _currentIndex == index) { ExitThrowMode(); return; }
            TryEquip(index);
        }

        void OnJumpPressedCancel()
        {
            if (IsActive) ExitThrowMode();
        }

        void Update()
        {
            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);

            // Throw input: "Throw" action (LMB / RT) via PlayerInputRouter
            if (IsActive && _cooldown == 0f && PlayerInputRouter.ThrowPressedThisFrame)
                TryThrow();

            // Auto-cancel on disallowed locomotion or airborne
            if (IsActive)
            {
                if (PlayerInputRouter.SprintHeld && PlayerInputRouter.Move.magnitude > 0.1f) ExitThrowMode();
                if (PlayerInputRouter.CrawlHeld) ExitThrowMode();
                if (PlayerInputRouter.GlideHeld) ExitThrowMode();
                if (PlayerInputRouter.ClimbPressedThisFrame) ExitThrowMode();
                if (!PlayerInputRouter.IsGrounded) ExitThrowMode();
            }

            // Animator sync
            if (anim)
            {
                anim.SetBool(p_IsThrowMode, IsActive);
                anim.SetInteger(p_ThrowItemType, IsActive ? (_currentIndex + 1) : 0);
            }
        }

        void TryEquip(int index)
        {
            if (index < 0 || index >= options.Count) return;

            var def = options[index].throwDef;
            if (!def || !def.item)
            {
                Debug.LogWarning($"[Throw] Option {index} missing ThrowItemDef or ItemDef.");
                return;
            }

            if (!PlayerInputRouter.IsGrounded) return;

            if (!inventory || inventory.GetCount(def.item) <= 0)
            {
                Debug.Log($"[Throw] No '{def.item?.name}' in inventory.");
                return;
            }

            EnterThrowMode(index, def);
        }

        void EnterThrowMode(int index, ThrowItemDef def)
        {
            _currentIndex = index;
            CurrentDef = def;
            IsActive = true;

            // 🔕 No held-in-hand visual (animation-only)
            if (_heldVisual) { Destroy(_heldVisual); _heldVisual = null; }

            if (showHeldInHand && def.holdInHandPrefab && hand)
            {
                // Kept behind a toggle in case you ever want it back.
                _heldVisual = Instantiate(def.holdInHandPrefab, hand);
                _heldVisual.transform.localPosition = Vector3.zero;
                _heldVisual.transform.localRotation = Quaternion.identity;

                // Safety: disable physics on any RBs if someone flips the toggle
                var heldRBs = _heldVisual.GetComponentsInChildren<Rigidbody>(includeInactive: true);
                foreach (var rb in heldRBs) { rb.isKinematic = true; rb.detectCollisions = false; }
            }

            if (preview is IThrowPreview p) p.Show(this);
        }

        void ExitThrowMode()
        {
            IsActive = false;
            CurrentDef = null;
            _currentIndex = -1;
            if (_heldVisual) { Destroy(_heldVisual); _heldVisual = null; }
            if (preview is IThrowPreview p) p.Hide();
        }

        void TryThrow()
        {
            if (!IsActive || CurrentDef == null || _cooldown > 0f) return;

            if (CurrentDef.consumeOnThrow && inventory && CurrentDef.item)
            {
                if (!inventory.TryConsume(CurrentDef.item, 1))
                {
                    ExitThrowMode();
                    return;
                }
            }

            if (!GetPredictedLaunch(out var origin, out var velocity)) return;

            var prefab = CurrentDef.projectilePrefab;
            if (prefab)
            {
                // Spawn with identity so we don’t inherit hand pitch/yaw.
                var projGO = Instantiate(prefab, origin, Quaternion.identity);

                // Ensure there is a ThrowableProjectile component.
                var proj = projGO.GetComponent<ThrowableProjectile>() ?? projGO.AddComponent<ThrowableProjectile>();
                proj.def = CurrentDef;
                proj.planeIsXY = planeIsXY;   // tell it which plane to lock to
                proj.Launch(velocity);        // handles rotation lock + constraints
            }

            anim?.SetTrigger(p_ThrowTrigger);
            _cooldown = CurrentDef.cooldown;

            if (CurrentDef.consumeOnThrow && inventory && CurrentDef.item && inventory.GetCount(CurrentDef.item) <= 0)
                ExitThrowMode();
        }

        // === Plane-aware launch ===
        public bool GetPredictedLaunch(out Vector3 origin, out Vector3 velocity)
        {
            origin = hand ? hand.position : transform.position + Vector3.up * 1.2f;
            velocity = Vector3.zero;
            if (CurrentDef == null) return false;

            if (!cam) cam = Camera.main;

            Vector3 dir = Vector3.right;

            if (CurrentDef.useAimRaycast && cam)
            {
                // Use new Input System pointer position
                var mp = (Vector3)InputPointer.ScreenPosition;

                if (planeIsXY)
                {
                    // 2D side-scroller: project pointer to the Z-plane of the hand
                    float depth = Mathf.Abs(cam.transform.position.z - origin.z);
                    if (cam.orthographic)
                    {
                        var world = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, depth));
                        var to = world - origin; to.z = 0f;
                        dir = to.sqrMagnitude > 0.00001f ? to.normalized : Vector3.right;
                    }
                    else
                    {
                        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, origin.z));
                        var ray = cam.ScreenPointToRay(mp);
                        if (plane.Raycast(ray, out float t))
                        {
                            var hit = ray.GetPoint(t);
                            var to = (hit - origin); to.z = 0f;
                            dir = to.sqrMagnitude > 0.00001f ? to.normalized : Vector3.right;
                        }
                        else
                        {
                            dir = cam.transform.right; dir.z = 0f; dir.Normalize();
                        }
                    }
                }
                else
                {
                    // Top-down style: project pointer to Y-plane of the hand
                    var plane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
                    var ray = cam.ScreenPointToRay(mp);
                    if (plane.Raycast(ray, out float t))
                    {
                        var hit = ray.GetPoint(t);
                        var to = (hit - origin); to.y = 0f;
                        dir = to.sqrMagnitude > 0.00001f ? to.normalized : Vector3.right;
                    }
                    else
                    {
                        dir = cam.transform.forward; dir.y = 0f; dir.Normalize();
                    }
                }
            }
            else
            {
                // No aim raycast: throw in facing/right direction locked to plane
                dir = cam ? cam.transform.right : transform.right;
                if (planeIsXY) dir = new Vector3(Mathf.Sign(dir.x == 0 ? 1 : dir.x), 0f, 0f);
                else dir = new Vector3(Mathf.Sign(dir.x == 0 ? 1 : dir.x), 0f, Mathf.Sign(dir.z == 0 ? 1 : dir.z));
            }

            float speed = CurrentDef.initialSpeed;
            velocity = dir * speed;

            // FINAL LOCK
            if (planeIsXY) velocity.z = 0f;   // X/Y only
            else velocity.y = 0f;             // X/Z only

            return true;
        }
    }

    public interface IThrowPreview { void Show(PlayerThrowController ctrl); void Hide(); }
}
