using System;
using System.Collections.Generic;
using GalaxyGourd.Grid;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Parital piece of InventoryItemGrid; Receives input related to the function of the inventory grid
    /// </summary>
    public partial class InventoryGrid : IUIInventoryInputReceiver
    {
        #region POINTER

        public override void DoGridPointerEnter()
        {
            base.DoGridPointerEnter();
            
            // Does the pointer for this operator have control of any dragging items?
            if (_dragSource && _dragSource.CurrentItem.Item && !GridIsItemCurrent()) 
            {
                // Transfer ownership from source to here
                _dragSource.CurrentItem.Current?.TransferDraggingFrom(_dragSource.CurrentItem);
                (this as IItemReceivable).TransferDraggingTo(_dragSource.CurrentItem);
            }
        }
        
        public override void DoGridPointerExit()
        {
            base.DoGridPointerExit();
            
            // Reset drag elapsed time
            DataDraggingItem d = _dragSource.CurrentItem;
            d.DragHoldStillElapsed = 0;
            _dragSource.CurrentItem = d;
            
            if (_mobileControls && _pointerCell)
            {
                DoCellPointerExit(_pointerCell);
            }
            
            _pointerCell = null;
            
            if (_currentHovered)
            {
                _dragSource.DoItemHoverExit(_currentHovered, _currentHovered.StackCount, this);
                _currentHovered = null;
            }
        }

        internal void RefreshCellPointer()
        {
            if (_pointerCell)
            {
                DoCellPointerEnter(_pointerCell);
            }
        }

        public override void DoCellPointerEnter(GridCell cell)
        {
            base.DoCellPointerEnter(cell);

            _pointerCell = cell;
            UIInventoryItem item = InventoryGridDistributor.GetItemInCell(cell.Index, _items) as UIInventoryItem;

            if (item == null || item == _dragSource.CurrentItem.Item)
            {
                if (_currentHovered)
                {
                    _dragSource.DoItemHoverExit(_currentHovered, _currentHovered.StackCount, this);
                    _currentHovered = null;
                }
                
                return;
            }

            if (item != _currentHovered && _dragSource.CurrentItem.Item != item)
            {
                if (_currentHovered != null)
                {
                    _dragSource.DoItemHoverExit(_currentHovered, _currentHovered.StackCount, this);
                }
                
                if (_mobileIsDraggingNoItem)
                    return;
                
                _dragSource.DoItemHoverEnter(item, item.StackCount, this);
                _currentHovered = item;
            }
        }

        #endregion POINTER


        #region INVENTORY

        void IUIInventoryInputReceiver.OnInputRotateItemRight()
        {
            UIInventoryItem item = _dragSource.CurrentItem.Item;
            if (item && item.CanBeRotated() && GridIsItemCurrent())
            {
                _dragSource.CurrentItem.Item.GridView.RotateItem(false);
            }
        }
         
        void IUIInventoryInputReceiver.OnInputRotateItemLeft()
        {
            UIInventoryItem item = _dragSource.CurrentItem.Item;
            if (item && item.CanBeRotated() && GridIsItemCurrent())
            {
                _dragSource.CurrentItem.Item.GridView.RotateItem(true);
            }
        }

        public virtual void OnInputGrabOrDropItem()
        {
            _mobileDragCancelFlag = false;
            if (_mobileIsDraggingNoItem)
            {
                _mobileIsDraggingNoItem = false;
                DoGridPointerExit();
                return;
            }
            
            // If we're holding an item, drop it; if not, pick it up
            if (_dragSource.CurrentItem.Item && GridIsItemCurrent())
            {
                EvaluateDropItemCommand(_dragSource.CurrentItem.GrabbedStackCount);

                if (_mobileControls)
                {
                    _mobileIsDraggingNoItem = false;
                    DoGridPointerExit();
                }
            }
            else if (_currentHovered)
            {
                // Grab the item
                GrabItem(_currentHovered, _currentHovered.StackCount);
                
                // Grabbed items cannot be hovered
                _dragSource.DoItemHoverExit(_currentHovered, _currentHovered.StackCount, this);
                _currentHovered = null;
            }
            // If we get this far and we're on mobile, it means we have released
            else if (_mobileControls)
            {
                DoGridPointerExit();
            }
        }
        
        public virtual void OnInputGrabItemSplitStack()
        {
            // Are we hovering over a container?
            if (_currentHovered is UIInventoryItemContainer container)
            {
                EvaluateInputGrabItemSplitStackOverContainer(container);
                
                if (_mobileControls)
                {
                    DoGridPointerExit();
                }
                
                return;
            }
            
            if (GridIsItemCurrent())
            {
                EvaluateDropItemCommand(1);
                
                if (_mobileControls)
                {
                    DoGridPointerExit();
                }
                
                return;
            }
            
            if (!_dragSource.CurrentItem.Item && _currentHovered) 
            {
                if (_mobileControls)
                {
                    _currentHovered.SelectItem();
                    DoGridPointerExit();
                    return;
                }
                
                if (_currentHovered.IsSplittable())
                {
                    // Grab half of the stack
                    GrabItem(_currentHovered, _currentHovered.StackCount / 2);
                    _currentHovered = null;
                }
            }
        }

        private void EvaluateInputGrabItemSplitStackOverContainer(UIInventoryItemContainer receivingContainer)
        {
            SetCurrentDragItemDragHoldState(false);

            // If we have an item grabbed, we want to auto-add it. Otherwise we open the container
            if (GridIsItemCurrent())
            {
                // Even though we're only dropping one item, we need to create this data structure to auto-add it correctly
                List<Tuple<DataItemInventoryUI, int>> items = new()
                {
                    new Tuple<DataItemInventoryUI, int>(
                        _dragSource.CurrentItem.Item.Data, 
                        _dragSource.CurrentItem.GrabbedStackCount)
                };

                switch (receivingContainer.ContainerGrid.GetAutoAddResults(items))
                {
                    case ItemGridAutoAddResults.FullyFits:
                        
                        // Drop the item into the container
                        receivingContainer.OnAutoDropItemIntoContainer(_dragSource.CurrentItem);
                    
                        // Make sure the item is removed from the original grid
                        _dragSource.CurrentItem.Source.RemoveItemExplicit(_dragSource.CurrentItem.Item);
                    
                        (this as IItemReceivable).OnItemTransferredOut(_dragSource.CurrentItem);
                        OnItemAutoDropped();
                    
                        if (_mobileControls)
                        {
                            DoGridPointerExit();
                        }

                        break;
                    
                    case ItemGridAutoAddResults.PartiallyFits:
                        
                        // We know that we can drop SOME of our stack, but we can't drop all of it. We need to figure out how
                        // much of the stack to drop
                        (int, int) distributionCount = InventoryGridDistributor.GetItemStackDistribution(
                            items[0].Item1, 
                            receivingContainer.ContainerGrid._config, 
                            receivingContainer.ContainerGrid.Items, 
                            items[0].Item2, 
                            false);

                        // Change the current dragging item to reflect the new stack count
                        int originalStackCount = _dragSource.CurrentItem.GrabbedStackCount;
                        int remainingOnDrag = distributionCount.Item2;
                        DataDraggingItem changed = _dragSource.CurrentItem;
                        changed.GrabbedStackCount = remainingOnDrag;
                        _dragSource.CurrentItem = changed;
                        _dragSource.CurrentItem.Item.SetStackCountExplicit(remainingOnDrag);
                        
                        // Now drop the remainder into the original container
                        DataDraggingItem dropItem = _dragSource.CurrentItem;
                        dropItem.GrabbedStackCount = originalStackCount - remainingOnDrag;
                        receivingContainer.OnAutoDropItemIntoContainer(dropItem);
                        
                        break;
                    
                    case ItemGridAutoAddResults.NoneFits:

                        Debug.Log("Container is full!");
                        if (_mobileControls)
                        {
                            OnItemDropInvalidOffGrid();
                            DoGridPointerExit();
                        }
                        
                        break;
                }

                return;
            }
            
            (this as IItemReceivable).OpenContainerItem(receivingContainer);
        }

        void IUIInventoryInputReceiver.OnInputGrabItemCancel()
        {
            // If we're dragging something, we should release it
            if (_dragSource.CurrentItem.Item && GridIsItemCurrent())
            {
                OnItemDropInvalidOffGrid();
                return;
            }
            
            // If we're hovered over an item, we want to drop it
        }

        void IUIInventoryInputReceiver.OnInputAutoEquip(BoolAction input)
        {
            
        }

        #endregion INVENTORY
    }
}