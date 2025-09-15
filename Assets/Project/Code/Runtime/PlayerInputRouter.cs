using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MothHunt.Input
{
    /// <summary>
    /// Centralized input facade. Gameplay never talks to InputSystem directly.
    /// Reads from one action map (usually "Player") and exposes intent values + edge events.
    /// </summary>
    public static class PlayerInputRouter
    {
        // ---------------- Values (INTENT, not gameplay state) ----------------
        public static Vector2 Move { get; private set; }
        public static Vector2 Look { get; private set; }
        public static bool IsMoving { get; private set; }

        public static bool JumpHeld { get; private set; }
        public static bool GlideHeld { get; private set; }
        public static bool SprintHeld { get; private set; }
        public static bool CrawlHeld { get; private set; } // maps to Crawl or (fallback) Crouch
        public static bool ClimbHeld { get; private set; }
        public static bool InteractHeld { get; private set; }
        public static bool ThrowHeld { get; private set; }
        public static bool MenuHeld { get; private set; }
        public static bool MapInventoryHeld { get; private set; }
        public static bool CamouflageHeld { get; private set; }
        public static bool Inv1Held { get; private set; }
        public static bool Inv2Held { get; private set; }
        public static bool Inv3Held { get; private set; }

        /// <summary>True while player intends an all-fours pose (sprint or crawl held).</summary>
        public static bool AllFoursIntent => SprintHeld || CrawlHeld;

        // --------------------------- Events (edges) ---------------------------
        public static event Action OnJumpPressed;
        public static event Action OnJumpReleased;

        public static event Action OnGlidePressed;
        public static event Action OnGlideReleased;

        public static event Action OnSprintPressed;
        public static event Action OnSprintReleased;

        public static event Action OnCrawlPressed;
        public static event Action OnCrawlReleased;

        public static event Action OnClimbPressed;
        public static event Action OnClimbReleased;

        public static event Action OnInteractPressed;
        public static event Action OnInteractReleased;

        public static event Action OnThrowPressed;
        public static event Action OnThrowReleased;

        public static event Action OnMenuPressed;
        public static event Action OnMenuReleased;

        public static event Action OnMapInventoryPressed;
        public static event Action OnMapInventoryReleased;

        public static event Action OnCamouflagePressed;
        public static event Action OnCamouflageReleased;

        public static event Action OnInventorySlotOnePressed;
        public static event Action OnInventorySlotOneReleased;

        public static event Action OnInventorySlotTwoPressed;
        public static event Action OnInventorySlotTwoReleased;

        public static event Action OnInventorySlotThreePressed;
        public static event Action OnInventorySlotThreeReleased;

        // -------------------------- Internal wiring --------------------------
        private const string ACTION_MOVE = "Move";
        private const string ACTION_LOOK = "Look";
        private const string ACTION_JUMP = "Jump";
        private const string ACTION_GLIDE = "Glide";
        private const string ACTION_SPRINT = "Sprint";
        private const string ACTION_CRAWL = "Crawl";
        private const string ACTION_CROUCH = "Crouch"; // fallback (older asset)
        private const string ACTION_CLIMB = "Climb";
        private const string ACTION_INTERACT = "Interact";
        private const string ACTION_THROW = "Throw";
        private const string ACTION_MENU = "Menu";
        private const string ACTION_MAP_INVENTORY = "Map/Inventory";
        private const string ACTION_CAMOUFLAGE = "Camouflage";
        private const string ACTION_INV_SLOT_ONE = "Inventory slot one";
        private const string ACTION_INV_SLOT_TWO = "Inventory slot two";
        private const string ACTION_INV_SLOT_THREE = "Inventory slot three";

        private static bool _bound;
        private static bool _jumpSupersededByGlide;

        private static InputActionMap _map;

        // Cached actions (may be null if not present in the asset)
        private static InputAction
            _actMove, _actLook, _actJump, _actGlide, _actSprint, _actCrawl, _actClimb,
            _actInteract, _actThrow, _actMenu, _actMapInv, _actCamouflage,
            _actInv1, _actInv2, _actInv3;

        /// <summary>Preferred entry point: bind using the generated wrapper.</summary>
        public static void Bind(MothHuntInput.PlayerActions actions)
        {
            if (_bound || actions.Equals(default)) return;
            Bind(actions.Get()); // explicit, avoids ambiguous implicit cast
        }

        /// <summary>Fallback: bind using a raw action map (usually the "Player" map).</summary>
        public static void Bind(InputActionMap map)
        {
            if (_bound || map == null) return;
            _bound = true;
            _map = map;

            // Resolve actions by name
            _actMove = _map.FindAction(ACTION_MOVE, throwIfNotFound: false);
            _actLook = _map.FindAction(ACTION_LOOK, throwIfNotFound: false);
            _actJump = _map.FindAction(ACTION_JUMP, throwIfNotFound: false);
            _actGlide = _map.FindAction(ACTION_GLIDE, throwIfNotFound: false);
            _actSprint = _map.FindAction(ACTION_SPRINT, throwIfNotFound: false);
            _actCrawl = _map.FindAction(ACTION_CRAWL, throwIfNotFound: false)
                         ?? _map.FindAction(ACTION_CROUCH, throwIfNotFound: false);
            _actClimb = _map.FindAction(ACTION_CLIMB, throwIfNotFound: false);
            _actInteract = _map.FindAction(ACTION_INTERACT, throwIfNotFound: false);
            _actThrow = _map.FindAction(ACTION_THROW, throwIfNotFound: false);
            _actMenu = _map.FindAction(ACTION_MENU, throwIfNotFound: false);
            _actMapInv = _map.FindAction(ACTION_MAP_INVENTORY, throwIfNotFound: false);
            _actCamouflage = _map.FindAction(ACTION_CAMOUFLAGE, throwIfNotFound: false);
            _actInv1 = _map.FindAction(ACTION_INV_SLOT_ONE, throwIfNotFound: false);
            _actInv2 = _map.FindAction(ACTION_INV_SLOT_TWO, throwIfNotFound: false);
            _actInv3 = _map.FindAction(ACTION_INV_SLOT_THREE, throwIfNotFound: false);

            // Subscribe if present
            if (_actMove != null)
            {
                _actMove.started += OnMoveStarted;
                _actMove.performed += OnMovePerformed;
                _actMove.canceled += OnMoveCanceled;
            }

            if (_actLook != null)
            {
                _actLook.performed += OnLookPerformed;
                _actLook.canceled += OnLookCanceled;
            }

            if (_actJump != null)
            {
                // Jump fires on key DOWN; release on key UP (Tap removed from binding)
                _actJump.started += OnJumpStarted;
                _actJump.canceled += OnJumpCanceled;
            }

            if (_actGlide != null)
            {
                _actGlide.performed += OnGlidePerformed; // after Hold minTime
                _actGlide.canceled += OnGlideCanceled;
            }

            if (_actSprint != null)
            {
                _actSprint.performed += OnSprintPerformed;
                _actSprint.canceled += OnSprintCanceled;
            }

            if (_actCrawl != null)
            {
                _actCrawl.performed += OnCrawlPerformed;
                _actCrawl.canceled += OnCrawlCanceled;
            }

            if (_actClimb != null)
            {
                _actClimb.performed += OnClimbPerformed;
                _actClimb.canceled += OnClimbCanceled;
            }

            if (_actInteract != null)
            {
                _actInteract.performed += OnInteractPerformed;
                _actInteract.canceled += OnInteractCanceled;
            }

            if (_actThrow != null)
            {
                _actThrow.performed += OnThrowPerformed;
                _actThrow.canceled += OnThrowCanceled;
            }

            if (_actMenu != null)
            {
                _actMenu.performed += OnMenuPerformed;
                _actMenu.canceled += OnMenuCanceled;
            }

            if (_actMapInv != null)
            {
                _actMapInv.performed += OnMapInventoryPerformed;
                _actMapInv.canceled += OnMapInventoryCanceled;
            }

            if (_actCamouflage != null)
            {
                _actCamouflage.performed += OnCamouflagePerformed;
                _actCamouflage.canceled += OnCamouflageCanceled;
            }

            if (_actInv1 != null)
            {
                _actInv1.performed += OnInv1Performed;
                _actInv1.canceled += OnInv1Canceled;
            }
            if (_actInv2 != null)
            {
                _actInv2.performed += OnInv2Performed;
                _actInv2.canceled += OnInv2Canceled;
            }
            if (_actInv3 != null)
            {
                _actInv3.performed += OnInv3Performed;
                _actInv3.canceled += OnInv3Canceled;
            }
        }

        /// <summary>Unbind all handlers and clear cached intent values.</summary>
        public static void Unbind()
        {
            if (!_bound) return;
            _bound = false;

            if (_actMove != null) { _actMove.started -= OnMoveStarted; _actMove.performed -= OnMovePerformed; _actMove.canceled -= OnMoveCanceled; }
            if (_actLook != null) { _actLook.performed -= OnLookPerformed; _actLook.canceled -= OnLookCanceled; }
            if (_actJump != null) { _actJump.started -= OnJumpStarted; _actJump.canceled -= OnJumpCanceled; }
            if (_actGlide != null) { _actGlide.performed -= OnGlidePerformed; _actGlide.canceled -= OnGlideCanceled; }
            if (_actSprint != null) { _actSprint.performed -= OnSprintPerformed; _actSprint.canceled -= OnSprintCanceled; }
            if (_actCrawl != null) { _actCrawl.performed -= OnCrawlPerformed; _actCrawl.canceled -= OnCrawlCanceled; }
            if (_actClimb != null) { _actClimb.performed -= OnClimbPerformed; _actClimb.canceled -= OnClimbCanceled; }
            if (_actInteract != null) { _actInteract.performed -= OnInteractPerformed; _actInteract.canceled -= OnInteractCanceled; }
            if (_actThrow != null) { _actThrow.performed -= OnThrowPerformed; _actThrow.canceled -= OnThrowCanceled; }
            if (_actMenu != null) { _actMenu.performed -= OnMenuPerformed; _actMenu.canceled -= OnMenuCanceled; }
            if (_actMapInv != null) { _actMapInv.performed -= OnMapInventoryPerformed; _actMapInv.canceled -= OnMapInventoryCanceled; }
            if (_actCamouflage != null) { _actCamouflage.performed -= OnCamouflagePerformed; _actCamouflage.canceled -= OnCamouflageCanceled; }
            if (_actInv1 != null) { _actInv1.performed -= OnInv1Performed; _actInv1.canceled -= OnInv1Canceled; }
            if (_actInv2 != null) { _actInv2.performed -= OnInv2Performed; _actInv2.canceled -= OnInv2Canceled; }
            if (_actInv3 != null) { _actInv3.performed -= OnInv3Performed; _actInv3.canceled -= OnInv3Canceled; }

            _map = null;
            _actMove = _actLook = _actJump = _actGlide = _actSprint = _actCrawl = _actClimb =
            _actInteract = _actThrow = _actMenu = _actMapInv = _actCamouflage = _actInv1 = _actInv2 = _actInv3 = null;

            // Reset intents
            Move = Look = Vector2.zero;
            IsMoving = false;
            JumpHeld = GlideHeld = SprintHeld = CrawlHeld = ClimbHeld = false;
            InteractHeld = ThrowHeld = MenuHeld = MapInventoryHeld = CamouflageHeld = false;
            Inv1Held = Inv2Held = Inv3Held = false;
            _jumpSupersededByGlide = false;
        }

        // ----------------------------- Handlers ------------------------------
        private static void OnMoveStarted(InputAction.CallbackContext _) => IsMoving = true;

        private static void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            Move = ctx.ReadValue<Vector2>();
            IsMoving = Move.sqrMagnitude > 0.0001f;
        }

        private static void OnMoveCanceled(InputAction.CallbackContext _)
        {
            Move = Vector2.zero;
            IsMoving = false;
        }

        private static void OnLookPerformed(InputAction.CallbackContext ctx)
        {
            Look = ctx.ReadValue<Vector2>();
        }

        private static void OnLookCanceled(InputAction.CallbackContext _)
        {
            Look = Vector2.zero;
        }

        // Jump: fire on key down; release on key up — but skip release if Glide took over
        private static void OnJumpStarted(InputAction.CallbackContext _)
        {
            JumpHeld = true;
            _jumpSupersededByGlide = false; // fresh press
            OnJumpPressed?.Invoke();
        }

        private static void OnJumpCanceled(InputAction.CallbackContext _)
        {
            if (JumpHeld)
            {
                JumpHeld = false;
                if (!_jumpSupersededByGlide)
                    OnJumpReleased?.Invoke();
            }
        }

        private static void OnGlidePerformed(InputAction.CallbackContext _)
        {
            GlideHeld = true;
            _jumpSupersededByGlide = true; // this press became a glide
            OnGlidePressed?.Invoke();
        }

        private static void OnGlideCanceled(InputAction.CallbackContext _)
        {
            if (GlideHeld)
            {
                GlideHeld = false;
                OnGlideReleased?.Invoke();
            }
        }

        private static void OnSprintPerformed(InputAction.CallbackContext _)
        {
            SprintHeld = true;
            OnSprintPressed?.Invoke();
        }

        private static void OnSprintCanceled(InputAction.CallbackContext _)
        {
            if (SprintHeld)
            {
                SprintHeld = false;
                OnSprintReleased?.Invoke();
            }
        }

        private static void OnCrawlPerformed(InputAction.CallbackContext _)
        {
            CrawlHeld = true;
            OnCrawlPressed?.Invoke();
        }

        private static void OnCrawlCanceled(InputAction.CallbackContext _)
        {
            if (CrawlHeld)
            {
                CrawlHeld = false;
                OnCrawlReleased?.Invoke();
            }
        }

        private static void OnClimbPerformed(InputAction.CallbackContext _)
        {
            ClimbHeld = true;
            OnClimbPressed?.Invoke();
        }

        private static void OnClimbCanceled(InputAction.CallbackContext _)
        {
            if (ClimbHeld)
            {
                ClimbHeld = false;
                OnClimbReleased?.Invoke();
            }
        }

        private static void OnInteractPerformed(InputAction.CallbackContext _)
        {
            InteractHeld = true;
            OnInteractPressed?.Invoke();
        }

        private static void OnInteractCanceled(InputAction.CallbackContext _)
        {
            if (InteractHeld)
            {
                InteractHeld = false;
                OnInteractReleased?.Invoke();
            }
        }

        private static void OnThrowPerformed(InputAction.CallbackContext _)
        {
            ThrowHeld = true;
            OnThrowPressed?.Invoke();
        }

        private static void OnThrowCanceled(InputAction.CallbackContext _)
        {
            if (ThrowHeld)
            {
                ThrowHeld = false;
                OnThrowReleased?.Invoke();
            }
        }

        private static void OnMenuPerformed(InputAction.CallbackContext _)
        {
            MenuHeld = true;
            OnMenuPressed?.Invoke();
        }

        private static void OnMenuCanceled(InputAction.CallbackContext _)
        {
            if (MenuHeld)
            {
                MenuHeld = false;
                OnMenuReleased?.Invoke();
            }
        }

        private static void OnMapInventoryPerformed(InputAction.CallbackContext _)
        {
            MapInventoryHeld = true;
            OnMapInventoryPressed?.Invoke();
        }

        private static void OnMapInventoryCanceled(InputAction.CallbackContext _)
        {
            if (MapInventoryHeld)
            {
                MapInventoryHeld = false;
                OnMapInventoryReleased?.Invoke();
            }
        }

        private static void OnCamouflagePerformed(InputAction.CallbackContext _)
        {
            CamouflageHeld = true;
            OnCamouflagePressed?.Invoke();
        }

        private static void OnCamouflageCanceled(InputAction.CallbackContext _)
        {
            if (CamouflageHeld)
            {
                CamouflageHeld = false;
                OnCamouflageReleased?.Invoke();
            }
        }

        private static void OnInv1Performed(InputAction.CallbackContext _)
        {
            Inv1Held = true;
            OnInventorySlotOnePressed?.Invoke();
        }

        private static void OnInv1Canceled(InputAction.CallbackContext _)
        {
            if (Inv1Held)
            {
                Inv1Held = false;
                OnInventorySlotOneReleased?.Invoke();
            }
        }

        private static void OnInv2Performed(InputAction.CallbackContext _)
        {
            Inv2Held = true;
            OnInventorySlotTwoPressed?.Invoke();
        }

        private static void OnInv2Canceled(InputAction.CallbackContext _)
        {
            if (Inv2Held)
            {
                Inv2Held = false;
                OnInventorySlotTwoReleased?.Invoke();
            }
        }

        private static void OnInv3Performed(InputAction.CallbackContext _)
        {
            Inv3Held = true;
            OnInventorySlotThreePressed?.Invoke();
        }

        private static void OnInv3Canceled(InputAction.CallbackContext _)
        {
            if (Inv3Held)
            {
                Inv3Held = false;
                OnInventorySlotThreeReleased?.Invoke();
            }
        }
    }
}
