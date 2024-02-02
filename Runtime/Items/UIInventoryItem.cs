using System.Collections.Generic;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Represents an instantiated instance of an inventory item in the UI (single or stack)
    /// </summary>
    public class UIInventoryItem : MonoBehaviour, IInventoryOccupant
    {
        #region VARIABLES

        [Header("Views")]
        [SerializeField] private GameObject _gridViewPrefab;
        [SerializeField] private InventoryItemViewGrid _gridView;

        public InventoryItemPointerState PointerState { get; private set; }
        public InventoryItemViewGrid GridView => _gridView;
        public string InstanceID { get; set; }
        public DataItemInventoryUI Data { get; private set; }
        public int ChildSubgrid { get; set; }
        public DataItemGridPosition GridPosition => new()
        {
            Coords = GridView.Coords,
            Orientation = (byte)GridView.Orientation,
            StackCount = _stackCount
        };
        public List<IInventoryOccupant> SubOccupants { get; set;  }
        public bool Equipped { get; set; }
        public List<int> OccupyingIndices => GridView.OccupyingIndices;
        public ItemEquipSlotType Slot => Data.Slot;
        public int StackCount => GridPosition.StackCount;

        protected IItemReceivable _source;
        private int _stackCount;
        private InventoryItemViewGrid _gridGhost;
        private bool _updatePositionOnEnable;

        #endregion VARIABLES


        #region INITIALIZATION

        internal void Init(
            IItemReceivable source,
            DataItemInventoryUI data, 
            int intialStackCount) 
        {
            _source = source;
            Data = data;
            _stackCount = intialStackCount;
            
            GridView.InitView(Data, this);
        }

        internal void SetToGrid(InventoryGrid grid, Vector2Int coords, byte orientation)
        {
            GridView.SetForGrid(grid, (InventoryItemGridOrientation)orientation);
            GridView.SetGridPosition(coords, true);
            //SetActiveView(_viewType);
        }

        private void OnEnable()
        {
            if (_updatePositionOnEnable)
            {
                GridView.SetGridPosition(GridPosition.Coords, true);
                _updatePositionOnEnable = false;
            }
        }

        #endregion INITIALIZATION


        #region TICK

        public void Tick(float delta)
        {
            GridView.Tick(delta);
            // switch (_viewType)
            // {
            //     case UIInventoryItemViewType.Grid: GridView.Tick(delta); break;
            //    // case UIInventoryItemViewType.Simple: _simpleView.Tick(delta); break;
            // }
        }

        #endregion TICK
        
        
        #region VIEWS

        public void SetHoveredPosition(Vector3 point, Camera ssCam, Vector3 offset)
        {
            GridView.SetHoveredPosition(point, ssCam, offset);
        }
        
        #endregion VIEWS


        #region POINTER STATE

        internal void SetPointerState(InventoryItemPointerState state)
        {
            if (PointerState == state)
                return;
            
            PointerState = state;
            switch (state)
            {
                case InventoryItemPointerState.None: DoSetPointerStateRelease(); break;
                case InventoryItemPointerState.Hovered: DoSetPointerStateHovered(); break;
                case InventoryItemPointerState.Grabbed: DoSetPointerStateGrab(); break;
            }
        }

        private void DoSetPointerStateGrab()
        {
            //_currentView.OnPointerGrab();
            GridView.OnPointerGrab();
        }
        
        private void DoSetPointerStateHovered()
        {
            //_currentView.OnPointerHover();
            GridView.OnPointerHover();
        }
        
        private void DoSetPointerStateRelease()
        {
            //_currentView.OnPointerRelease();
            GridView.OnPointerRelease();
        }

        #endregion POINTER STATE


        #region GRAB

        internal void OnItemGrabbed(InventoryGrid grid)
        {
            if (_gridGhost)
            {
                Destroy(_gridGhost.gameObject);
                _gridGhost = null;
            }
            
            _gridGhost = Instantiate(_gridViewPrefab.gameObject, grid.ItemParent).GetComponent<InventoryItemViewGrid>();
            _gridGhost.InitView(Data, this, true);
            _gridGhost.SetForGrid(grid, GridView.Orientation);
            _gridGhost.SetGridPosition(GridView.Coords, true);
        }

        public void OnItemDropped()
        {
            if (_gridGhost)
            {
                Destroy(_gridGhost.gameObject);
                _gridGhost = null;
            }
        }

        internal virtual void DestroyItem()
        {
            if (_gridGhost)
            {
                Destroy(_gridGhost.gameObject);
            }
            Destroy(gameObject);
        }

        internal void FlagItemForSnapReset()
        {
            _updatePositionOnEnable = true;
        }

        #endregion GRAB


        #region TRANSFER

        public virtual void OnItemTransferredToNewSource(IItemReceivable newSource)
        {
            _source = newSource;
        }
        
        /// <summary>
        /// When we reset an item into a container that is now closed, we need to visually transition the item before re-parenting it
        /// </summary>
        internal void OnItemResetIntoClosedContainerGrid(InventoryGridContainer container)
        {
            Debug.Log("Resetting into closed container");
            _updatePositionOnEnable = true;
        }

        #endregion TRANSFER


        #region STACK

        internal bool IsSplittable()
        {
            return Data.StackSize > 1 && _stackCount > 1;
        }

        /// <summary>
        /// Used to add items to this item's stack. We return -1 if nothing could be added; 0 if everything was added successfully;
        /// Any other num represents the leftover after maxing the stack
        /// </summary>
        int IInventoryOccupant.TryAddToStack(int numToAdd)
        {
            // If we cannot add anymore
            if (_stackCount >= Data.StackSize) 
                return numToAdd;

            int neededForFull = Data.StackSize - _stackCount;
            if (numToAdd <= neededForFull)
            {
                _stackCount += numToAdd;
                GridView.UpdateItemStackCount(_stackCount);
                
                return 0;
            }

            _stackCount = Data.StackSize;
            GridView.UpdateItemStackCount(_stackCount);
            
            return numToAdd - neededForFull;
        }

        internal void RemoveFromStack(int toRemove)
        {
            _stackCount -= toRemove;
            GridView.UpdateItemStackCount(_stackCount);
        }

        internal void SetStackCountExplicit(int count)
        {
            _stackCount = count;
            GridView.UpdateItemStackCount(_stackCount);
        }

        #endregion STACK
        
        
        #region SELECT

        internal void SelectItem()
        {
            
        }

        #endregion SELECT


        #region EQUIP

        internal void SetItemEquipped(bool b)
        {
            GridView.SetItemIsEquipped(b);
        }

        #endregion EQUIP


        #region UTILITY

        internal bool CanBeRotated()
        {
            return Data.CanRotate;
        }

        #endregion UTILITY
    }
}