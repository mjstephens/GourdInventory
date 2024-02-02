using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Parital piece of InventoryItemGrid; Enables item spawning and calculates auto-adding
    /// </summary>
    public partial class InventoryGrid
    {
        #region COLLECTION

        /// <summary>
        /// Destroys all instantiated items
        /// </summary>
        public void UnloadItemCollection()
        {
            TransformUtility.DeleteChildren(_itemParent);
            _items.Clear();
            _stagedItems.Clear();
            _currentHovered = null;
            _currentContainerItem = null;
        }

        #endregion COLLECTION
        
        
        #region SPAWN

        public ItemGridAutoAddResults GetAutoAddResults(
            List<Tuple<DataItemInventoryUI, int>> toAdd,
            int forceStackSize = -1)
        {
            bool didPlaceItemSuccesfully = false;
            bool didFailToPlaceItem = false;
            bool canPlacePartialStack = false;
            
            List<IInventoryOccupant> items = new();
            foreach (Tuple<DataItemInventoryUI, int> item in toAdd)
            {
                // Find out the total number of items needed
                (int, int) distributionCount = forceStackSize != -1 ? (0, forceStackSize) :
                    InventoryGridDistributor.GetItemStackDistribution(
                        item.Item1, 
                        _config, 
                        _items, 
                        item.Item2, 
                        false);

                canPlacePartialStack = distributionCount.Item2 != 0 && distributionCount.Item2 < item.Item2;
                
                // Try and add all of the items to the grid
                int totalItemCount = distributionCount.Item2 == 0 ? distributionCount.Item1 : distributionCount.Item1 + 1;
                for(int i = 0; i < totalItemCount; i++)
                {
                    Tuple<Vector2Int, InventoryItemGridOrientation> results = InventoryGridDistributor.GetItemAutoAddPlacement(
                        item.Item1, 
                        _config, 
                        _items, 
                        _dragSource.CurrentItem.Item);
                    if (results.Item1.x != -1 && results.Item1.y != -1)
                    {
                        DataItemGridPosition dataPosition = new DataItemGridPosition
                        {
                            Coords = results.Item1,
                            Orientation = (byte)results.Item2,
                            StackCount = 1
                        };
                        
                        // Add the item
                        items.Add(AddStagedItem(item.Item1, dataPosition));
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
            
            // We need to remove all the added items, as they were just added to check spacing
            foreach (IInventoryOccupant item in items)
            {
                if (_stagedItems.Contains(item))
                {
                    _stagedItems.Remove(item);
                }

                if (_items.Contains(item))
                {
                    _items.Remove(item);
                }
            }

            // Return result
            if (didFailToPlaceItem)
            {
                return didPlaceItemSuccesfully || canPlacePartialStack ? 
                    ItemGridAutoAddResults.PartiallyFits :
                    ItemGridAutoAddResults.NoneFits;
            }

            return ItemGridAutoAddResults.FullyFits;
        }
        
        public List<IInventoryOccupant> AutoAddItems(
            List<Tuple<DataItemInventoryUI, int>> toAdd, 
            bool asStaged,
            int forceStackSize = -1)
        {
            List<IInventoryOccupant> addedItems = new();
            foreach (Tuple<DataItemInventoryUI, int> item in toAdd)
            {
                // Get item count. If we want to force this stack size, we just set the count manually
                (int, int) distributionCount = forceStackSize != -1 ? (0, forceStackSize) :
                    InventoryGridDistributor.GetItemStackDistribution(
                    item.Item1, 
                    _config, 
                    _items, 
                    item.Item2, 
                    true);
                
                // Now add rest of whole items
                for (int i = 0; i < distributionCount.Item1; i++)
                {
                    Tuple<Vector2Int, InventoryItemGridOrientation> results = 
                        InventoryGridDistributor.GetItemAutoAddPlacement(
                            item.Item1, 
                            _config, 
                            _items, 
                            _dragSource.CurrentItem.Item);
                    
                    if (results.Item1.x != -1 && results.Item1.y != -1)
                    {
                        DataItemGridPosition dataPosition = new DataItemGridPosition
                        {
                            Coords = results.Item1,
                            Orientation = (byte)results.Item2,
                            StackCount = item.Item1.StackSize
                        };
                        
                        if (asStaged)
                        {
                            addedItems.Add(AddStagedItem(item.Item1, dataPosition));
                        }
                        else
                        {
                            addedItems.Add(InstantiateAndAddItem(item.Item1, dataPosition, item.Item1.InstanceID));
                        }
                    }
                }
                
                // Now add stack remainder, if it exists
                if (distributionCount.Item2 > 0)
                {
                    Tuple<Vector2Int, InventoryItemGridOrientation> remResults = 
                        InventoryGridDistributor.GetItemAutoAddPlacement(
                            item.Item1, 
                            _config, 
                            _items, 
                            _dragSource.CurrentItem.Item);
                    
                    if (remResults.Item1.x != -1 && remResults.Item1.y != -1)
                    {
                        DataItemGridPosition dataPosition = new DataItemGridPosition
                        {
                            Coords = remResults.Item1,
                            Orientation = (byte)remResults.Item2,
                            StackCount = distributionCount.Item2
                        };
                        
                        if (asStaged)
                        {
                            addedItems.Add(AddStagedItem(item.Item1, dataPosition));
                        }
                        else
                        {
                            addedItems.Add(InstantiateAndAddItem(item.Item1, dataPosition, item.Item1.InstanceID));
                        }
                    }
                }
            }

            return addedItems;
        }

        public UIInventoryItem InstantiateAndAddItem(
            DataItemInventoryUI data, 
            DataItemGridPosition dataPosition,
            string instanceID)
        {
            UIInventoryItem item = Instantiate(data.InventoryPrefab.gameObject, _itemParent).GetComponent<UIInventoryItem>();
            item.Init(this, data, dataPosition.StackCount);
            item.SetToGrid(this, dataPosition.Coords, dataPosition.Orientation);
            item.InstanceID = instanceID;
            
            if (item is UIInventoryItemContainer container)
            {
                container.SpawnContainerGrid(transform);
            }
            
            _items.Add(item);
            return item;
        }

        private IInventoryOccupant AddStagedItem(
            DataItemInventoryUI item, 
            DataItemGridPosition positionData)
        {
            IInventoryOccupant stub = CreateStagedItem(item, positionData, _config.GridWidth, item.InstanceID);
            _items.Add(stub);
            _stagedItems.Add(stub);

            return stub;
        }
        
        public void AddStagedItem(IInventoryOccupant item)
        {
            _items.Add(item);
            _stagedItems.Add(item);
        }

        internal static IInventoryOccupant CreateStagedItem(
            DataItemInventoryUI data,
            DataItemGridPosition positionData,
            int gridWidth,
            string instanceID,
            int subgridIndex = 0)
        {
            IInventoryOccupant stub;
            if (data is DataItemContainerInventoryUI container)
            {
                stub = new UIInventoryGridItemContainerStub(container, positionData, gridWidth, subgridIndex);
            }
            else
            {
                stub = new UIInventoryGridItemStub(data, positionData, gridWidth);
            }

            stub.InstanceID = instanceID;
            return stub;
        }
        
        /// <summary>
        /// Converts staged (data-only) inventory items into actual gameObject representations.
        /// Will clear the staged items when finished.
        /// </summary>
        public IEnumerator InstantiateStagedItems(IInventoryOccupant draggingItem)
        {
            // TODO: WHY IS THIS NECCESSARY? WAITING FOR UI LAYOUT REBUILD AFTER ENABLING?? FORCING DOESN'T SEEM TO WORK
            // POSSIBLE ANSWER: https://forum.unity.com/threads/force-immediate-layout-update.372630/
            yield return null;
            yield return null;
            
            foreach (IInventoryOccupant staged in _stagedItems)
            {
                _items.Remove(staged);
                UIInventoryItem thisItem = InstantiateAndAddItem(staged.Data, staged.GridPosition, staged.InstanceID);
                
                // If this item is a container, we want to set up its staged items
                if (thisItem is UIInventoryItemContainer container)
                {
                    container.StageContainerGridItems(staged.SubOccupants);
                }
                
                // If we flagged this item to be dragged when the inventory is opened, let's do that now
                if (draggingItem != null && draggingItem == staged)
                {
                    _dragSource.StageDraggingItem(thisItem, this);
                }
            }
            
            _stagedItems.Clear();
        }

        #endregion SPAWN
    }
}