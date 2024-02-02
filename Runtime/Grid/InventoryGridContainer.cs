using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Special override for grids that are container objects
    /// </summary>
    public class InventoryGridContainer : InventoryGrid
    {
        #region VARIABLES

        //[Header("Container")]
        ///[SerializeField] private TMP_Text _textContainerName;
        
        [Header("Transition")]
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private AnimationCurve _openCurve;
        [SerializeField] private float _openTime;
        [SerializeField] private AnimationCurve _closeCurve;
        [SerializeField] private float _closeTime;
        
        internal IItemReceivable ContainerRoot { get; private set; }    // The bottommost (root) item that contains this container
        internal IItemReceivable ContainerSource { get; private set; }
        internal UIInventoryItemContainer ItemSource { get; private set; }

        #endregion VARIABLES
        
        
        #region TRANSITION

        internal IEnumerator CR_OpenContainerView(string containerName)
        {
            StopAllCoroutines();

            //_textContainerName.text = containerName;
            transform.localScale = Vector3.zero;
            _group.alpha = 0;
            float timeElapsed = 0;
            while (timeElapsed < _openTime)
            {
                float progress = timeElapsed / _openTime;
                float curveProgress = _openCurve.Evaluate(progress);
                transform.localScale = Vector3.one * curveProgress;
                _group.alpha = curveProgress;
                timeElapsed += Time.deltaTime;

                yield return null;
            }

            // Once the container is open, we can set the initial pointer cell
            SetInitialPointerCellAfterOpen();
            
            _group.alpha = 1;
            transform.localScale = Vector3.one;
            SetCurrentDragItemDragHoldState(false);
        }

        internal IEnumerator CR_CloseContainerView()
        {
            StopAllCoroutines();
            transform.localScale = Vector3.one;
            _group.alpha = 1;
            float timeElapsed = 0;
            while (timeElapsed < _closeTime)
            {
                float progress = timeElapsed / _closeTime;
                float curveProgress = _closeCurve.Evaluate(progress);
                transform.localScale = Vector3.one * curveProgress;
                timeElapsed += Time.deltaTime;

                yield return null;
            }

            SetClosed();
        }

        internal void SetClosed()
        {
            _group.alpha = 0;
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
            SetCurrentDragItemDragHoldState(false);
        }

        #endregion TRANSITION


        #region CONTAINER

        internal void SetRootForContainer(IItemReceivable root)
        {
            // Containers cannot themselves be roots
            if (root is InventoryGridContainer container)
            {
                // We need to find the root of the container we just entered
                ContainerRoot = container.ContainerRoot;               
                return;
            }
            
            ContainerRoot = root;
            
            // Any other containers WITHIN this container must have their roots set as well
            foreach (IInventoryOccupant item in _items)
            {
                if (item is UIInventoryItemContainer c)
                {
                    c.SetContainerRootDirectly(root);
                }
            }
        }
        
        internal void SetSourceForContainer(IItemReceivable source)
        {
            ContainerSource = source;
        }

        internal void SetItemForContainer(UIInventoryItemContainer item)
        {
            ItemSource = item;
        }

        internal IEnumerator ResizeItemsForNewContainerSource()
        {            
            // TODO: Figure out why tf these returns are necessary. Without it, items will not be in correct position. Waiting for layout?
            yield return null;
            yield return null;

            foreach (IInventoryOccupant item in _items)
            {
                if (item is not UIInventoryItem inv) 
                    continue;
                
                Vector2Int coords = item.GridPosition.Coords;
                inv.GridView.SetForGrid(this, (InventoryItemGridOrientation)item.GridPosition.Orientation);
                inv.GridView.SetGridPosition(coords, true);
            }
        }

        internal InventoryItemView GetFirstVisibleViewForContainerStack(IItemReceivable source)
        {
            InventoryItemView view = ItemSource.GridView;
            while (!ItemSource.GridView.gameObject.activeInHierarchy)
            {
                source = ContainerSource;
                if (source is InventoryGridContainer container)
                {
                    view = container.GetFirstVisibleViewForContainerStack(source);
                }
            }
            
            return view;
        }

        #endregion CONTAINER


        #region INPUT

        protected override void OnTick(float delta)
        {
            base.OnTick(delta);
            
            // We only want the "top" container (if there are multiple stacked containers active)
            if (!CanReceiveInput)
                return;
            
            // Containers are sub-grids; we need to treat entering their root object the same as entering their own rect in order
            // for the container-specific functionality to work right
            Vector2 overlap = UIUtility.GetRectOverlapOutside(
                _gridRectCache1, 
                _gridRectCache2, 
                ContainerRoot.Rect, 
                _dragSource.PointerRect, 
                _input.UICamera);
            if (overlap == Vector2.zero)
            {
                DoGridPointerEnter();
            }
        }
        
        protected override void EvaluateItemDragHold()
        {
            // We only care about this control when on mobile
            if (!_mobileControls)
                return;
            
            // If we are hovered out of the container, we want to close it
            if (!(this as IItemReceivable).PointerIsInsideContentRect())
            {
                ContainerSource.CloseContainerItem();
                return;
            }
            
            base.EvaluateItemDragHold();
        }

        internal void SetInitialPointerCellAfterOpen()
        {
            // Is the pointer over the grid? If so, we want to refresh the pointer cell
            Vector2 overlap = UIUtility.GetRectOverlapOutside(
                _gridRectCache1, 
                _gridRectCache2, 
                _inputRect, 
                _dragSource.PointerRect,
                _input.UICamera);
            if (overlap == Vector2.zero && !_mobileControls)
            {
                _input.RefreshPointerPositionManual();
                RefreshCellPointer();
            }
            else
            {
                _pointerCell = null;
            }
        }

        public override void OnInputGrabOrDropItem()
        {
            if (ContainerSource == null || ContainerRoot == null)
                return;
            
            // If clicking outside the container, we want to close it whilst continuing to hold any items
            if ((GridIsItemCurrent() && _currentGrabbedGrid.CurrentPlacementResult == ItemGridPlacementResult.OffGrid) ||
                _pointerCell == null && ContainerRoot.PointerIsInsideContentRect())
            {
                // If we are on mobile and tapping inside a container, we don't want to close it
                if (!_mobileControls || !(this as IItemReceivable).PointerIsInsideContentRect())
                {
                    // If we are clicking outside of this container on mobile, with an item, we want to auto-drop the item but keep the 
                    // container open
                    if (_mobileControls)
                    {
                        AutoDropItemOutOfContainer();
                    }
                    else
                    {
                        ContainerSource.CloseContainerItem();
                    }
                    
                    return;
                }
            }
            
            base.OnInputGrabOrDropItem();
        }

        /// <summary>
        /// When we auto-drop an item out of this container (and into this container's source)
        /// </summary>
        private void AutoDropItemOutOfContainer()
        {
            if (ContainerSource is not InventoryGrid sourceGrid || _dragSource.CurrentItem.Item == null)
                return;
            
            // Even though we're only dropping one item, we need to create this data structure to auto-add it correctly
            List<Tuple<DataItemInventoryUI, int>> items = new()
            {
                new Tuple<DataItemInventoryUI, int>(
                    _dragSource.CurrentItem.Item.Data, 
                    _dragSource.CurrentItem.GrabbedStackCount)
            };
                
            // If there's room in the source to take the items we're trying to drop...
            if (sourceGrid.GetAutoAddResults(items) == ItemGridAutoAddResults.FullyFits)
            {
                // Make sure the item is removed from the original grid
                _dragSource.CurrentItem.Source.RemoveItemExplicit(_dragSource.CurrentItem.Item);
                
                // Drop the item into the container
                if (sourceGrid is InventoryGridContainer sourceContainer && sourceContainer.ItemSource)
                {
                    sourceContainer.ItemSource.OnAutoDropItemIntoContainer(_dragSource.CurrentItem);
                }
                else
                {
                    List<IInventoryOccupant> added = sourceGrid.AutoAddItems(items, false);
                    
                    // If we added a container, we want to make sure we add back in the items that were in the container originally
                    if (_dragSource.CurrentItem.Item is UIInventoryItemContainer original && 
                        added[0] is UIInventoryItemContainer newItem)
                    {
                        newItem.StageContainerGridItems(original.ContainerGrid.Items);
                    }
                }
                
                (this as IItemReceivable).OnItemTransferredOut(_dragSource.CurrentItem);
                OnItemAutoDropped();
            }
            else
            {
                Debug.Log("Container is full!");
            }

            DoGridPointerExit();
        }

        public override void OnInputGrabItemSplitStack()
        {
            // If we have no items held and aren't hovering over something relevant, we can exit the container
            if (!HoveredHasSplitStackSpecialInput() && ContainerRoot.PointerIsInsideContentRect())
            {
                if (!_dragSource.CurrentItem.Item)
                {
                    // If we are on mobile and tapping inside a container, we don't want to close it
                    if (!_mobileControls || !(this as IItemReceivable).PointerIsInsideContentRect())
                    {
                        ContainerSource.CloseContainerItem();
                        return;
                    }
                }
                else if (!_mobileControls)
                {
                    AutoDropItemOutOfContainer();
                    return;
                }
            }
            
            base.OnInputGrabItemSplitStack();
        }

        #endregion INPUT
    }
}