using System.Collections.Generic;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Defines functionality for all inventory occupants, whether instantiated (UIInventoryItem) or not (UIInventoryGridItemStub)
    /// </summary>
    public interface IInventoryOccupant
    {
        #region PROPERTIES

        string InstanceID { get; set; }
        DataItemInventoryUI Data { get; }
        int ChildSubgrid { get; set; }
        List<int> OccupyingIndices { get; }
        Vector2Int Dimensions => Data.Dimensions;
        DataItemGridPosition GridPosition { get; }
        int StackSize => Data.StackSize;
        List<IInventoryOccupant> SubOccupants { get; set; } // Items inside of the grid (applicable to containers)

        #endregion PROPERTIES


        #region METHODS

        bool HasStackRoom()
        {
            return Data.StackSize > 1 && GridPosition.StackCount < Data.StackSize;
        }

        bool ItemCanStackWithThis(DataItemInventoryUI item)
        {
            return Data.StackSize > 1 && item.ConfigID == Data.ConfigID;
        }
        
        /// <summary>
        /// We return -1 if nothing could be added; 0 if everything can be added successfully;
        /// Any other num represents the leftover after maxing the stack
        /// </summary>
        int GetItemStackResult(int numToAdd)
        {
            // If we cannot add anymore
            if (GridPosition.StackCount >= Data.StackSize) 
                return numToAdd;

            int neededForFull = Data.StackSize - GridPosition.StackCount;
            if (numToAdd <= neededForFull)
            {
                return 0;
            }
            
            return numToAdd - neededForFull;
        }
        
        int TryAddToStack(int numToAdd);
        List<int> GetOccupyingIndices(int gridWidth)
        {
            List<int> indices = new();
            
            // Get orientation-adjusted axises
            int xAxis = (InventoryItemGridOrientation)GridPosition.Orientation is 
                InventoryItemGridOrientation.Up or InventoryItemGridOrientation.Down
                ? Dimensions.x
                : Dimensions.y;
            int yAxis = xAxis == Dimensions.x ? Dimensions.y : Dimensions.x;
            
            // Set occupying indices
            for (int i = 0; i < xAxis; i++)
            {
                for (int e = 0; e < yAxis; e++)
                {
                    Vector2Int thisCoords = new(GridPosition.Coords.x + i, GridPosition.Coords.y + e);
                    indices.Add(thisCoords.x + (gridWidth * thisCoords.y));
                }
            }

            return indices;
        }

        #endregion METHODS
    }
}