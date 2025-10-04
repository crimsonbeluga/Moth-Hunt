using MothHunt.Input;
using MothHunt.Inventory;
using UnityEngine;

namespace MothHunt.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class InteractionTrigger : MonoBehaviour
    {
        private bool _playerInside;
        private PlayerInventory _playerInv;
        private IInteractable _interactable;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            _interactable = GetComponent<IInteractable>()
                         ?? GetComponentInChildren<IInteractable>()
                         ?? GetComponentInParent<IInteractable>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInside = true;
            _playerInv = other.GetComponent<PlayerInventory>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInside = false;
            _playerInv = null;
        }

        private void Update()
        {
            if (!_playerInside || _playerInv == null || _interactable == null) return;
            if (PlayerInputRouter.InteractPressedThisFrame)
            {
                _interactable.Interact(_playerInv);
            }
        }
    }
}
