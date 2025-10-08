// Assets/_Project/Code/Runtime/Inventory/ItemDef.cs
using UnityEngine;

namespace MothHunt.Inventory
{
    public enum ItemCategory { Throwable /* extend later (Key, Crafting, Quest...) */ }

    [CreateAssetMenu(menuName = "MothHunt/Inventory/Item", fileName = "Item_")]
    public class ItemDef : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID for save/load & debugging. Keep names unique.")]
        public string Id = "pebble";   // e.g., "pebble", "bottle", "mushroom"
        public string DisplayName = "Pebble";
        public Sprite Icon;

        [Header("Gameplay")]
        public ItemCategory Category = ItemCategory.Throwable;
        [Min(1)] public int StackLimit = 99;

        [Header("Throwable Tuning")]
        public bool Throwable = true;
        public float ThrowForce = 12f; // used later by your throw system
    }
}
