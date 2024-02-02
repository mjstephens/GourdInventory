using System;
using System.Collections;
using System.Collections.Generic;
using GalaxyGourd.Grid;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Core class for inventory grid instance; is split amongst multiple partial classes bc it was toooo long
    /// </summary>
    [RequireComponent(typeof(GridInputUI))]
    public partial class InventoryGrid : UIScalableGrid, IItemReceivable
    {
        #region VARIABLES

        [Header("Input")] 
        [SerializeField] private ActionMapListenerUIInventory _listener;
        
        [Header("Inventory")]
        [SerializeField] protected RectTransform _itemParent;
        [SerializeField] private GameObject _objCoverlay;
        
        public bool CanReceiveInput { get; private set; }
        public bool MobileHoldFlag { get; set; }
        public Action<List<IInventoryOccupant>> OnGridCompositionModified; // Called if the grid comp changed when grid closed
        public Action<DataDraggingItem> OnItemDropped;
        public RectTransform Rect => _contentRect;
        internal Transform ItemParent => _itemParent;
        public List<IInventoryOccupant> Items => _items;

        protected InventoryDragSource _dragSource;
        protected readonly List<IInventoryOccupant> _items = new();
        private readonly List<IInventoryOccupant> _stagedItems = new();
        protected GridCell _pointerCell;
        protected UIInventoryItem _currentHovered;
        protected DataDraggingItemGrid _currentGrabbedGrid;
        private bool _isSubbedToPointer;
        private UIInventoryItemContainer _currentContainerItem;
        private float _cellSizeScreenSpace;
        private IInventoryViewProvider _viewProvider;
        private bool _itemsModifiedFlag;    // True if item composition changed (used for triggering re-serialization)
        
        protected bool _mobileControls;                   // Touch controls requires different input logic
        private Vector3 _previousItemDragPosition;
        private bool _mobileIsDraggingNoItem;
        private bool _mobileDragCancelFlag;
        
        protected static readonly Vector3[] _gridRectCache1 = new Vector3[4];  // Cache for GetRectOverlapOutside
        protected static readonly Vector3[] _gridRectCache2 = new Vector3[4];  // Cache for GetRectOverlapOutside

        #endregion VARIABLES


        #region INITIALIZATION

        protected override void Awake()
        {
            base.Awake();

            _dragSource = FindObjectOfType<InventoryDragSource>();
            if (!_dragSource)
            {
                _dragSource = gameObject.AddComponent<InventoryDragSource>();
            }
            
            UIInventoryItemDragPanel drag = gameObject.AddComponent<UIInventoryItemDragPanel>();
            _dragSource.SetDragParent(drag.Rect);
            _dragSource.Init(_input);
            OnCellsUpdated += DoCellsUpdated;
            
            CanReceiveInput = true;
            DoCellsUpdated();
            
            // Check to make sure our data is of the correct type
            if (_config is not DataConfigUIInventoryGrid)
            {
                Debug.LogError("ERR - Inventory grid must have config of type DataConfigUIInventoryGrid");
            }
        }
        
        public void SetInputEnabled(bool b)
        {
            if (_input)
            {
                _input.SetGridInputEnabled(b);
            }
            CanReceiveInput = b;
        }

        private void DoCellsUpdated()
        {
            float point1 = 
                _input.UICamera == null ? 
                    _cellViews[0].transform.position.x : 
                    _input.UICamera.WorldToScreenPoint(_cellViews[0].transform.position).x;
            float point2 = 
                _input.UICamera == null ? 
                    _cellViews[1].transform.position.x : 
                    _input.UICamera.WorldToScreenPoint(_cellViews[1].transform.position).x;
            
            _cellSizeScreenSpace = Mathf.Abs(point1 - point2);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _listener.RegisterReceiver(this);
            if (this is not InventoryGridContainer)
            {
                _viewProvider?.OnInventoryOpened();
            }
        }

        private void OnDisable()
        {
            _listener.UnregisterReceiver(this);
        }

        public void OnCloseInventory()
        {
            // Are we currently dragging an item? 
            if (_dragSource != null && _dragSource.CurrentItem.Item)
            {
                _dragSource.DoItemReset(this);
            }

            // If we modified the composition of the grid, we want to notify 
            if (_itemsModifiedFlag)
            {
                _itemsModifiedFlag = false;
                List<IInventoryOccupant> allItems = new List<IInventoryOccupant>();
                allItems.AddRange(_items);
                
                OnGridCompositionModified?.Invoke(allItems);
            }
        }

        #endregion INITIALIZATION


        #region CONTAINER

        void IItemReceivable.OpenContainerItem(UIInventoryItemContainer containerItem)
        {
            if (_currentHovered != null)
            {
                _dragSource.DoItemHoverExit(_currentHovered, _currentHovered.StackCount, this);
                _currentHovered = null;
            }
            
            _currentContainerItem = containerItem;
            SetInputEnabled(false);
            _objCoverlay.SetActive(true);
            
            // Tell the container to show its grid
            StartCoroutine(_currentContainerItem.OnContainerOpen(this, CellSize));
        }

        void IItemReceivable.CloseContainerItem()
        {
            _objCoverlay.SetActive(false);
            _currentContainerItem.OnContainerClose();
            _currentContainerItem = null;

            // We wait a frame to prevent input weirdness
            StartCoroutine(CR_WaitToEnableInput());
            IEnumerator CR_WaitToEnableInput()
            {
                yield return null;
                
                SetInputEnabled(true);
                DoGridPointerEnter();
                if (this is InventoryGridContainer container)
                {
                    container.SetInitialPointerCellAfterOpen();
                }
                else
                {
                    RefreshCellPointer();
                }
                
                if (_dragSource.CurrentItem.Item)
                {
                    SetCurrentDragItemDragHoldTime(0);
                    _previousItemDragPosition = _dragSource.CurrentItem.Item.GridView.transform.localPosition;
                }
            }
        }
        
        internal void OnForceContainersClosed()
        {
            _objCoverlay.SetActive(false);
            _currentContainerItem = null;
        }

        #endregion CONTAINER


        #region UTILITY

        void IItemReceivable.RemoveItemExplicit(UIInventoryItem item)
        {
            if (!_items.Contains(item))
                return;
            
            _items.Remove(item);
            item.DestroyItem();
        }

        private List<int> GetGridIndicesForItem(IInventoryOccupant item, Vector2Int coords)
        {
            // Get orientation-adjusted axises
            List<int> itemIndices = new List<int>();
            int xAxis = (InventoryItemGridOrientation)item.GridPosition.Orientation is 
                InventoryItemGridOrientation.Up or InventoryItemGridOrientation.Down ? 
                item.Dimensions.x : 
                item.Dimensions.y;
            int yAxis = xAxis == item.Dimensions.x ? item.Dimensions.y : item.Dimensions.x;
            
            for (int i = 0; i < xAxis; i++)
            {
                for (int e = 0; e < yAxis; e++)
                {
                    Vector2Int thisCoords = new Vector2Int(coords.x + i, coords.y + e);
                    if (!_grid.CoordsAreWithinGrid(thisCoords))
                        return null;
                    itemIndices.Add(_grid.GetFlattenedIndexForCoords(thisCoords.x, thisCoords.y));
                }
            }

            return itemIndices;
        }

        private ItemGridPlacementResult GetItemPlacementResult(
            IInventoryOccupant item, 
            DataItemInventoryUI data, 
            Vector2Int coords)
        {
            // Get indices; bail if they include off-grid indices
            List<int> itemIndices = GetGridIndicesForItem(item, coords);
            if (itemIndices == null) 
                return ItemGridPlacementResult.OffGrid;
            
            // Get any overlapping items
            List<IInventoryOccupant> overlaps = 
                InventoryGridDistributor.GetOverlappingItems(_items, _dragSource.CurrentItem.Item, itemIndices);
            
            // Set the overlapping items
            DataDraggingItemGrid c = _currentGrabbedGrid;
            c.OverlappingItems = overlaps;
            _currentGrabbedGrid = c;

            // If there's nothing overlapping, we know we're clear
            if (overlaps.Count <= 0)
                return ItemGridPlacementResult.Clear;

            // If we know there are overlapping items, we need to see if any are stackable
            return InventoryGridDistributor.GetValidStackableOverlappingItem(
                item, 
                data, 
                _currentGrabbedGrid.OverlappingItems) != null ? 
                    ItemGridPlacementResult.OverlappingStackable :
                    ItemGridPlacementResult.OverlappingExisting;
        }

        private bool ItemIsInsideGridRect(InventoryItemViewGrid item)
        {
            Vector2 outsideOverlap = UIUtility.GetRectOverlapOutside(
                _gridRectCache1, 
                _gridRectCache2, 
                _cellsRect, 
                item.RectTrans, 
                _input.UICamera, 
                _cellSizeScreenSpace);
            return outsideOverlap is { x: 0, y: 0 };
        }

        private bool GridIsItemSource()
        {
            return ReferenceEquals(_dragSource.CurrentItem.Source, this);
        }
        
        protected bool GridIsItemCurrent()
        {
            return ReferenceEquals(_dragSource.CurrentItem.Current, this);
        }

        private void ResetCurrentGridIndicesState()
        {
            if (_currentGrabbedGrid.CurrentGridIndices == null)
                return;
            
            foreach (int index in _currentGrabbedGrid.CurrentGridIndices)
            {
                ((InventoryGridCell)_cellViews[index]).SetItemState(InventoryGridCell.ItemMovingState.None);
            }
        }

        private void RemoveItemsFromCurrentlyDragging(int amount)
        {
            DataDraggingItem c = _dragSource.CurrentItem;
            c.GrabbedStackCount -= amount;
            _dragSource.CurrentItem = c;
        }

        /// <summary>
        /// Forces the rect to move to fit within the bounds of this grid's content rect
        /// </summary>
        /// <param name="rect">The rect we want to be fit within this grid</param>
        internal void FitRectInsideGridContent(RectTransform rect)
        {
            // Gets overlap in SCREEN SPACE
            Vector3 overlapOutside = UIUtility.GetRectOverlapOutside(
                _gridRectCache1, 
                _gridRectCache2, 
                _contentRect, 
                rect, 
                _input.UICamera);
            Vector3 ssPosition = _input.UICamera.WorldToScreenPoint(rect.position) + overlapOutside;
            rect.position = _input.UICamera.ScreenToWorldPoint(ssPosition);
        }
        
        bool IItemReceivable.PointerIsInsideContentRect()
        {
            Vector2 overlap = UIUtility.GetRectOverlapOutside(
                _gridRectCache1, 
                _gridRectCache2,
                _contentRect, 
                _dragSource.PointerRect, 
                _input.UICamera);
            return overlap == Vector2.zero;
        }

        /// <summary>
        /// Returns true if the current hovered item has special response to split stack input
        /// </summary>
        protected bool HoveredHasSplitStackSpecialInput()
        {
            if (!_currentHovered)
                return false;
            
            return _currentHovered.IsSplittable() || _currentHovered is UIInventoryItemContainer;
        }
        
        protected void SetCurrentDragItemDragHoldState(bool hasHeld)
        {
            if (_dragSource == null) 
                return;
            
            // Reset drag elapsed time
            DataDraggingItem d = _dragSource.CurrentItem;
            d.DragHasMovedForCurrentContainer = hasHeld;
            _dragSource.CurrentItem = d;
        }

        protected void SetCurrentDragItemDragHoldTime(float holdTime)
        {
            DataDraggingItem d = _dragSource.CurrentItem;
            d.DragHoldStillElapsed = holdTime;
            _dragSource.CurrentItem = d;
        }

        #endregion UTILITY
    }
}