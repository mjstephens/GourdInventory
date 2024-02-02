using System;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Data relating to the runtime position of an item within a grid
    /// </summary>
    [Serializable]
    public struct DataItemGridPosition
    {
        /// <summary>
        /// The coordinates of the item's origin (upper-left cell)
        /// </summary>
        public Vector2Int Coords;

        /// <summary>
        /// The orientation of the item
        /// </summary>
        public byte Orientation;

        /// <summary>
        /// How many items are in the item stack
        /// </summary>
        public int StackCount;
    }
}