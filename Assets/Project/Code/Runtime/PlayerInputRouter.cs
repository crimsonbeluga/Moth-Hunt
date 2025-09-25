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

        /// <summary>True while player intends an all-fours pose (sprint or crawl held).</summary>
        public static bool AllFoursIntent => SprintHeld || CrawlHeld;  // Derived convenience flag

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
            Bind(actions.Get()); // explicit, avoids ambiguous implicit cast // Extract underlying InputActionMap and delegate to other Bind
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
            if (_actMove != null)                                      // Only wire handlers if the action exists
            {
                _actMove.started += OnMoveStarted;                     // Start edge: input begins actuating (crosses threshold)
                _actMove.performed += OnMovePerformed;                 // Performed: value updates while active (Vector2 changes)
                _actMove.canceled += OnMoveCanceled;                   // Canceled: input returns to neutral/released
            }

            if (_actLook != null)
            {
                _actLook.performed += OnLookPerformed;                 // Look value updated
                _actLook.canceled += OnLookCanceled;                   // Look ends (e.g., stick centered)
            }

            if (_actJump != null)
            {
                // Jump fires on key DOWN; release on key UP (Tap removed from binding)
                _actJump.started += OnJumpStarted;                     // Jump press edge
                _actJump.canceled += OnJumpCanceled;                   // Jump release edge (unless superseded by Glide)
            }

            if (_actGlide != null)
            {
                _actGlide.performed += OnGlidePerformed;               // Glide engaged (e.g., hold interaction min time passed)
                _actGlide.canceled += OnGlideCanceled;                 // Glide ended (button released or interaction canceled)
            }

            if (_actSprint != null)
            {
                _actSprint.performed += OnSprintPerformed;             // Sprint pressed/engaged
                _actSprint.canceled += OnSprintCanceled;               // Sprint released
            }

            if (_actCrawl != null)
            {
                _actCrawl.performed += OnCrawlPerformed;               // Crawl pressed/engaged
                _actCrawl.canceled += OnCrawlCanceled;                 // Crawl released
            }

            if (_actClimb != null)
            {
                _actClimb.performed += OnClimbPerformed;               // Climb pressed/engaged
                _actClimb.canceled += OnClimbCanceled;                 // Climb released
            }

            if (_actInteract != null)
            {
                _actInteract.performed += OnInteractPerformed;         // Interact pressed/engaged
                _actInteract.canceled += OnInteractCanceled;           // Interact released
            }

            if (_actThrow != null)
            {
                _actThrow.performed += OnThrowPerformed;               // Throw pressed/engaged
                _actThrow.canceled += OnThrowCanceled;                 // Throw released
            }

            if (_actMenu != null)
            {
                _actMenu.performed += OnMenuPerformed;                 // Menu pressed/engaged
                _actMenu.canceled += OnMenuCanceled;                   // Menu released
            }

            if (_actMapInv != null)
            {
                _actMapInv.performed += OnMapInventoryPerformed;       // Map/Inventory pressed/engaged
                _actMapInv.canceled += OnMapInventoryCanceled;         // Map/Inventory released
            }

            if (_actCamouflage != null)
            {
                _actCamouflage.performed += OnCamouflagePerformed;     // Camouflage pressed/engaged
                _actCamouflage.canceled += OnCamouflageCanceled;       // Camouflage released
            }

            if (_actInv1 != null)
            {
                _actInv1.performed += OnInv1Performed;                 // Inventory slot 1 pressed
                _actInv1.canceled += OnInv1Canceled;                   // Inventory slot 1 released
            }
            if (_actInv2 != null)
            {
                _actInv2.performed += OnInv2Performed;                 // Inventory slot 2 pressed
                _actInv2.canceled += OnInv2Canceled;                   // Inventory slot 2 released
            }
            if (_actInv3 != null)
            {
                _actInv3.performed += OnInv3Performed;                 // Inventory slot 3 pressed
                _actInv3.canceled += OnInv3Canceled;                   // Inventory slot 3 released
            }
        }

        /// <summary>Unbind all handlers and clear cached intent values.</summary>
        public static void Unbind()                                    // Call when unloading or switching maps to clean up
        {
            if (!_bound) return;                                       // If not currently bound, nothing to do
            _bound = false;                                            // Mark unbound

            // For every non-null action, remove the exact handlers we added during Bind
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

            _map = null;                                               // Clear cached map reference
            _actMove = _actLook = _actJump = _actGlide = _actSprint = _actCrawl = _actClimb =
            _actInteract = _actThrow = _actMenu = _actMapInv = _actCamouflage = _actInv1 = _actInv2 = _actInv3 = null; // Null all action refs

            // Reset intents
            Move = Look = Vector2.zero;                                // Clear last vectors
            IsMoving = false;                                          // Not moving after unbind
            JumpHeld = GlideHeld = SprintHeld = CrawlHeld = ClimbHeld = false; // Clear held states
            InteractHeld = ThrowHeld = MenuHeld = MapInventoryHeld = CamouflageHeld = false; // Clear held states
            Inv1Held = Inv2Held = Inv3Held = false;                    // Clear inventory holds
            _jumpSupersededByGlide = false;                            // Reset jump/glide relation flag
        }

        // ----------------------------- Handlers ------------------------------
        private static void OnMoveStarted(InputAction.CallbackContext _) => IsMoving = true; // Mark moving as soon as move starts

        private static void OnMovePerformed(InputAction.CallbackContext ctx) // Called when move value updates while active
        {
            Move = ctx.ReadValue<Vector2>();                         // Read current Vector2 from the event context
            IsMoving = Move.sqrMagnitude > 0.0001f;                  // True if not basically zero (cheap magnitude check)
        }

        private static void OnMoveCanceled(InputAction.CallbackContext _)   // Called when move ends (stick neutral / keys up)
        {
            Move = Vector2.zero;                                     // Clear movement vector
            IsMoving = false;                                        // No longer moving
        }

        private static void OnLookPerformed(InputAction.CallbackContext ctx) // Called when look value updates
        {
            Look = ctx.ReadValue<Vector2>();                         // Read current look delta/axis
        }

        private static void OnLookCanceled(InputAction.CallbackContext _)   // Called when look ends/neutral
        {
            Look = Vector2.zero;                                     // Clear look vector
        }

        // Jump: fire on key down; release on key up ÅEbut skip release if Glide took over
        private static void OnJumpStarted(InputAction.CallbackContext _)    // Jump press edge
        {
            JumpHeld = true;                                         // Mark jump as held
            _jumpSupersededByGlide = false; // fresh press           // Reset glide-supersede state for this press
            OnJumpPressed?.Invoke();                                 // Broadcast jump pressed to subscribers
        }

        private static void OnJumpCanceled(InputAction.CallbackContext _)   // Jump release edge
        {
            if (JumpHeld)                                            // Only if we actually considered it held
            {
                JumpHeld = false;                                    // No longer holding jump
                if (!_jumpSupersededByGlide)                         // If glide did NOT take over this press
                    OnJumpReleased?.Invoke();                        // Broadcast jump release (avoid double-release when gliding)
            }
        }

        private static void OnGlidePerformed(InputAction.CallbackContext _) // Glide engaged (e.g., hold satisfied)
        {
            GlideHeld = true;                                        // Mark glide as held
            _jumpSupersededByGlide = true; // this press became a glide // Ensure jump release is suppressed for this cycle
            OnGlidePressed?.Invoke();                                // Broadcast glide pressed
        }

        private static void OnGlideCanceled(InputAction.CallbackContext _)  // Glide ended
        {
            if (GlideHeld)                                           // Only if we considered it held
            {
                GlideHeld = false;                                   // Clear glide hold
                OnGlideReleased?.Invoke();                           // Broadcast glide release
            }
        }

        private static void OnSprintPerformed(InputAction.CallbackContext _) // Sprint engaged
        {
            SprintHeld = true;                                       // Mark sprint as held
            OnSprintPressed?.Invoke();                               // Broadcast sprint pressed
        }

        private static void OnSprintCanceled(InputAction.CallbackContext _) // Sprint ended
        {
            if (SprintHeld)                                          // Guard against duplicate cancels
            {
                SprintHeld = false;                                  // Clear sprint hold
                OnSprintReleased?.Invoke();                          // Broadcast sprint release
            }
        }

        private static void OnCrawlPerformed(InputAction.CallbackContext _) // Crawl engaged
        {
            CrawlHeld = true;                                        // Mark crawl as held
            OnCrawlPressed?.Invoke();                                // Broadcast crawl pressed
        }

        private static void OnCrawlCanceled(InputAction.CallbackContext _)  // Crawl ended
        {
            if (CrawlHeld)                                           // Guard against duplicate cancels
            {
                CrawlHeld = false;                                   // Clear crawl hold
                OnCrawlReleased?.Invoke();                           // Broadcast crawl release
            }
        }

        private static void OnClimbPerformed(InputAction.CallbackContext _) // Climb engaged
        {
            ClimbHeld = true;                                        // Mark climb as held
            OnClimbPressed?.Invoke();                                // Broadcast climb pressed
        }

        private static void OnClimbCanceled(InputAction.CallbackContext _)  // Climb ended
        {
            if (ClimbHeld)                                           // Guard against duplicate cancels
            {
                ClimbHeld = false;                                   // Clear climb hold
                OnClimbReleased?.Invoke();                           // Broadcast climb release
            }
        }

        private static void OnInteractPerformed(InputAction.CallbackContext _) // Interact engaged
        {
            InteractHeld = true;                                     // Mark interact as held
            OnInteractPressed?.Invoke();                             // Broadcast interact pressed
        }

        private static void OnInteractCanceled(InputAction.CallbackContext _) // Interact ended
        {
            if (InteractHeld)                                        // Guard against duplicate cancels
            {
                InteractHeld = false;                                // Clear interact hold
                OnInteractReleased?.Invoke();                        // Broadcast interact release
            }
        }

        private static void OnThrowPerformed(InputAction.CallbackContext _) // Throw engaged
        {
            ThrowHeld = true;                                        // Mark throw as held
            OnThrowPressed?.Invoke();                                // Broadcast throw pressed
        }

        private static void OnThrowCanceled(InputAction.CallbackContext _)  // Throw ended
        {
            if (ThrowHeld)                                           // Guard against duplicate cancels
            {
                ThrowHeld = false;                                   // Clear throw hold
                OnThrowReleased?.Invoke();                           // Broadcast throw release
            }
        }

        private static void OnMenuPerformed(InputAction.CallbackContext _)  // Menu engaged
        {
            MenuHeld = true;                                         // Mark menu as held
            OnMenuPressed?.Invoke();                                 // Broadcast menu pressed
        }

        private static void OnMenuCanceled(InputAction.CallbackContext _)   // Menu ended
        {
            if (MenuHeld)                                            // Guard against duplicate cancels
            {
                MenuHeld = false;                                    // Clear menu hold
                OnMenuReleased?.Invoke();                            // Broadcast menu release
            }
        }

        private static void OnMapInventoryPerformed(InputAction.CallbackContext _) // Map/Inventory engaged
        {
            MapInventoryHeld = true;                                 // Mark map/inventory as held
            OnMapInventoryPressed?.Invoke();                         // Broadcast map/inventory pressed
        }

        private static void OnMapInventoryCanceled(InputAction.CallbackContext _)  // Map/Inventory ended
        {
            if (MapInventoryHeld)                                    // Guard against duplicate cancels
            {
                MapInventoryHeld = false;                            // Clear map/inventory hold
                OnMapInventoryReleased?.Invoke();                    // Broadcast map/inventory release
            }
        }

        private static void OnCamouflagePerformed(InputAction.CallbackContext _)   // Camouflage engaged
        {
            CamouflageHeld = true;                                   // Mark camouflage as held
            OnCamouflagePressed?.Invoke();                           // Broadcast camouflage pressed
        }

        private static void OnCamouflageCanceled(InputAction.CallbackContext _)    // Camouflage ended
        {
            if (CamouflageHeld)                                      // Guard against duplicate cancels
            {
                CamouflageHeld = false;                              // Clear camouflage hold
                OnCamouflageReleased?.Invoke();                      // Broadcast camouflage release
            }
        }

        private static void OnInv1Performed(InputAction.CallbackContext _)         // Inventory slot 1 engaged
        {
            Inv1Held = true;                                         // Mark inv1 as held
            OnInventorySlotOnePressed?.Invoke();                     // Broadcast inv1 pressed
        }

        private static void OnInv1Canceled(InputAction.CallbackContext _)          // Inventory slot 1 ended
        {
            if (Inv1Held)                                            // Guard against duplicate cancels
            {
                Inv1Held = false;                                    // Clear inv1 hold
                OnInventorySlotOneReleased?.Invoke();                // Broadcast inv1 release
            }
        }

        private static void OnInv2Performed(InputAction.CallbackContext _)         // Inventory slot 2 engaged
        {
            Inv2Held = true;                                         // Mark inv2 as held
            OnInventorySlotTwoPressed?.Invoke();                     // Broadcast inv2 pressed
        }

        private static void OnInv2Canceled(InputAction.CallbackContext _)          // Inventory slot 2 ended
        {
            if (Inv2Held)                                            // Guard against duplicate cancels
            {
                Inv2Held = false;                                    // Clear inv2 hold
                OnInventorySlotTwoReleased?.Invoke();                // Broadcast inv2 release
            }
        }

        private static void OnInv3Performed(InputAction.CallbackContext _)         // Inventory slot 3 engaged
        {
            Inv3Held = true;                                         // Mark inv3 as held
            OnInventorySlotThreePressed?.Invoke();                   // Broadcast inv3 pressed
        }

        private static void OnInv3Canceled(InputAction.CallbackContext _)          // Inventory slot 3 ended
        {
            if (Inv3Held)                                            // Guard against duplicate cancels
            {
                Inv3Held = false;                                    // Clear inv3 hold
                OnInventorySlotThreeReleased?.Invoke();              // Broadcast inv3 release
            }
        }
    }
}
