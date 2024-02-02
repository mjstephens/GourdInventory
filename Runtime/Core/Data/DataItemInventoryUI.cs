using System;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Defines data for an item's inventory representation
    /// </summary>
    [Serializable]
    public class DataItemInventoryUI
    {
        [Header("Item")]
        [SerializeField] public UIInventoryItem InventoryPrefab;
        [SerializeField] public string DisplayName = "Item";
        [SerializeField] public Sprite Icon;
        [SerializeField] public ItemEquipSlotType Slot;
        [SerializeField] public int StackSize;
        [SerializeField] public Vector2Int Dimensions;
        [SerializeField] public bool CanRotate;
        
        public string ConfigID { get; set; }
        public string InstanceID { get; set; }
        // Used at runtime only for associating items being distributed into grids
        public int ChildSubgrid { get; set; }
    }
}