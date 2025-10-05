// Assets/_Project/Code/Runtime/Inventory/ItemDatabase.cs
using System.Collections.Generic;
using UnityEngine;

namespace MothHunt.Inventory
{
    [CreateAssetMenu(menuName = "MothHunt/Inventory/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDef> Items = new(); // Drag your Item_ assets here (Pebble, Bottle, Mushroom)
    }
}
