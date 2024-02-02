using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// View for inventory item (simplified)
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InventoryItemViewSimple : InventoryItemView
    {
        #region VARIABLES

        
        #endregion VARIABLES


        #region POSITION

        private void SetItemDimensionsNormal()
        {
            _transform.sizeDelta = Slot.Rect.sizeDelta;
            _transform.position = Slot.Rect.position;
        }

        #endregion POSITION
        
        
        #region TICK

        internal override void Tick(float delta)
        {
            base.Tick(delta);
        }
        
        #endregion TICK
    } 
}