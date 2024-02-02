using GalaxyGourd.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace GalaxyGourd.Inventory
{
    public class InventoryGridCell : UIScalableGridCell
    {
        #region VARIABLES

        [Header("References")]
        [SerializeField] private Image _image;
        
        [Header("Colors")]
        [SerializeField] private Color _itemHoverCellColor;

        private Color _defaultColor;
        private ItemMovingState _itemMovingState;
        
        #endregion VARIABLES

        /// <summary>
        /// The state of this cell relative to the currently moving item
        /// </summary>
        internal enum ItemMovingState
        {
            None,
            Origin,
            Hover
        }
        
        #region INITIALIZATION

        public override void SetupCell(int x, int y, int index)
        {
            base.SetupCell(x, y, index);
            
            _defaultColor = _image.color;
        }

        #endregion INITIALIZATION


        #region VIEW

        internal void SetItemState(ItemMovingState state)
        {
            if (_itemMovingState == state || _itemMovingState == ItemMovingState.Origin)
                return;

            _itemMovingState = state;
            switch (_itemMovingState)
            {
                case ItemMovingState.None: _image.color = _defaultColor; break;
                case ItemMovingState.Hover: _image.color = _itemHoverCellColor; break;
            }
        }

        #endregion VIEW
    }
}