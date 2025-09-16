using System;
using UnityEngine;
using UnityEngine.InputSystem;
using MothHunt.Input;

public class InputProbe : MonoBehaviour
{
    [Header("Drag your .inputactions asset")]
    [SerializeField] private InputActionAsset asset;

    [SerializeField] private string actionMapName = "Player";

    private InputActionMap _player;
    private bool _bound;
    private bool _subscribed;

    // Stored delegates so -= actually removes them
    private Action
        _jumpP, _jumpR,
        _glideP, _glideR,
        _sprintP, _sprintR,
        _crawlP, _crawlR,
        _climbP, _climbR,
        _interactP, _interactR,
        _throwP, _throwR,
        _menuP, _menuR,
        _mapInvP, _mapInvR,
        _camoP, _camoR,
        _inv1P, _inv1R,
        _inv2P, _inv2R,
        _inv3P, _inv3R;

    private void OnEnable()
    {
        if (asset == null)
        {
            Debug.LogWarning("[InputProbe] No InputActionAsset assigned.");
            return;
        }

        _player = asset.FindActionMap(actionMapName, throwIfNotFound: true);
        _player.Enable();

        PlayerInputRouter.Bind(_player);
        _bound = true;

        // Build delegates once
        _jumpP ??= () => Debug.Log("Jump PRESSED");
        _jumpR ??= () => Debug.Log("Jump RELEASED");
        _glideP ??= () => Debug.Log("Glide PRESSED");
        _glideR ??= () => Debug.Log("Glide RELEASED");
        _sprintP ??= () => Debug.Log("Sprint PRESSED");
        _sprintR ??= () => Debug.Log("Sprint RELEASED");
        _crawlP ??= () => Debug.Log("Crawl PRESSED");
        _crawlR ??= () => Debug.Log("Crawl RELEASED");
        _climbP ??= () => Debug.Log("Climb PRESSED");
        _climbR ??= () => Debug.Log("Climb RELEASED");
        _interactP ??= () => Debug.Log("Interact PRESSED");
        _interactR ??= () => Debug.Log("Interact RELEASED");
        _throwP ??= () => Debug.Log("Throw PRESSED");
        _throwR ??= () => Debug.Log("Throw RELEASED");
        _menuP ??= () => Debug.Log("Menu PRESSED");
        _menuR ??= () => Debug.Log("Menu RELEASED");
        _mapInvP ??= () => Debug.Log("Map/Inventory PRESSED");
        _mapInvR ??= () => Debug.Log("Map/Inventory RELEASED");
        _camoP ??= () => Debug.Log("Camouflage PRESSED");
        _camoR ??= () => Debug.Log("Camouflage RELEASED");
        _inv1P ??= () => Debug.Log("Inventory Slot 1 PRESSED");
        _inv1R ??= () => Debug.Log("Inventory Slot 1 RELEASED");
        _inv2P ??= () => Debug.Log("Inventory Slot 2 PRESSED");
        _inv2R ??= () => Debug.Log("Inventory Slot 2 RELEASED");
        _inv3P ??= () => Debug.Log("Inventory Slot 3 PRESSED");
        _inv3R ??= () => Debug.Log("Inventory Slot 3 RELEASED");

        // Ensure not double-subscribed across reloads/toggles
        if (_subscribed) UnsubscribeAll();
        SubscribeAll();
        _subscribed = true;

        Debug.Log("[InputProbe] Bound to Player map and subscribed to router events.");
    }

    private void OnDisable()
    {
        if (_subscribed) { UnsubscribeAll(); _subscribed = false; }
        if (_bound) { PlayerInputRouter.Unbind(); _bound = false; }

        _player?.Disable();
        Debug.Log("[InputProbe] Unbound and unsubscribed.");
    }

    private void SubscribeAll()
    {
        PlayerInputRouter.OnJumpPressed += _jumpP;
        PlayerInputRouter.OnJumpReleased += _jumpR;
        PlayerInputRouter.OnGlidePressed += _glideP;
        PlayerInputRouter.OnGlideReleased += _glideR;

        PlayerInputRouter.OnSprintPressed += _sprintP;
        PlayerInputRouter.OnSprintReleased += _sprintR;
        PlayerInputRouter.OnCrawlPressed += _crawlP;
        PlayerInputRouter.OnCrawlReleased += _crawlR;
        PlayerInputRouter.OnClimbPressed += _climbP;
        PlayerInputRouter.OnClimbReleased += _climbR;

        PlayerInputRouter.OnInteractPressed += _interactP;
        PlayerInputRouter.OnInteractReleased += _interactR;
        PlayerInputRouter.OnThrowPressed += _throwP;
        PlayerInputRouter.OnThrowReleased += _throwR;

        PlayerInputRouter.OnMenuPressed += _menuP;
        PlayerInputRouter.OnMenuReleased += _menuR;
        PlayerInputRouter.OnMapInventoryPressed += _mapInvP;
        PlayerInputRouter.OnMapInventoryReleased += _mapInvR;

        PlayerInputRouter.OnCamouflagePressed += _camoP;
        PlayerInputRouter.OnCamouflageReleased += _camoR;

        PlayerInputRouter.OnInventorySlotOnePressed += _inv1P;
        PlayerInputRouter.OnInventorySlotOneReleased += _inv1R;
        PlayerInputRouter.OnInventorySlotTwoPressed += _inv2P;
        PlayerInputRouter.OnInventorySlotTwoReleased += _inv2R;
        PlayerInputRouter.OnInventorySlotThreePressed += _inv3P;
        PlayerInputRouter.OnInventorySlotThreeReleased += _inv3R;
    }

    private void UnsubscribeAll()
    {
        PlayerInputRouter.OnJumpPressed -= _jumpP;
        PlayerInputRouter.OnJumpReleased -= _jumpR;
        PlayerInputRouter.OnGlidePressed -= _glideP;
        PlayerInputRouter.OnGlideReleased -= _glideR;

        PlayerInputRouter.OnSprintPressed -= _sprintP;
        PlayerInputRouter.OnSprintReleased -= _sprintR;
        PlayerInputRouter.OnCrawlPressed -= _crawlP;
        PlayerInputRouter.OnCrawlReleased -= _crawlR;
        PlayerInputRouter.OnClimbPressed -= _climbP;
        PlayerInputRouter.OnClimbReleased -= _climbR;

        PlayerInputRouter.OnInteractPressed -= _interactP;
        PlayerInputRouter.OnInteractReleased -= _interactR;
        PlayerInputRouter.OnThrowPressed -= _throwP;
        PlayerInputRouter.OnThrowReleased -= _throwR;

        PlayerInputRouter.OnMenuPressed -= _menuP;
        PlayerInputRouter.OnMenuReleased -= _menuR;
        PlayerInputRouter.OnMapInventoryPressed -= _mapInvP;
        PlayerInputRouter.OnMapInventoryReleased -= _mapInvR;

        PlayerInputRouter.OnCamouflagePressed -= _camoP;
        PlayerInputRouter.OnCamouflageReleased -= _camoR;

        PlayerInputRouter.OnInventorySlotOnePressed -= _inv1P;
        PlayerInputRouter.OnInventorySlotOneReleased -= _inv1R;
        PlayerInputRouter.OnInventorySlotTwoPressed -= _inv2P;
        PlayerInputRouter.OnInventorySlotTwoReleased -= _inv2R;
        PlayerInputRouter.OnInventorySlotThreePressed -= _inv3P;
        PlayerInputRouter.OnInventorySlotThreeReleased -= _inv3R;
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Compact snapshot of intent/state every frame
        Debug.Log(
            $"Move={PlayerInputRouter.Move} " +
            $"Look={PlayerInputRouter.Look} " +
            $"IsMoving={PlayerInputRouter.IsMoving} " +
            $"JumpHeld={PlayerInputRouter.JumpHeld} " +
            $"GlideHeld={PlayerInputRouter.GlideHeld} " +
            $"SprintHeld={PlayerInputRouter.SprintHeld} " +
            $"CrawlHeld={PlayerInputRouter.CrawlHeld} " +
            $"ClimbHeld={PlayerInputRouter.ClimbHeld} " +
            $"InteractHeld={PlayerInputRouter.InteractHeld} " +
            $"ThrowHeld={PlayerInputRouter.ThrowHeld} " +
            $"MenuHeld={PlayerInputRouter.MenuHeld} " +
            $"MapInvHeld={PlayerInputRouter.MapInventoryHeld} " +
            $"CamouflageHeld={PlayerInputRouter.CamouflageHeld} " +
            $"Inv1Held={PlayerInputRouter.Inv1Held} " +
            $"Inv2Held={PlayerInputRouter.Inv2Held} " +
            $"Inv3Held={PlayerInputRouter.Inv3Held} " +
            $"AllFoursIntent={PlayerInputRouter.AllFoursIntent}"
        );
#endif
    }
}
