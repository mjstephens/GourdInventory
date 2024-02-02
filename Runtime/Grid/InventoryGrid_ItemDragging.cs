using System;
using System.Collections.Generic;
using GalaxyGourd.Grid;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Parital piece of InventoryItemGrid; Enables item dragging and moving between grid views
    /// </summary>
    public partial class InventoryGrid
    {
        #region GRAB N DRAG
        
        protected override void OnTick(float delta)
        {
            // If we begin dragging on mobile and we DON'T have an item...
            if (_mobileControls && !_mobileDragCancelFlag)
            {
                if (MobileHoldFlag && _dragSource.CurrentItem.Item == null)
                {
                    _mobileIsDraggingNoItem = true;
                    return;
                }
                
                _mobileIsDraggingNoItem = false;
            }
            
            if (GridIsItemCurrent())
            {
                MoveGrabbedItem(_dragSource.CurrentItem.Item.GridView);
                _dragSource.CurrentItem.Item.Tick(delta);

                EvaluateItemDrag(delta);
                _previousItemDragPosition = _dragSource.CurrentItem.Item.GridView.transform.localPosition;
            }

            // We want to tick the other items in our container, in case of passive effects
            foreach (IInventoryOccupant item in _items)
            {
                if (item is UIInventoryItem inv &&
                    _dragSource.CurrentItem.Item != inv &&
                    inv.gameObject.activeInHierarchy)
                {
                    inv.Tick(delta);
                }
            }
        }

        private void EvaluateItemDrag(float delta)
        {
            // Get the distance we have dragged this item since last frame
            Vector3 thisPosition = _dragSource.CurrentItem.Item.GridView.transform.localPosition;
            float dragDistance = Vector3.Distance(thisPosition, _previousItemDragPosition);
            
            // Have we started dragging yet?
            if (dragDistance >= 3 && !_dragSource.CurrentItem.DragHasMovedForCurrentContainer)
            {
                SetCurrentDragItemDragHoldState(true);
            }

            // If we have been holding still long enough...
            float currentDrag = _dragSource.CurrentItem.DragHoldStillElapsed;
            currentDrag = dragDistance < 3 ? (currentDrag + delta) : 0;
            if (currentDrag >= (_dragSource.CurrentItem.DragHasMovedForCurrentContainer ? 0.2f : 0.8f))
            {
                currentDrag = 0;
                EvaluateItemDragHold();
            }
            
            // Set drag value
            SetCurrentDragItemDragHoldTime(currentDrag);
        }

        protected virtual void EvaluateItemDragHold()
        {
            // We only care about this control when on mobile
            if (!_mobileControls)
                return;
            
            // If we are dragged onto a container, we want to open it to allow for manual item placement
            if (_currentHovered is UIInventoryItemContainer container)
            {
                (this as IItemReceivable).OpenContainerItem(container);
            }
        }
        
        public void GrabItem(UIInventoryItem item, int grabbedCount)
        {
            // If we are not grabbing the entire stack
            if (grabbedCount < item.StackCount)
            {
                // Remove the grabbed count from the source item
                item.RemoveFromStack(grabbedCount);
                
                DataItemGridPosition dataPosition = new DataItemGridPosition
                {
                    Coords = item.GridView.Coords,
                    Orientation = (byte)item.GridView.Orientation,
                    StackCount = grabbedCount
                };
                
                // Create a new item with the amount grabbed
                item = InstantiateAndAddItem(item.Data, dataPosition, item.InstanceID);
            }
            else
            {
                item.OnItemGrabbed(this);
            }
            
            // Grab the item
            _dragSource.DoItemGrab(item, grabbedCount, this);
            
            // Set grid grabbed metadata
            _currentGrabbedGrid.OriginalGridIndices = item.GridView.OccupyingIndices;
            _currentGrabbedGrid.OriginalOrientation = item.GridView.Orientation;
            _currentGrabbedGrid.CurrentGridIndices = item.GridView.OccupyingIndices;
            _currentGrabbedGrid.OriginalCoords = item.GridView.Coords;
        }

        private void MoveGrabbedItem(InventoryItemViewGrid item)
        {
            bool insideGrid = ItemIsInsideGridRect(_dragSource.CurrentItem.Item.GridView);
            if (!insideGrid)
            {
                ResetCurrentGridIndicesState();
                _currentGrabbedGrid.CurrentGridIndices = new List<int>();
                _currentGrabbedGrid.CurrentPlacementResult = ItemGridPlacementResult.OffGrid;

                return;
            }

            // Find the grid cells currently "under" the hovering item
            GridCell hoveredCell = _input.GetCellClosestToPosition(item.Origin).Item1;
            _currentGrabbedGrid.HoveredOrigin = hoveredCell;
            UpdateGrabbedActiveCells();
        }

        private void UpdateGrabbedActiveCells()
        {
            List<int> itemIndices = GetGridIndicesForItem(
                _dragSource.CurrentItem.Item, 
                _grid.GetCoordsForFlattenedIndex(_currentGrabbedGrid.HoveredOrigin.Index));
            
            ItemGridPlacementResult placementResults = GetItemPlacementResult(
                _dragSource.CurrentItem.Item, 
                _dragSource.CurrentItem.Item.Data,
                _grid.GetCoordsForFlattenedIndex(_currentGrabbedGrid.HoveredOrigin.Index));
            _currentGrabbedGrid.CurrentPlacementResult = placementResults;
            
            if (placementResults != ItemGridPlacementResult.Clear)
                ResetCurrentGridIndicesState();

            switch (placementResults)
            {
                case ItemGridPlacementResult.OffGrid or ItemGridPlacementResult.OverlappingExisting:
                    _currentGrabbedGrid.CurrentGridIndices = new List<int>();
                    return;
                case ItemGridPlacementResult.Clear:
                {
                    foreach (int index in itemIndices)
                    {
                        ((InventoryGridCell)_cellViews[index]).SetItemState(InventoryGridCell.ItemMovingState.Hover);
                    }
                    break;
                }
                case ItemGridPlacementResult.OverlappingStackable:
                    // TODO: Have the item we are stacking into pulse or something
                    break;
            }

            foreach (int index in _currentGrabbedGrid.CurrentGridIndices)
            {
                if (!itemIndices.Contains(index))    
                    ((InventoryGridCell)_cellViews[index]).SetItemState(InventoryGridCell.ItemMovingState.None);
            }

            _currentGrabbedGrid.CurrentGridIndices = itemIndices;
        }

        /// <summary>
        /// When selecting drop item, we may perform different contextual actions
        /// </summary>
        private void EvaluateDropItemCommand(int amountToDrop)
        {
            // If we are hovering over a container, we want to open the container instead of dropping
            if (_currentHovered && _currentHovered is UIInventoryItemContainer container)
            {
                (this as IItemReceivable).OpenContainerItem(container);
                return;
            }
            
            DropItem(amountToDrop);
        }
        
        private void DropItem(int amountToDrop)
        {
            _dragSource.CurrentItem.Item.OnItemDropped();
            _itemsModifiedFlag = true;
            
            bool offgrid = false;
            switch (_currentGrabbedGrid.CurrentPlacementResult)
            {
                case ItemGridPlacementResult.OffGrid:
                    offgrid = true;
                    OnItemDropInvalidOffGrid();
                    break;
                case ItemGridPlacementResult.Clear:
                    OnItemDropClear(amountToDrop);
                    break;
                case ItemGridPlacementResult.OverlappingStackable:
                    OnItemDropOntoStack(_currentGrabbedGrid.OverlappingItems[0], amountToDrop);
                    break;
                case ItemGridPlacementResult.OverlappingExisting:
                    OnItemDropInvalidOverlapping();
                    break;
            }

            // If we're over an item, we want to manually re-hover so we can grab again ASAP
            if (!offgrid)
            {
                RefreshCellPointer();
            }
        }
        
        private void OnItemDropInvalidOffGrid()
        {
            _dragSource.DoItemRelease();
            _dragSource.DoItemReset(this);
        }
        
        private void OnItemDropInvalidOverlapping()
        {
            _dragSource.DoItemRelease();
            _dragSource.DoItemReset(this);
            ClearDraggingItem();
        }

        private void OnItemDropClear(int amount)
        {
            // In containers, we don't want to auto-set the pointer cell as this overrides container closing functionality
            if (this is not InventoryGridContainer)
            {
                _pointerCell = _currentGrabbedGrid.HoveredOrigin;
            }
            
            // If we are dropping the whole stack, we can just transfer the dragging item to the drop position
            if (amount == _dragSource.CurrentItem.GrabbedStackCount)
            {
                // If this is not the original source
                if (!GridIsItemSource())
                {
                    // Add this item to this grid
                    _items.Add(_dragSource.CurrentItem.Item);
                    
                    // Tell the source we have transferred the item out
                    _dragSource.CurrentItem.Source?.OnItemTransferredOut(_dragSource.CurrentItem);
                }
                
                _dragSource.CurrentItem.Item.GridView.SetGridPosition(
                    _grid.GetCoordsForFlattenedIndex(_currentGrabbedGrid.HoveredOrigin.Index), true);
                
                _dragSource.DoItemRelease();
                _dragSource.DoItemDrop(true);
                ClearDraggingItem();

                return;
            }
            
            // If we are dropping only a partial amount, we want to create a new item and keep the dragging item
            _dragSource.CurrentItem.Item.RemoveFromStack(amount);
            
            // Set position data for item
            DataItemGridPosition dataPosition = new DataItemGridPosition
            {
                Coords = _grid.GetCoordsForFlattenedIndex(_currentGrabbedGrid.HoveredOrigin.Index),
                Orientation = (byte)_dragSource.CurrentItem.Item.GridView.Orientation,
                StackCount = amount
            };
            
            InstantiateAndAddItem(
                _dragSource.CurrentItem.Item.Data, 
                dataPosition, 
                _dragSource.CurrentItem.Item.InstanceID);
            
            _dragSource.DoItemDrop(false);
            
            // Correct number of currently dragged items
            RemoveItemsFromCurrentlyDragging(amount);
        }

        private void OnItemDropOntoStack(IInventoryOccupant targetStack, int amount)
        {
            // We can only drop up to the limit of the target stack
            int numToDrop = amount;
            if (targetStack.GridPosition.StackCount + numToDrop > targetStack.StackSize)
            {
                numToDrop = targetStack.StackSize - targetStack.GridPosition.StackCount;
            }
            
            // Are we dropping all of our items?
            if (numToDrop == _dragSource.CurrentItem.GrabbedStackCount)
            {
                targetStack.TryAddToStack(amount);

                if (!GridIsItemSource())
                {
                    _dragSource.CurrentItem.Source?.OnItemTransferredOut(_dragSource.CurrentItem);
                }
                else
                {
                    _items.Remove(_dragSource.CurrentItem.Item);
                }
                
                _dragSource.DoItemRelease();
                _dragSource.DoItemDrop(true);
                Destroy(_dragSource.CurrentItem.Item.gameObject);
            
                // Make sure our dragging item is cleared
                ClearDraggingItem();
                return;
            }
            
            // We are only dropping a partial amount
            targetStack.TryAddToStack(numToDrop);
            _dragSource.CurrentItem.Item.RemoveFromStack(numToDrop);
            
            _dragSource.DoItemRelease();
            _dragSource.DoItemDrop(false);
            RemoveItemsFromCurrentlyDragging(numToDrop);
        }
        
        private void OnItemResetInvalid()
        {
            // Try auto-adding the item to the next available indices
            Tuple<Vector2Int, InventoryItemGridOrientation> aa = 
                InventoryGridDistributor.GetItemAutoAddPlacement(
                    _dragSource.CurrentItem.Item.Data, 
                    _config, 
                    _items, 
                    _dragSource.CurrentItem.Item);
            if (aa.Item1.x == -1 || aa.Item1.y == -1)
            {
                // If we cannot auto-add, we need to resolve somehow
                Debug.Log("Failed to resolve item reset!");
                return;
            }
            
            // Drop the item into the automatically found spot
            _dragSource.CurrentItem.Item.GridView.ForceOrientation(aa.Item2);
            _dragSource.CurrentItem.Item.GridView.SetGridPosition(aa.Item1);
            ClearDraggingItem();
        }

        void IItemReceivable.OnItemResetToWorld()
        {
            Debug.Log("RESET TO WORLD! (GRID)");
            _items.Remove(_dragSource.CurrentItem.Item);
            OnItemDropped?.Invoke(_dragSource.CurrentItem);
            
            Destroy(_dragSource.CurrentItem.Item);
        }
        
        protected void OnItemAutoDropped()
        {
            _dragSource.DoItemRelease();
            _dragSource.CurrentItem.Item.DestroyItem();
            _mobileIsDraggingNoItem = false;
            ClearDraggingItem();
        }

        private void ClearDraggingItem()
        {
            _dragSource.ClearDraggingItem(_itemParent);
            ResetCurrentGridIndicesState();
        }

        #endregion GRAB N DRAG
        
        
        #region TRANSFER

        void IItemReceivable.TransferDraggingTo(DataDraggingItem item)
        {
            // Set our current dragging item
            DataDraggingItem i = _dragSource.CurrentItem;
            i.Current = this;
            _dragSource.CurrentItem = i;
            
            // Set item to grid view
            //item.Item.SetActiveView(UIInventoryItemViewType.Grid);
            item.Item.GridView.SetForGrid(this, item.Item.GridView.Orientation);
            item.Item.OnItemTransferredToNewSource(this);
            _previousItemDragPosition = _dragSource.CurrentItem.Item.GridView.transform.localPosition;
            _itemsModifiedFlag = true;

            // We need to create new grid dragging data for use on this grid, unless it originally belonged to us
            if (GridIsItemSource())
                return;

            Tuple<Vector2Int, InventoryItemGridOrientation> aa = 
                InventoryGridDistributor.GetItemAutoAddPlacement(item.Item.Data, _config, _items, _dragSource.CurrentItem.Item);
            ItemGridPlacementResult initialPlacement = GetItemPlacementResult(item.Item, item.Item.Data, aa.Item1);
            _currentGrabbedGrid = new DataDraggingItemGrid()
            {
                OriginalCoords = aa.Item1,
                OriginalGridIndices = new List<int>(),
                CurrentGridIndices = new List<int>(),
                OriginalOrientation = item.Item.GridView.Orientation,
                HoveredOrigin = _cellViews[0],
                CurrentPlacementResult = initialPlacement
            };
        }

        void IItemReceivable.TransferDraggingFrom(DataDraggingItem item)
        {
            ResetCurrentGridIndicesState();
            _itemsModifiedFlag = true;
        }

        void IItemReceivable.ResetItemToSource(DataDraggingItem item, bool snap)
        {
            // We may be resetting an item to inside a container that is now closed, which necessitates special behavior
            if (snap)
            {
                //item.Item.OnItemResetIntoClosedContainerGrid(container);
                item.Item.FlagItemForSnapReset();

                if (this is InventoryGridContainer { gameObject: { activeInHierarchy: false } } container)
                {
                    container.ItemSource.OnItemResetIntoThisClosedContainer(item.Item);
                }
            }
            
            item.Item.OnItemTransferredToNewSource(this);
            //item.Item.SetActiveView(UIInventoryItemViewType.Grid);
            item.Item.GridView.SetForGrid(this, _currentGrabbedGrid.OriginalOrientation);
            item.Item.GridView.SetPositionDirectly(item.CurrentDragPosition);

            // Are we resetting this item onto an existing stack? (ie if we grabbed a partial stack to begin with)
            foreach (IInventoryOccupant thisItem in _items)
            {
                if (!ReferenceEquals(thisItem, _dragSource.CurrentItem.Item) && 
                    thisItem.GridPosition.Coords == _currentGrabbedGrid.OriginalCoords)
                {
                    OnItemDropOntoStack(thisItem, _dragSource.CurrentItem.GrabbedStackCount);
                    return;
                }
            }

            // It's possible, if we dropped items along the way, that our original indices are not available for resetting
            // In this case we need to decide how to handle the drop since we cannot simply return to defaults
            if (InventoryGridDistributor.GetOverlappingItems(
                    _items, 
                    _dragSource.CurrentItem.Item, 
                    _currentGrabbedGrid.OriginalGridIndices).Count > 0)
            {
                OnItemResetInvalid();
                return;
            }
            
            _dragSource.CurrentItem.Item.GridView.ForceOrientation(_currentGrabbedGrid.OriginalOrientation);
            _dragSource.CurrentItem.Item.GridView.SetGridPosition(_currentGrabbedGrid.OriginalCoords);
            ClearDraggingItem();
        }

        void IItemReceivable.OnItemTransferredOut(DataDraggingItem item)
        {
            _items.Remove(_dragSource.CurrentItem.Item);
            _itemsModifiedFlag = true;
        }

        #endregion TRANSFER
    }
}