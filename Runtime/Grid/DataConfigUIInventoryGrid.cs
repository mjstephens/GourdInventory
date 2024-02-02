using System;
using System.Collections.Generic;
using GalaxyGourd.Grid;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    [CreateAssetMenu(
        fileName = "DAT_UIInventoryGrid", 
        menuName = "RPG/UI/Inventory Grid")] 
    public class DataConfigUIInventoryGrid : DataConfigUIScalableGrid
    {
        #region DATA

        [Header("Containers")]
        [SerializeField] internal bool CanHoldContainers = true;

        // We use a temporary array of items to create and configure placement
        // since this happens sequentially we can just reuse this one
        private static readonly List<IInventoryOccupant> _tempAddItemsCache = new();

        #endregion DATA


        #region UTILITY

        /// <summary>
        /// Returns results of attempting to auto-add items to grid
        /// </summary>
        public ItemGridAutoAddResults GetAutoAddResults(List<(DataItemInventoryUI, int)> toAdd)
        {
            bool didPlaceItemSuccesfully = false;
            bool didFailToPlaceItem = false;
            _tempAddItemsCache.Clear();
            foreach ((DataItemInventoryUI, int) item in toAdd)
            {
                // Find out the total number of items needed
                (int, int) distributionCount = InventoryGridDistributor.GetItemStackDistribution(
                    item.Item1, 
                    this, 
                    _tempAddItemsCache, 
                    item.Item2, 
                    false);
                
                // Try and add all of the items to the grid
                int totalItemCount = distributionCount.Item2 == 0 ? distributionCount.Item1 : distributionCount.Item1 + 1;
                for(int i = 0; i < totalItemCount; i++)
                {
                    Tuple<Vector2Int, InventoryItemGridOrientation> results = InventoryGridDistributor.GetItemAutoAddPlacement(
                        item.Item1, 
                        this, 
                        _tempAddItemsCache, 
                        null);
                    if (results.Item1.x != -1 && results.Item1.y != -1)
                    {
                        DataItemGridPosition dataPosition = new DataItemGridPosition
                        {
                            Coords = results.Item1,
                            Orientation = (byte)results.Item2,
                            StackCount = 1,
                        };
                        
                        // Add the item
                        _tempAddItemsCache.Add(
                            InventoryGrid.CreateStagedItem(item.Item1, dataPosition, GridWidth, item.Item1.InstanceID));
                        didPlaceItemSuccesfully = true;
                    }
                    else
                    {
                        didFailToPlaceItem = true;
                        break;
                    }
                }
                
                if (didFailToPlaceItem)
                    break;
            }

            // Return result
            if (didFailToPlaceItem)
            {
                return didPlaceItemSuccesfully ? ItemGridAutoAddResults.PartiallyFits : ItemGridAutoAddResults.NoneFits;
            }

            return ItemGridAutoAddResults.FullyFits;
        }
        
        /// <summary>
        /// Returns list of grid occupants that would be positioned on grid
        /// </summary>
        public IEnumerable<IInventoryOccupant> GetGridItemDistribution(
            List<(DataItemInventoryUI, int)> toAdd,
            GridItemsArrangementType arrangementType = GridItemsArrangementType.Scattered)
        {
            List<IInventoryOccupant> addedItems = new();
            foreach ((DataItemInventoryUI, int) item in toAdd)
            {
                // Get item count
                (int, int) distributionCount = InventoryGridDistributor.GetItemStackDistribution(
                    item.Item1, 
                    this, 
                    addedItems, 
                    item.Item2, 
                    true);
                
                // Now add rest of whole items
                for (int i = 0; i < distributionCount.Item1; i++)
                {
                    Tuple<Vector2Int, InventoryItemGridOrientation> results = 
                        InventoryGridDistributor.GetItemAutoAddPlacement(item.Item1, this, addedItems, null);
                    if (results.Item1.x != -1 && results.Item1.y != -1)
                    {
                        DataItemGridPosition dataPosition = new DataItemGridPosition
                        {
                            Coords = results.Item1,
                            Orientation = (byte)results.Item2,
                            StackCount = item.Item1.StackSize,
                        };

                        IInventoryOccupant occupant = 
                            InventoryGrid.CreateStagedItem(item.Item1, dataPosition, GridWidth, item.Item1.InstanceID);
                        occupant.ChildSubgrid = item.Item1.ChildSubgrid;
                        addedItems.Add(occupant);
                    }
                }
                
                // Now add stack remainder, if it exists
                if (distributionCount.Item2 > 0)
                {
                    Tuple<Vector2Int, InventoryItemGridOrientation> remResults = 
                        InventoryGridDistributor.GetItemAutoAddPlacement(item.Item1, this, addedItems, null);
                    if (remResults.Item1.x != -1 && remResults.Item1.y != -1)
                    {
                        DataItemGridPosition dataPosition = new DataItemGridPosition
                        {
                            Coords = remResults.Item1,
                            Orientation = (byte)remResults.Item2,
                            StackCount = item.Item1.StackSize,
                        };
                        
                        IInventoryOccupant occupant = 
                            InventoryGrid.CreateStagedItem(item.Item1, dataPosition, GridWidth, item.Item1.InstanceID);
                        occupant.ChildSubgrid = item.Item1.ChildSubgrid;
                        addedItems.Add(occupant);
                    }
                }
            }

            return addedItems;
        }

        #endregion UTILITY
    }
}