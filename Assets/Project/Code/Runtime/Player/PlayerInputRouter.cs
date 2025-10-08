using System;                                   // Brings in base .NET types (e.g., Action, EventHandler, etc.)
using UnityEngine;                              // Unity core API (Vector2, MonoBehaviour types if needed, etc.)
using UnityEngine.InputSystem;                  // Unity Input System (InputAction, InputActionMap, CallbackContext, etc.)

namespace MothHunt.Input                          // Project namespace to avoid name collisions and group input code
{
    /// <summary>
    /// Centralized input facade. Gameplay never talks to InputSystem directly.
    /// Reads from one action map (usually "Player") and exposes intent values + edge events.
    /// </summary>
    public static class PlayerInputRouter        // Static = single global router; not instantiated; holds state/events for input
    {
        // ---------------- Values (INTENT, not gameplay state) ----------------
        public static Vector2 Move { get; private set; }   // Last movement input vector (e.g., WASD/gamepad stick)
        public static Vector2 Look { get; private set; }   // Last look input vector (e.g., mouse/stick for camera)
        public static bool IsMoving { get; private set; }  // Convenience flag: true when Move is non-zero

        public static bool JumpHeld { get; private set; }        // True while jump input is held
        public static bool GlideHeld { get; private set; }       // True while glide input is held (often same control with hold)
        public static bool SprintHeld { get; private set; }      // True while sprint input is held
        public static bool CrawlHeld { get; private set; }       // True while crawl/crouch input is held (fallback to Crouch)
        public static bool ClimbHeld { get; private set; }       // True while climb input is held
        public static bool InteractHeld { get; private set; }    // True while interact input is held
        public static bool ThrowHeld { get; private set; }       // True while throw input is held
        public static bool MenuHeld { get; private set; }        // True while menu input is held
        public static bool MapInventoryHeld { get; private set; }// True while map/inventory input is held
        public static bool CamouflageHeld { get; private set; }  // True while camouflage input is held
        public static bool Inv1Held { get; private set; }        // True while inventory slot 1 held
        public static bool Inv2Held { get; private set; }        // True while inventory slot 2 held
        public static bool Inv3Held { get; private set; }        // True while inventory slot 3 held
        public static bool DropChord => (Move.y <= -0.5f) || CrawlHeld;

        /// <summary>True while player intends an all-fours pose (sprint or crawl held).</summary>
        public static bool AllFoursIntent => SprintHeld || CrawlHeld;  // Derived convenience flag

        // ---------- NEW: simple edge queries we need for Throw & cancels ----------
        public static bool ThrowPressedThisFrame => _actThrow != null && _actThrow.WasPressedThisFrame();
        public static bool ClimbPressedThisFrame => _actClimb != null && _actClimb.WasPressedThisFrame();

        // ---------- NEW: motor sets this each frame so everyone agrees on grounding ----------
        public static bool IsGrounded { get; set; }

        // --------------------------- Events (edges) ---------------------------
        public static event Action OnJumpPressed;               // Fired on jump press edge
        public static event Action OnJumpReleased;              // Fired on jump release edge

        public static event Action OnGlidePressed;              // Fired when glide starts (e.g., hold threshold met)
        public static event Action OnGlideReleased;             // Fired when glide ends

        public static event Action OnSprintPressed;             // Fired on sprint press edge
        public static event Action OnSprintReleased;            // Fired on sprint release edge

        public static event Action OnCrawlPressed;              // Fired on crawl press edge
        public static event Action OnCrawlReleased;             // Fired on crawl release edge

        public static event Action OnClimbPressed;              // Fired on climb press edge
        public static event Action OnClimbReleased;             // Fired on climb release edge

        public static event Action OnInteractPressed;           // Fired on interact press edge
        public static event Action OnInteractReleased;          // Fired on interact release edge

        public static event Action OnThrowPressed;              // Fired on throw press edge
        public static event Action OnThrowReleased;             // Fired on throw release edge

        public static event Action OnMenuPressed;               // Fired on menu press edge
        public static event Action OnMenuReleased;              // Fired on menu release edge

        public static event Action OnMapInventoryPressed;       // Fired on map/inventory press edge
        public static event Action OnMapInventoryReleased;      // Fired on map/inventory release edge

        public static event Action OnCamouflagePressed;         // Fired on camouflage press edge
        public static event Action OnCamouflageReleased;        // Fired on camouflage release edge

        public static event Action OnInventorySlotOnePressed;   // Fired on inventory slot 1 press
        public static event Action OnInventorySlotOneReleased;  // Fired on inventory slot 1 release

        public static event Action OnInventorySlotTwoPressed;   // Fired on inventory slot 2 press
        public static event Action OnInventorySlotTwoReleased;  // Fired on inventory slot 2 release

        public static event Action OnInventorySlotThreePressed; // Fired on inventory slot 3 press
        public static event Action OnInventorySlotThreeReleased;// Fired on inventory slot 3 release

        // -------------------------- Internal wiring --------------------------
        private const string ACTION_MOVE = "Move";                    // Name of action in the InputActionMap
        private const string ACTION_LOOK = "Look";                    // Name of look action
        private const string ACTION_JUMP = "Jump";                    // Name of jump action
        private const string ACTION_GLIDE = "Glide";                  // Name of glide action
        private const string ACTION_SPRINT = "Sprint";                // Name of sprint action
        private const string ACTION_CRAWL = "Crawl";                  // Name of crawl action
        private const string ACTION_CROUCH = "Crouch";                // Fallback name if older asset used "Crouch"
        private const string ACTION_CLIMB = "Climb";                  // Name of climb action
        private const string ACTION_INTERACT = "Interact";            // Name of interact action
        private const string ACTION_THROW = "Throw";                  // Name of throw action
        private const string ACTION_MENU = "Menu";                    // Name of menu action
        private const string ACTION_MAP_INVENTORY = "Map/Inventory";  // Name of map/inventory action
        private const string ACTION_CAMOUFLAGE = "Camouflage";        // Name of camouflage action
        private const string ACTION_INV_SLOT_ONE = "Inventory slot one";   // Name of inv slot 1
        private const string ACTION_INV_SLOT_TWO = "Inventory slot two";   // Name of inv slot 2
        private const string ACTION_INV_SLOT_THREE = "Inventory slot three";// Name of inv slot 3

        private static bool _bound;                             // Guard to prevent double-binding (duplicate subscriptions)
        private static bool _jumpSupersededByGlide;             // Flag to suppress JumpReleased if Glide took over

        public static bool InteractPressedThisFrame
            => _actInteract != null && _actInteract.WasPressedThisFrame();

        private static InputActionMap _map;                     // Cached reference to the bound InputActionMap (e.g., "Player")

        // Cached actions (may be null if not present in the asset)
        private static InputAction                                // Each field caches a single InputAction for quick access
            _actMove, _actLook, _actJump, _actGlide, _actSprint, _actCrawl, _actClimb,
            _actInteract, _actThrow, _actMenu, _actMapInv, _actCamouflage,
            _actInv1, _actInv2, _actInv3;

        /// <summary>Preferred entry point: bind using the generated wrapper.</summary>
        public static void Bind(MothHuntInput.PlayerActions actions)   // Overload that accepts the generated wrapper's PlayerActions
        {
            if (_bound || actions.Equals(default)) return;             // If already bound or wrapper uninitialized, do nothing
            Bind(actions.Get());                                       // Extract underlying InputActionMap and delegate to other Bind
        }

        /// <summary>Fallback: bind using a raw action map (usually the "Player" map).</summary>
        public static void Bind(InputActionMap map)                    // Overload that accepts a raw InputActionMap
        {
            if (_bound || map == null) return;                         // Prevent double-binding; ensure map is valid
            _bound = true;                                             // Mark as bound so we don't subscribe twice
            _map = map;                                                // Cache the map for unbinding and reference

            // Resolve actions by name
            _actMove = _map.FindAction(ACTION_MOVE, throwIfNotFound: false);       // Look up "Move"; null if missing
            _actLook = _map.FindAction(ACTION_LOOK, throwIfNotFound: false);       // Look up "Look"
            _actJump = _map.FindAction(ACTION_JUMP, throwIfNotFound: false);       // Look up "Jump"
            _actGlide = _map.FindAction(ACTION_GLIDE, throwIfNotFound: false);     // Look up "Glide"
            _actSprint = _map.FindAction(ACTION_SPRINT, throwIfNotFound: false);   // Look up "Sprint"
            _actCrawl = _map.FindAction(ACTION_CRAWL, throwIfNotFound: false)      // Try "Crawl" first
                         ?? _map.FindAction(ACTION_CROUCH, throwIfNotFound: false); // Fallback to "Crouch" if "Crawl" not present
            _actClimb = _map.FindAction(ACTION_CLIMB, throwIfNotFound: false);     // Look up "Climb"
            _actInteract = _map.FindAction(ACTION_INTERACT, throwIfNotFound: false);// Look up "Interact"
            _actThrow = _map.FindAction(ACTION_THROW, throwIfNotFound: false);     // Look up "Throw"
            _actMenu = _map.FindAction(ACTION_MENU, throwIfNotFound: false);       // Look up "Menu"
            _actMapInv = _map.FindAction(ACTION_MAP_INVENTORY, throwIfNotFound: false); // Look up "Map/Inventory"
            _actCamouflage = _map.FindAction(ACTION_CAMOUFLAGE, throwIfNotFound: false);// Look up "Camouflage"
            _actInv1 = _map.FindAction(ACTION_INV_SLOT_ONE, throwIfNotFound: false);    // Look up "Inventory slot one"
            _actInv2 = _map.FindAction(ACTION_INV_SLOT_TWO, throwIfNotFound: false);    // Look up "Inventory slot two"
            _actInv3 = _map.FindAction(ACTION_INV_SLOT_THREE, throwIfNotFound: false);  // Look up "Inventory slot three"

            // Subscribe if present
            if (_actMove != null) { _actMove.started += OnMoveStarted; _actMove.performed += OnMovePerformed; _actMove.canceled += OnMoveCanceled; }
            if (_actLook != null) { _actLook.performed += OnLookPerformed; _actLook.canceled += OnLookCanceled; }
            if (_actJump != null) { _actJump.started += OnJumpStarted; _actJump.canceled += OnJumpCanceled; }
            if (_actGlide != null) { _actGlide.performed += OnGlidePerformed; _actGlide.canceled += OnGlideCanceled; }
            if (_actSprint != null) { _actSprint.performed += OnSprintPerformed; _actSprint.canceled += OnSprintCanceled; }
            if (_actCrawl != null) { _actCrawl.performed += OnCrawlPerformed; _actCrawl.canceled += OnCrawlCanceled; }
            if (_actClimb != null) { _actClimb.performed += OnClimbPerformed; _actClimb.canceled += OnClimbCanceled; }
            if (_actInteract != null) { _actInteract.performed += OnInteractPerformed; _actInteract.canceled += OnInteractCanceled; }
            if (_actThrow != null) { _actThrow.performed += OnThrowPerformed; _actThrow.canceled += OnThrowCanceled; }
            if (_actMenu != null) { _actMenu.performed += OnMenuPerformed; _actMenu.canceled += OnMenuCanceled; }
            if (_actMapInv != null) { _actMapInv.performed += OnMapInventoryPerformed; _actMapInv.canceled += OnMapInventoryCanceled; }
            if (_actCamouflage != null) { _actCamouflage.performed += OnCamouflagePerformed; _actCamouflage.canceled += OnCamouflageCanceled; }
            if (_actInv1 != null) { _actInv1.performed += OnInv1Performed; _actInv1.canceled += OnInv1Canceled; }
            if (_actInv2 != null) { _actInv2.performed += OnInv2Performed; _actInv2.canceled += OnInv2Canceled; }
            if (_actInv3 != null) { _actInv3.performed += OnInv3Performed; _actInv3.canceled += OnInv3Canceled; }
        }

        /// <summary>Unbind all handlers and clear cached intent values.</summary>
        public static void Unbind()                                    // Call when unloading or switching maps to clean up
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
            IsGrounded = false;
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

        private static void OnLookPerformed(InputAction.CallbackContext ctx) => Look = ctx.ReadValue<Vector2>();
        private static void OnLookCanceled(InputAction.CallbackContext _) => Look = Vector2.zero;

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
            _jumpSupersededByGlide = true;   // this press became a glide
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

        private static void OnSprintPerformed(InputAction.CallbackContext _) { SprintHeld = true; OnSprintPressed?.Invoke(); }
        private static void OnSprintCanceled(InputAction.CallbackContext _) { if (SprintHeld) { SprintHeld = false; OnSprintReleased?.Invoke(); } }

        private static void OnCrawlPerformed(InputAction.CallbackContext _) { CrawlHeld = true; OnCrawlPressed?.Invoke(); }
        private static void OnCrawlCanceled(InputAction.CallbackContext _) { if (CrawlHeld) { CrawlHeld = false; OnCrawlReleased?.Invoke(); } }

        private static void OnClimbPerformed(InputAction.CallbackContext _) { ClimbHeld = true; OnClimbPressed?.Invoke(); }
        private static void OnClimbCanceled(InputAction.CallbackContext _) { if (ClimbHeld) { ClimbHeld = false; OnClimbReleased?.Invoke(); } }

        private static void OnInteractPerformed(InputAction.CallbackContext _) { InteractHeld = true; OnInteractPressed?.Invoke(); }
        private static void OnInteractCanceled(InputAction.CallbackContext _) { if (InteractHeld) { InteractHeld = false; OnInteractReleased?.Invoke(); } }

        private static void OnThrowPerformed(InputAction.CallbackContext _) { ThrowHeld = true; OnThrowPressed?.Invoke(); }
        private static void OnThrowCanceled(InputAction.CallbackContext _) { if (ThrowHeld) { ThrowHeld = false; OnThrowReleased?.Invoke(); } }

        private static void OnMenuPerformed(InputAction.CallbackContext _) { MenuHeld = true; OnMenuPressed?.Invoke(); }
        private static void OnMenuCanceled(InputAction.CallbackContext _) { if (MenuHeld) { MenuHeld = false; OnMenuReleased?.Invoke(); } }

        private static void OnMapInventoryPerformed(InputAction.CallbackContext _) { MapInventoryHeld = true; OnMapInventoryPressed?.Invoke(); }
        private static void OnMapInventoryCanceled(InputAction.CallbackContext _) { if (MapInventoryHeld) { MapInventoryHeld = false; OnMapInventoryReleased?.Invoke(); } }

        private static void OnCamouflagePerformed(InputAction.CallbackContext _) { CamouflageHeld = true; OnCamouflagePressed?.Invoke(); }
        private static void OnCamouflageCanceled(InputAction.CallbackContext _) { if (CamouflageHeld) { CamouflageHeld = false; OnCamouflageReleased?.Invoke(); } }

        private static void OnInv1Performed(InputAction.CallbackContext _) { Inv1Held = true; OnInventorySlotOnePressed?.Invoke(); }
        private static void OnInv1Canceled(InputAction.CallbackContext _) { if (Inv1Held) { Inv1Held = false; OnInventorySlotOneReleased?.Invoke(); } }

        private static void OnInv2Performed(InputAction.CallbackContext _) { Inv2Held = true; OnInventorySlotTwoPressed?.Invoke(); }
        private static void OnInv2Canceled(InputAction.CallbackContext _) { if (Inv2Held) { Inv2Held = false; OnInventorySlotTwoReleased?.Invoke(); } }

        private static void OnInv3Performed(InputAction.CallbackContext _) { Inv3Held = true; OnInventorySlotThreePressed?.Invoke(); }
        private static void OnInv3Canceled(InputAction.CallbackContext _) { if (Inv3Held) { Inv3Held = false; OnInventorySlotThreeReleased?.Invoke(); } }
    }
}
