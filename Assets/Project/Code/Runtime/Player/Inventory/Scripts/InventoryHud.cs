// Assets/_Project/Code/Runtime/Inventory/InventoryHud.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MothHunt.Inventory;

public class InventoryHud : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory PlayerInventory;
    public ItemDatabase Database;

    [System.Serializable]
    public class Row
    {
        public ItemDef Item;
        public Text Label; // or TextMeshProUGUI
    }

    public List<Row> Rows = new();

    private void OnEnable()
    {
        if (!PlayerInventory) PlayerInventory = FindObjectOfType<PlayerInventory>();
        if (PlayerInventory)
        {
            PlayerInventory.OnItemChanged += HandleChanged;
        }

        // Initial fill
        foreach (var r in Rows)
            RefreshRow(r);
    }

    private void OnDisable()
    {
        if (PlayerInventory)
            PlayerInventory.OnItemChanged -= HandleChanged;
    }

    private void HandleChanged(ItemDef item, int newCount)
    {
        foreach (var r in Rows)
            if (r.Item == item)
                RefreshRow(r);
    }

    private void RefreshRow(Row r)
    {
        if (!r.Item || !r.Label) return;
        r.Label.text = $"{r.Item.DisplayName}: {PlayerInventory?.GetCount(r.Item) ?? 0}";
    }
}
