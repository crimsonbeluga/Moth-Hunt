using System;
using System.Collections.Generic;
using UnityEngine;

namespace MothHunt.Inventory
{
    [DisallowMultipleComponent]
    public class PlayerInventory : MonoBehaviour
    {
        // Dictionary: ItemDef asset -> count
        private readonly Dictionary<ItemDef, int> _counts = new();

        // Fired when a specific item’s count changes
        public event Action<ItemDef, int> OnItemChanged;

        public int GetCount(ItemDef def) => def && _counts.TryGetValue(def, out var c) ? c : 0;

        public int Add(ItemDef def, int amount = 1)
        {
            if (!def || amount <= 0) return 0;

            _counts.TryGetValue(def, out var current);
            _counts[def] = current + amount;

            OnItemChanged?.Invoke(def, _counts[def]);
            return amount;
        }

        public bool TryConsume(ItemDef def, int amount = 1)
        {
            if (!def || amount <= 0) return false;

            var current = GetCount(def);
            if (current < amount) return false;

            _counts[def] = current - amount;
            OnItemChanged?.Invoke(def, _counts[def]);
            return true;
        }

        public bool Has(ItemDef def, int atLeast = 1) => GetCount(def) >= atLeast;

#if UNITY_EDITOR
        // ---------------- DEBUG LOGGING ----------------
        private void OnEnable()
        {
            OnItemChanged += DebugItemChange;
        }

        private void OnDisable()
        {
            OnItemChanged -= DebugItemChange;
        }

        private void DebugItemChange(ItemDef item, int newCount)
        {
            if (item == null) return;

            // prints clean, readable line in console
            Debug.Log($"[Inventory] {item.DisplayName} → {newCount}");
        }
#endif
    }
}
