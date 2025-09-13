using System;
using UnityEngine;
using UnityEngine.InputSystem;
using MothHunt.Input;

public class InputProbe : MonoBehaviour
{
    [Header("Drag your .inputactions asset")]
    [SerializeField] private InputActionAsset asset;

    private InputActionMap _player;
    private bool _bound;

    // Stored delegates so -= actually removes them
    private Action _onJumpP, _onJumpR;
    private Action _onSprintP, _onSprintR;
    private Action _onCrawlP, _onCrawlR;
    private Action _onClimbP, _onClimbR;
    private Action _onGlideP, _onGlideR;

    void OnEnable()
    {
        if (asset == null)
        {
            Debug.LogWarning("[InputProbe] No InputActionAsset assigned.");
            return;
        }

        _player = asset.FindActionMap("Player", throwIfNotFound: true);
        _player.Enable();

        PlayerInputRouter.Bind(_player);
        _bound = true;

        // Prepare delegates
        _onJumpP = () => Debug.Log("Jump PRESSED");
        _onJumpR = () => Debug.Log("Jump RELEASED");
        _onSprintP = () => Debug.Log("Sprint PRESSED");
        _onSprintR = () => Debug.Log("Sprint RELEASED");
        _onCrawlP = () => Debug.Log("Crawl PRESSED");
        _onCrawlR = () => Debug.Log("Crawl RELEASED");
        _onClimbP = () => Debug.Log("Climb PRESSED");
        _onClimbR = () => Debug.Log("Climb RELEASED");
        _onGlideP = () => Debug.Log("Glide PRESSED");
        _onGlideR = () => Debug.Log("Glide RELEASED");

        // Subscribe
        PlayerInputRouter.OnJumpPressed += _onJumpP;
        PlayerInputRouter.OnJumpReleased += _onJumpR;
        PlayerInputRouter.OnSprintPressed += _onSprintP;
        PlayerInputRouter.OnSprintReleased += _onSprintR;
        PlayerInputRouter.OnCrawlPressed += _onCrawlP;
        PlayerInputRouter.OnCrawlReleased += _onCrawlR;
        PlayerInputRouter.OnClimbPressed += _onClimbP;
        PlayerInputRouter.OnClimbReleased += _onClimbR;
        PlayerInputRouter.OnGlidePressed += _onGlideP;
        PlayerInputRouter.OnGlideReleased += _onGlideR;

        Debug.Log("[InputProbe] Bound to Player map and subscribed to router events.");
    }

    void OnDisable()
    {
        if (_bound)
        {
            // Unsubscribe
            PlayerInputRouter.OnJumpPressed -= _onJumpP;
            PlayerInputRouter.OnJumpReleased -= _onJumpR;
            PlayerInputRouter.OnSprintPressed -= _onSprintP;
            PlayerInputRouter.OnSprintReleased -= _onSprintR;
            PlayerInputRouter.OnCrawlPressed -= _onCrawlP;
            PlayerInputRouter.OnCrawlReleased -= _onCrawlR;
            PlayerInputRouter.OnClimbPressed -= _onClimbP;
            PlayerInputRouter.OnClimbReleased -= _onClimbR;
            PlayerInputRouter.OnGlidePressed -= _onGlideP;
            PlayerInputRouter.OnGlideReleased -= _onGlideR;

            PlayerInputRouter.Unbind();
            _bound = false;
        }

        if (_player != null)
            _player.Disable();

        Debug.Log("[InputProbe] Unbound and unsubscribed.");
    }

    void Update()
    {
        // Compact snapshot of all intent flags each frame
        Debug.Log(
            $"Move={PlayerInputRouter.Move} " +
            $"IsMoving={PlayerInputRouter.IsMoving} " +
            $"JumpHeld={PlayerInputRouter.JumpHeld} " +
            $"SprintHeld={PlayerInputRouter.SprintHeld} " +
            $"CrawlHeld={PlayerInputRouter.CrawlHeld} " +
            $"ClimbHeld={PlayerInputRouter.ClimbHeld} " +
            $"GlideHeld={PlayerInputRouter.GlideHeld} " +
            $"AllFoursIntent={PlayerInputRouter.AllFoursIntent}"
        );
    }
}
