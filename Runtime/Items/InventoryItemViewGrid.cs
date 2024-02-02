using System.Collections.Generic;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// View for inventory item on a complex grid
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InventoryItemViewGrid : InventoryItemView
    {
        #region VARIABLES 

        [Header("References")]
        [SerializeField] private RectTransform _origin;

        internal Vector2Int Coords { get; private set; }
        internal List<int> OccupyingIndices { get; private set; } 
        internal InventoryItemGridOrientation Orientation { get; private set; }
        internal Vector3 Origin => _origin.position;

        private InventoryGrid _inventory;
        private float _width;
        private float _height;
        private float _defaultWidth;
        private float _defaultHeight;
        private Vector2 _gridPosition;
        private Vector2 _posOffsetFromOriginCell;
        private float _iconRotationTarget;  // Used to aid in icon rotation
        private Quaternion _targetRotation; // Used to aid in icon rotation
        private bool _isUpdatingRotation;   // Used to tick smooth rotation

        #endregion VARIABLES


        #region INITIALIZATION

        internal override void InitView(DataItemInventoryUI data, UIInventoryItem item, bool isGhost = false)
        {
            base.InitView(data, item, isGhost);
            
            OccupyingIndices = new List<int>();
            _itemBackground.color = _backgroundColorDefault;
        }

        internal void SetForGrid(
            InventoryGrid inventory, 
            InventoryItemGridOrientation orientation = InventoryItemGridOrientation.Up)
        {
            Coords = new Vector2Int(-1, -1);
            _inventory = inventory;
            
            // We need to set the size first as default in order for the _icon rect to be correctly sized
            SetItemDimensions(false);
            _itemBackgroundRect.sizeDelta = _transform.sizeDelta;
            _defaultWidth = _width;
            _defaultHeight = _height;

            // Now we adjust for actual rotation value
            Orientation = orientation;
            SetSizeFromOrientation(true);
        }

        #endregion INITIALIZATION


        #region TICK

        internal override void Tick(float delta)
        {
            if (_isUpdatingRotation)
            {
                TickSmoothRotation(delta);
            }
            
            base.Tick(delta);
        }

        private void TickSmoothRotation(float delta)
        {
            if (Quaternion.Angle(_itemBackground.transform.localRotation, _targetRotation) > 0.5f)
            {
                _itemBackground.transform.localRotation = Quaternion.Slerp(
                    _itemBackground.transform.localRotation,
                    _targetRotation, delta * 30);
            }
            else
            {
                _itemBackground.transform.localEulerAngles = new Vector3(0, 0, _iconRotationTarget);
                _isUpdatingRotation = false;
            }   
        }

        #endregion TICK


        #region POSITION

        /// <summary>
        /// Sets the position of the item in the inventory grid
        /// </summary>
        internal void SetGridPosition(Vector2Int upperLeftCoords, bool snap = false)
        {
            Transform cacheParent = transform.parent;
            transform.SetParent(_inventory.ItemParent);
            Coords = upperLeftCoords;
            _gridPosition = GetGridPosition(upperLeftCoords);
            
            // Set the item's occupying indices
            OccupyingIndices = (_item as IInventoryOccupant).GetOccupyingIndices(_inventory.Grid.GridWidth);

            // Optionally smoothly move the background
            if (!snap)
            {
                _isUpdatingPosition = true;
                _itemBackgroundRect.SetParent(_transform.parent);
            }
            else
            {
                _itemBackgroundRect.anchoredPosition = Vector2.zero;
            }
            
            // Set position of object
            _transform.anchoredPosition = _gridPosition;
            _transform.localPosition = new Vector3(_transform.localPosition.x, _transform.localPosition.y, 0);
            _itemBackgroundRect.SetParent(_transform);
            _itemBackgroundRect.SetAsFirstSibling();
            _itemBackgroundRect.localPosition = new Vector3(_itemBackgroundRect.localPosition.x, _itemBackgroundRect.localPosition.y, 0);
            transform.SetParent(cacheParent);
        }

        private Vector2 GetGridPosition(Vector2Int upperLeftCoords)
        {
            int cellIndex = _inventory.CellAtCoords(upperLeftCoords).Index;
            Vector2 originCellPosition = _inventory.CellViews[cellIndex].GetComponent<RectTransform>().anchoredPosition;
            Vector2 gridPosition = originCellPosition + _posOffsetFromOriginCell;

            return gridPosition;
        }

        #endregion POSITION


        #region ROTATION

        /// <summary>
        /// Rotates the item 90 degrees to the left or right
        /// </summary>
        internal void RotateItem(bool trueLeftFalseRight)
        {
            // Set new orientation
            int current = (int)Orientation;
            current += trueLeftFalseRight ? 1 : -1;
            if (current > 3) current = 0;
            else if (current < 0) current = 3; 
            Orientation = (InventoryItemGridOrientation)current;

            SetSizeFromOrientation();
        }

        public void ForceOrientation(InventoryItemGridOrientation orientation)
        {
            Orientation = orientation;
            SetSizeFromOrientation();
            _itemBackground.transform.localEulerAngles = new Vector3(0, 0, _iconRotationTarget);
        }

        /// <summary>
        /// When we rotate the item, we must resize the transform to fill the appropriate grid dimensions
        /// </summary>
        private void SetSizeFromOrientation(bool snapOrientation = false)
        {
            switch (Orientation)
            {
                case InventoryItemGridOrientation.Up: 
                    SetItemDimensions(false);
                    _targetRotation = Quaternion.Euler(0, 0, 0);
                    _iconRotationTarget = 0;
                    break;
                case InventoryItemGridOrientation.Right:
                    SetItemDimensions(true);
                    _targetRotation = Quaternion.Euler(0, 0, 90);
                    _iconRotationTarget = 90;
                    break;
                case InventoryItemGridOrientation.Down:
                    SetItemDimensions(false);
                   _targetRotation = Quaternion.Euler(0, 0, 180);
                    _iconRotationTarget = 180;
                    break;
                case InventoryItemGridOrientation.Left: 
                    SetItemDimensions(true);
                    _targetRotation = Quaternion.Euler(0, 0, 270);
                    _iconRotationTarget = 270;
                    break;
            }

            if (snapOrientation)
            {
                _itemBackground.transform.localEulerAngles = new Vector3(0, 0, _iconRotationTarget);
            }
            else
            {
                _isUpdatingRotation = true;
            }
        }

        private void SetItemDimensions(bool isRotated)
        {
            float xSize = _inventory.CellSize * 
                          _item.Data.Dimensions.x + (_item.Data.Dimensions.x * _inventory.CellSpacing) - 
                          _inventory.CellSpacing;
            float ySize = _inventory.CellSize * 
                          _item.Data.Dimensions.y + (_item.Data.Dimensions.y * _inventory.CellSpacing) - 
                          _inventory.CellSpacing;

            _width = isRotated ? ySize : xSize;
            _height = isRotated ? xSize : ySize;
            _transform.sizeDelta = new Vector2(_width, _height);
            SetOrigin();
        }

        /// <summary>
        /// Sets the position of the item grid origin. This should always be the upper-left area of the item, regardless of orientation
        /// </summary>
        private void SetOrigin()
        {
            // Adjust for width of cells + spacing
            float spacingAdjustment = _inventory.CellSize / 2f + (_inventory.CellSpacing / 2);
            float xAdjust = (_transform.sizeDelta.x / 2) - spacingAdjustment + (_inventory.CellSpacing / 2);
            float yAdjust = (_transform.sizeDelta.y / 2) - spacingAdjustment + (_inventory.CellSpacing / 2);
            _posOffsetFromOriginCell = new Vector2(xAdjust, -yAdjust);
            
            // Move origin marker
            _origin.anchoredPosition = new Vector2(spacingAdjustment, -spacingAdjustment);
        }

        #endregion POSITION


        #region POINTER STATE

        internal override void OnPointerRelease()
        {
            _itemBackgroundRect.sizeDelta = new Vector2(_defaultWidth, _defaultHeight);
            _itemBackground.color = _backgroundColorDefault;
        }

        internal override void OnPointerHover()
        {
            _itemBackgroundRect.sizeDelta = new Vector2(_defaultWidth, _defaultHeight) + 
                                            new Vector2(_inventory.CellSpacing, _inventory.CellSpacing);
            _itemBackground.color = _backgroundColorHovered;
        }

        internal override void OnPointerGrab()
        {
            _transform.SetAsLastSibling();
            _itemBackgroundRect.sizeDelta = new Vector2(_defaultWidth, _defaultHeight) + 
                                            new Vector2(_inventory.CellSpacing, _inventory.CellSpacing);
            _itemBackground.color = _backgroundColorGrabbed;
        }

        #endregion POINTER STATE
    } 
}