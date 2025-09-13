using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MothHunt.Input
{
    /// <summary>Centralized input facade. Gameplay never talks to InputSystem directly.</summary>
    public static class PlayerInputRouter
    {
        // ---------------- Values (INTENT, not gameplay state) ----------------
        public static Vector2 Move { get; private set; }
        public static bool IsMoving { get; private set; }

        public static bool SprintHeld { get; private set; }
        public static bool CrawlHeld { get; private set; } // maps to Crawl or (fallback) Crouch
        public static bool ClimbHeld { get; private set; }
        public static bool GlideHeld { get; private set; }
        public static bool JumpHeld { get; private set; }

        /// <summary>True while player intends an all-fours pose (sprint or crawl held).</summary>
        public static bool AllFoursIntent => SprintHeld || CrawlHeld;

        // --------------------------- Events (edges) ---------------------------
        public static event Action OnJumpPressed;
        public static event Action OnJumpReleased;

        public static event Action OnSprintPressed;
        public static event Action OnSprintReleased;

        public static event Action OnCrawlPressed;
        public static event Action OnCrawlReleased;

        public static event Action OnClimbPressed;
        public static event Action OnClimbReleased;

        public static event Action OnGlidePressed;
        public static event Action OnGlideReleased;

        // WHERE WE LEFT OFF

        // -------------------------- Internal wiring --------------------------
        // Single source of truth for action names
        private const string ACTION_MOVE = "Move";
        private const string ACTION_JUMP = "Jump";
        private const string ACTION_SPRINT = "Sprint";
        private const string ACTION_CRAWL = "Crawl";
        private const string ACTION_CROUCH = "Crouch"; // fallback (older asset)
        private const string ACTION_CLIMB = "Climb";
        private const string ACTION_GLIDE = "Glide";

        private static bool _bound;
        private static InputActionMap _map;

        // Cached actions (may be null if not present in the asset)
        private static InputAction _actMove, _actJump, _actSprint, _actCrawl, _actClimb, _actGlide;

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
            _actJump = _map.FindAction(ACTION_JUMP, throwIfNotFound: false);
            _actSprint = _map.FindAction(ACTION_SPRINT, throwIfNotFound: false);
            _actCrawl = _map.FindAction(ACTION_CRAWL, throwIfNotFound: false)
                      ?? _map.FindAction(ACTION_CROUCH, throwIfNotFound: false); // fallback
            _actClimb = _map.FindAction(ACTION_CLIMB, throwIfNotFound: false);
            _actGlide = _map.FindAction(ACTION_GLIDE, throwIfNotFound: false);

            // Subscribe if present
            if (_actMove != null)
            {
                _actMove.started += OnMoveStarted;
                _actMove.performed += OnMovePerformed;
                _actMove.canceled += OnMoveCanceled;
            }

            if (_actJump != null)
            {
                _actJump.performed += OnJumpPerformed;
                _actJump.canceled += OnJumpCanceled;
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

            if (_actGlide != null)
            {
                _actGlide.performed += OnGlidePerformed;
                _actGlide.canceled += OnGlideCanceled;
            }
        }

        /// <summary>Unbind all handlers and clear cached intent values.</summary>
        public static void Unbind()
        {
            if (!_bound) return;
            _bound = false;

            if (_actMove != null) { _actMove.started -= OnMoveStarted; _actMove.performed -= OnMovePerformed; _actMove.canceled -= OnMoveCanceled; }
            if (_actJump != null) { _actJump.performed -= OnJumpPerformed; _actJump.canceled -= OnJumpCanceled; }
            if (_actSprint != null) { _actSprint.performed -= OnSprintPerformed; _actSprint.canceled -= OnSprintCanceled; }
            if (_actCrawl != null) { _actCrawl.performed -= OnCrawlPerformed; _actCrawl.canceled -= OnCrawlCanceled; }
            if (_actClimb != null) { _actClimb.performed -= OnClimbPerformed; _actClimb.canceled -= OnClimbCanceled; }
            if (_actGlide != null) { _actGlide.performed -= OnGlidePerformed; _actGlide.canceled -= OnGlideCanceled; }

            _map = null;
            _actMove = _actJump = _actSprint = _actCrawl = _actClimb = _actGlide = null;

            // Reset intents
            Move = Vector2.zero;
            IsMoving = false;
            SprintHeld = CrawlHeld = ClimbHeld = GlideHeld = JumpHeld = false;
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

        private static void OnJumpPerformed(InputAction.CallbackContext _)
        {
            JumpHeld = true;
            OnJumpPressed?.Invoke();
        }

        private static void OnJumpCanceled(InputAction.CallbackContext _)
        {
            JumpHeld = false;
            OnJumpReleased?.Invoke();
        }

        private static void OnSprintPerformed(InputAction.CallbackContext _)
        {
            SprintHeld = true;
            OnSprintPressed?.Invoke();
        }

        private static void OnSprintCanceled(InputAction.CallbackContext _)
        {
            SprintHeld = false;
            OnSprintReleased?.Invoke();
        }

        private static void OnCrawlPerformed(InputAction.CallbackContext _)
        {
            CrawlHeld = true;
            OnCrawlPressed?.Invoke();
        }

        private static void OnCrawlCanceled(InputAction.CallbackContext _)
        {
            CrawlHeld = false;
            OnCrawlReleased?.Invoke();
        }

        private static void OnClimbPerformed(InputAction.CallbackContext _)
        {
            ClimbHeld = true;
            OnClimbPressed?.Invoke();
        }

        private static void OnClimbCanceled(InputAction.CallbackContext _)
        {
            ClimbHeld = false;
            OnClimbReleased?.Invoke();
        }

        private static void OnGlidePerformed(InputAction.CallbackContext _)
        {
            GlideHeld = true;
            OnGlidePressed?.Invoke();
        }

        private static void OnGlideCanceled(InputAction.CallbackContext _)
        {
            GlideHeld = false;
            OnGlideReleased?.Invoke();
        }
    }

}
