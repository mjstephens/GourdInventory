using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Defines data for a transient (currently moving) inventory item
    /// </summary>
    public struct DataDraggingItem
    {
        public UIInventoryItem Item;
        public int GrabbedStackCount;
        public IItemReceivable Source;  // The entity from which the item originated its drag
        public IItemReceivable Current; // The entity from which the item originated its drag
        public Vector3 GrabOffset;      // Offset is in SCREEN SPACE
        public Vector3 CurrentDragPosition;
        public float DragHoldStillElapsed;           // The time that the item has been held still (without moving position)
        public bool DragHasMovedForCurrentContainer; // If we have yet drag-moved the item inside its current container
    }
}