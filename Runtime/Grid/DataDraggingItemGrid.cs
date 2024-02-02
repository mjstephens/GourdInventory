using System.Collections.Generic;
using GalaxyGourd.Grid;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Dragging item data specific to grids
    /// </summary>
    public struct DataDraggingItemGrid
    {
        public Vector2Int OriginalCoords;
        public List<int> OriginalGridIndices;
        public List<int> CurrentGridIndices;
        public InventoryItemGridOrientation OriginalOrientation;
        public List<IInventoryOccupant> OverlappingItems;
        public GridCell HoveredOrigin;
        public ItemGridPlacementResult CurrentPlacementResult;
    }
}