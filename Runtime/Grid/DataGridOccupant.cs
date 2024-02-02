using System;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Serialization data for a single griddable item
    /// </summary>
    [Serializable]
    public struct DataGridOccupant
    {
        /// <summary>
        /// The address of the item config file (DataConfigUIInventoryItem)
        /// </summary>
        public string ItemConfigID;
        
        /// <summary>
        /// The ID of this specific item instance
        /// </summary>
        public string ItemInstanceID;
        
        /// <summary>
        /// 
        /// </summary>
        public DataItemGridPosition GridPosition;

        /// <summary>
        /// The index of the grid to which this item directly belongs
        /// </summary>
        [Header("Subgrid Data")]
        public int ParentSubgrid;

        /// <summary>
        /// The index of the subgrid that this item represents (must be a container item)
        /// </summary>
        public int ChildSubgrid;
    }
}