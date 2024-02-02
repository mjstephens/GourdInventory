using System;
using System.Collections.Generic;
using GalaxyGourd.Grid;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Collection of methods to place + distribute items inside an inventory grid
    /// </summary>
    public static class InventoryGridDistributor
    {
        #region COLLECTIONS
        
        /// <summary>
        /// Creates a map matching subgrids with the items that they contain
        /// int 0 = base grid, 1 = item with '1' as child subgrid, etc
        /// </summary>
        public static Dictionary<int, List<DataGridOccupant>> CreateSubgridItemMap(DataItemCollection collection)
        {
            Dictionary<int, List<DataGridOccupant>> structuredItemData = new();

            if (collection.Occupants != null)
            {
                foreach (DataGridOccupant item in collection.Occupants)
                {
                    // If we don't yet have an entry for this subgrid, create one
                    if (!structuredItemData.ContainsKey(item.ParentSubgrid))
                    {
                        structuredItemData.Add(item.ParentSubgrid, new List<DataGridOccupant>());
                    }
                
                    // Add this item to the correct subgrid entry
                    structuredItemData[item.ParentSubgrid].Add(item);
                }
            }

            return structuredItemData;
        }
        
        /// <summary>
        /// Converts item collection data to grid occupants for in-game use
        /// </summary>
        public static Dictionary<int, List<IInventoryOccupant>> ConvertSubgridItemMap(
            Dictionary<int, List<DataGridOccupant>> items,
            IInventoryViewProvider viewProvider,
            InventoryGrid grid)
        {
            // Gather all grid items (subcontainers) - we need their grid width to calculate object positioning 
            List<Tuple<int, int>> containerGrids = new List<Tuple<int, int>>();
            foreach (KeyValuePair<int, List<DataGridOccupant>> subgridSet in items)
            {
                foreach (DataGridOccupant item in subgridSet.Value)
                {
                    if (item.ChildSubgrid != 0)
                    {
                        DataItemContainerInventoryUI containerData =
                            viewProvider.GetItemConfig(item.ItemConfigID) as DataItemContainerInventoryUI;
                        containerGrids.Add(new Tuple<int, int>(item.ChildSubgrid, containerData.ContainerGrid.GridWidth));
                    }
                }
            }

            // Create stubs for all items
            Dictionary<int, List<IInventoryOccupant>> occupants = new();
            foreach (KeyValuePair<int, List<DataGridOccupant>> subgridSet in items)
            {
                List<IInventoryOccupant> subgridOccupants = new();
                foreach (DataGridOccupant item in subgridSet.Value)
                {
                    int width = GetGridWidthForContainer(containerGrids, item.ParentSubgrid);
                    subgridOccupants.Add(InventoryGrid.CreateStagedItem(
                        viewProvider.GetItemConfig(item.ItemConfigID),
                        item.GridPosition,
                        width == -1 ? grid.Grid.GridWidth : width,
                        item.ItemInstanceID,
                        item.ChildSubgrid));
                }

                occupants.Add(subgridSet.Key, subgridOccupants);
            }

            return occupants;
        }
        
        /// <summary>
        /// Stages items in the given grid, including subgrid containers and items contained therein
        /// </summary>
        public static void StageItemCollection(Dictionary<int, List<IInventoryOccupant>> occupants, InventoryGrid grid)
        {
            if (occupants.Count == 0)
                return;
            
            // Stage base items
            foreach (IInventoryOccupant baseItem in occupants[0])
            {
                grid.AddStagedItem(baseItem);
            }
            
            // Set subitems for all subgrids
            foreach (KeyValuePair<int, List<IInventoryOccupant>> subgridOccupants in occupants)
            {
                foreach (IInventoryOccupant item in subgridOccupants.Value)
                {
                    if (item is UIInventoryGridItemContainerStub container && occupants.ContainsKey(container.SubgridIndex))
                    {
                        // Find the index for the grid this container item opens
                        int subgrid = container.SubgridIndex;
                        
                        // Add subitems to this container
                        container.AddSubItems(occupants[subgrid]);
                    }
                }
            }
        }
        
        private static int GetGridWidthForContainer(List<Tuple<int, int>> grids, int gridIndex)
        {
            foreach (Tuple<int, int> grid in grids)
            {
                if (grid.Item1 == gridIndex)
                {
                    return grid.Item2;
                }
            }

            // This means this entry wasn't present - aka it was not a subgrid, so we use the default grid width
            return -1;
        }
        
        #endregion COLLECTIONS

        
        #region DISTRIBUTION
        
        /// <summary>
        /// Determines how an item of a specific stack size would be distributed across the grid;
        /// optionally performs the distribution
        /// </summary>
        /// <param name="data">The item to be distributed</param>
        /// <param name="grid">Grid data</param>
        /// <param name="items">Ref item list</param>
        /// <param name="stackSize">The size of the item stack being distributed</param>
        /// <param name="applyStack">If FALSE, only the calculation results will be run; if TRUE, the item will be
        /// physically distributed</param>
        /// <returns>1st param is the number of FULL stacks; 2nd param is the count of the remaining PARTIAL stack</returns>
        internal static (int, int) GetItemStackDistribution(
            DataItemInventoryUI data, 
            DataConfigGridView grid,
            List<IInventoryOccupant> items, 
            int stackSize, 
            bool applyStack)
        {
            if (data.StackSize == 1)
            {
                return (stackSize, 0);
            }

            int remainderAfterDistribution = RemainderAfterFullyStackingItem(data, grid, items, stackSize, applyStack);
            if (remainderAfterDistribution == 0)
            {
                return (0, 0);
            }

            if (remainderAfterDistribution <= data.StackSize)
            {
                return (0, remainderAfterDistribution);
            }
            
            int whole = remainderAfterDistribution / data.StackSize;
            int remainder = remainderAfterDistribution % data.StackSize;
            
            return (whole, remainder);
        }

        private static int RemainderAfterFullyStackingItem(
            DataItemInventoryUI dataItem, 
            DataConfigGridView grid,
            List<IInventoryOccupant> items, 
            int stackSize, 
            bool applyStack)
        {
            int leftToStack = stackSize;
            List<IInventoryOccupant> foundItems = new();
            
            for (int i = 0; i < grid.GridWidth; i++)
            {
                for (int e = 0; e < grid.GridHeight; e++)
                {
                    int index = grid.GetFlattenedIndexForCoords(i, e);
                    IInventoryOccupant item = GetItemInCell(index, items);
                    if (item == null || !item.ItemCanStackWithThis(dataItem) || !item.HasStackRoom() || foundItems.Contains(item)) 
                        continue;
                    
                    foundItems.Add(item);
                    int stackResult = applyStack ? item.TryAddToStack(leftToStack) : item.GetItemStackResult(leftToStack);
                    leftToStack -= (stackSize - stackResult);

                    if (leftToStack <= 0)
                        return 0;
                }
            }
            
            return leftToStack;
        }

        /// <summary>
        /// Tests if the given item can be auto-added, and returns the results
        /// </summary>
        internal static Tuple<Vector2Int, InventoryItemGridOrientation> GetItemAutoAddPlacement(
            DataItemInventoryUI data, 
            DataConfigGridView grid,
            List<IInventoryOccupant> items, 
            UIInventoryItem currentItem)
        {
            // Cycle through all grid indices, looking for an open space the size of the item
            List<int> indicesCache = new();
            
            // We want to cycle from top-left down first, then right
            for (int i = 0; i < grid.GridWidth; i++)
            {
                for (int e = 0; e < grid.GridHeight; e++)
                {
                    Vector2Int coords = new(i, e);
                    
                    // Will this item fit in the grid in default configuration?
                    indicesCache.Clear();
                    if (ItemDoesFitInGridWithCoords(grid, data, coords, true))
                    {
                        for (int x = 0; x < data.Dimensions.x; x++)
                        {
                            for (int y = 0; y < data.Dimensions.y; y++)
                            {
                                Vector2Int thisCoord = new(coords.x + x, coords.y + y);
                                indicesCache.Add(grid.GetFlattenedIndexForCoords(thisCoord.x, thisCoord.y));
                            }
                        }

                        // If no overlap, this item fits!
                        if (GetOverlappingItems(items, currentItem, indicesCache).Count == 0)
                        {
                            return new Tuple<Vector2Int, InventoryItemGridOrientation>(coords, InventoryItemGridOrientation.Up);
                        }
                    }
                
                    // Will this item fit in rotated configuration?
                    indicesCache.Clear();
                    if (ItemDoesFitInGridWithCoords(grid, data, coords, false))
                    {
                        for (int x = 0; x < data.Dimensions.y; x++)
                        {
                            for (int y = 0; y < data.Dimensions.x; y++)
                            {
                                Vector2Int thisCoord = new Vector2Int(coords.x + x, coords.y + y);
                                indicesCache.Add(grid.GetFlattenedIndexForCoords(thisCoord.x, thisCoord.y));
                            }
                        }

                        // If no overlap, this item fits!
                        if (GetOverlappingItems(items, currentItem, indicesCache).Count == 0)
                        {
                            return new Tuple<Vector2Int, InventoryItemGridOrientation>(
                                coords, InventoryItemGridOrientation.Right);
                        }
                    }
                }
            }
            
            // If we get to this point, the item does not fit anywhere in the grid
            return new Tuple<Vector2Int, InventoryItemGridOrientation>(
                new Vector2Int(-1, -1), InventoryItemGridOrientation.Up);
        }
        
        internal static List<IInventoryOccupant> GetOverlappingItems(
            List<IInventoryOccupant> items, 
            UIInventoryItem currentItem,
            List<int> indices, 
            List<int> excludeIndices = null)
        {
            List<IInventoryOccupant> overlapping = new();
            foreach (IInventoryOccupant thisItem in items)
            {
                // Skip if item is the one we're dragging
                if (ReferenceEquals(thisItem, currentItem))
                    continue;
                
                // Check item
                foreach (int index in indices)
                {
                    bool conflict = thisItem.OccupyingIndices.Contains(index);
                    if (conflict && excludeIndices != null && excludeIndices.Contains(index))
                        continue;

                    if (conflict && !overlapping.Contains(thisItem))
                    {
                        overlapping.Add(thisItem);
                    }
                }
            }
            
            return overlapping;
        }
        
        internal static IInventoryOccupant GetValidStackableOverlappingItem(
            IInventoryOccupant item, 
            DataItemInventoryUI data, 
            List<IInventoryOccupant> items)
        {
            if (items == null)
                return null;
            
            if (items.Count == 1 && items[0].ItemCanStackWithThis(data) && items[0].HasStackRoom())
            {
                return items[0];
            }
            
            return null;
        }
        
        /// <summary>
        /// Returns the item in the given cell index. Returns null if no item present.
        /// </summary>
        internal static IInventoryOccupant GetItemInCell(int index, List<IInventoryOccupant> items)
        {
            IInventoryOccupant item = null;
            foreach (IInventoryOccupant thisItem in items)
            {
                if (thisItem.OccupyingIndices.Contains(index))
                {
                    item = thisItem;
                    break;
                }
            }
            
            return item;
        }
        
        private static bool ItemDoesFitInGridWithCoords(
            DataConfigGridView dataGrid, 
            DataItemInventoryUI data, 
            Vector2Int coords, 
            bool defaultOrientation)
        {
            int xAxis = defaultOrientation ? data.Dimensions.x : data.Dimensions.y;
            int yAxis = xAxis == data.Dimensions.x ? data.Dimensions.y : data.Dimensions.x;
            for (int i = 0; i < xAxis; i++)
            {
                for (int e = 0; e < yAxis; e++)
                {
                    Vector2Int thisCoords = new Vector2Int(coords.x + i, coords.y + e);
                    if (!dataGrid.CoordsAreWithinGrid(thisCoords))
                        return false;
                }
            }

            return true;
        }
        
        #endregion DISTRIBUTION
    }
}