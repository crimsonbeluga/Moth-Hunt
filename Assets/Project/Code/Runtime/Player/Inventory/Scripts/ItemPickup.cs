using MothHunt.Inventory;
using UnityEngine;

namespace MothHunt.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("Pickup")]
        public ItemDef Item;
        [Min(1)] public int Quantity = 1;
        public bool ForceTriggerCollider = true;

        public string Prompt => Item ? $"Pick up {Item.DisplayName} x{Quantity}" : "Pick up";

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (ForceTriggerCollider) col.isTrigger = true;
        }

        public bool Interact(PlayerInventory inventory)
        {
            if (!Item || inventory == null) return false;
            inventory.Add(Item, Quantity);
            Destroy(gameObject);
            return true;
        }
    }
}
