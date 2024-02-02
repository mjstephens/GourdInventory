using System;
using GalaxyGourd.Grid;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    ///  We define a class to keep track of any inventory objects we are dragging
    /// </summary>
    public class InventoryDragSource : MonoBehaviour
    {
        #region PROPERTIES

        internal RectTransform PointerRect => GetPointerRect();

        public DataDraggingItem CurrentItem { get; set; }
        public Action<UIInventoryItem> OnItemHoverEnter;
        public Action<UIInventoryItem> OnItemHoverExit;
        public Action<UIInventoryItem> OnItemSelect;
        public Action<UIInventoryItem> OnItemDeselect;

        private RectTransform _grabItemParent;
        private GridInputUI _input;

        #endregion PROPERTIES


        #region INITIALIZATION

        internal void Init(GridInputUI input)
        {
            _input = input;
        }
        
        public void SetDragParent(RectTransform parent)
        {
            _grabItemParent = parent;
        }

        #endregion INITIALIZATION


        #region DRAGGING

        public void DoItemHoverEnter(UIInventoryItem item, int stackCount, IItemReceivable source)
        {
            item.SetPointerState(InventoryItemPointerState.Hovered);
            OnItemHoverEnter?.Invoke(item);
        }
        
        public void DoItemHoverExit(UIInventoryItem item, int stackCount, IItemReceivable source)
        {
            item.SetPointerState(InventoryItemPointerState.None);          
            OnItemHoverExit?.Invoke(item);
        }

        public void DoItemGrab(UIInventoryItem item, int stackCount, IItemReceivable source)
        {
            CurrentItem = new DataDraggingItem()
            {
                Item = item,
                GrabbedStackCount = stackCount,
                Source = source,
                Current = source,
                GrabOffset = GetGrabOffsetScreenSpace(item.GridView.RectTrans)
            };
            
            OnItemSelect?.Invoke(item);
            item.SetPointerState(InventoryItemPointerState.Grabbed);
            item.transform.SetParent(_grabItemParent);
        }

        /// <summary>
        /// When an item is released, regardless of drop outcome
        /// </summary>
        public void DoItemRelease()
        {
            OnItemDeselect?.Invoke(CurrentItem.Item);
        }
        
        /// <summary>
        /// When an item is successfully dropped to a new location
        /// </summary>
        public void DoItemDrop(bool wasFullStack)
        {
           // Debug.Log("DROP (" + wasFullStack + "): " + CurrentItem.Item.name);
        }

        /// <summary>
        /// When an item that was being dragged is reset to it's original position/source
        /// </summary>
        /// <param name="resetSource">The source from which the reset is being initiated (ie the 'current' source)</param>
        public void DoItemReset(IItemReceivable resetSource)
        {
            // If there is no source, this item originated from the world and must be hence returned
            if (CurrentItem.Source == null)
            {
                resetSource.OnItemResetToWorld();
                return;
            }
            
            CurrentItem.Source.ResetItemToSource(CurrentItem, true);
        }

        private void Update()
        {
            // If we have an item selected, we want to drag it
            if (CurrentItem.Item)
            {
                MoveGrabbedItem(CurrentItem.Item);
            }
        }

        private void MoveGrabbedItem(UIInventoryItem item)
        {
            item.SetHoveredPosition(_input.InputValues.GridPointerPosition, _input.UICamera, CurrentItem.GrabOffset);

            DataDraggingItem d = CurrentItem;
            d.CurrentDragPosition = _input.InputValues.GridPointerPosition;
            CurrentItem = d;
        }
        
        public void ClearDraggingItem(Transform itemParent)
        {
            if (!CurrentItem.Item)
                return;
            
            CurrentItem.Item.transform.SetParent(itemParent);
            CurrentItem.Item.SetPointerState(InventoryItemPointerState.None);
            CurrentItem = new DataDraggingItem();
        }
        
        protected virtual Vector3 GetGrabOffsetScreenSpace(RectTransform itemView)
        {
            Vector3 pointerScreenPosition = _input.UICamera == null ?
                _input.InputValues.GridPointerPosition :
                _input.UICamera.WorldToScreenPoint(_input.InputValues.GridPointerPosition);
            Vector3 itemScreenPosition = 
                _input.UICamera == null ? 
                    itemView.position : 
                    _input.UICamera.WorldToScreenPoint(itemView.position);
            Vector3 offset = pointerScreenPosition - itemScreenPosition;

            return offset;
        }

        #endregion DRAGGING


        #region STAGING

        public virtual void StageDraggingItem(UIInventoryItem item, InventoryGrid grid)
        {
            // Grab the item
            grid.GrabItem(item, item.StackCount);
            
            CurrentItem = new DataDraggingItem
            {
                Item = item,
                GrabbedStackCount = item.StackCount,
                Source = grid,
                Current = grid,
                GrabOffset = Vector2.zero
            };
        }

        #endregion STAGING


        #region UTILITY

        protected virtual RectTransform GetPointerRect()
        {
            return new RectTransform()
            {
                anchoredPosition = Input.mousePosition,
                anchorMax = Vector2.one,
                anchorMin = -Vector2.one
            };
        }

        #endregion UTILITY
    }
}