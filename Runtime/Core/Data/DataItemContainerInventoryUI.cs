using System;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    [Serializable]
    public class DataItemContainerInventoryUI : DataItemInventoryUI
    {
        public readonly DataConfigUIInventoryGrid ContainerGrid;
        internal readonly GameObject GridObjPrefab;

        public DataItemContainerInventoryUI(
            DataItemInventoryUI baseData, 
            DataConfigUIInventoryGrid containerGrid,
            GameObject gridPrefab)
        {
            InventoryPrefab = baseData.InventoryPrefab;
            DisplayName = baseData.DisplayName;
            Icon = baseData.Icon;
            Slot = baseData.Slot;
            Dimensions = baseData.Dimensions;
            CanRotate = baseData.CanRotate;
            StackSize = baseData.StackSize;
            ConfigID = baseData.ConfigID;

            ContainerGrid = containerGrid;
            GridObjPrefab = gridPrefab;
        }
    }
}