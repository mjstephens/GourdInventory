using UnityEngine;

namespace GalaxyGourd.Inventory
{
    public interface IOperatorInventoryInputManager
    {
        #region PROPERTIES

        InventoryDragSource DragSource { get; }
        RectTransform ItemGrabParent { get; }

        #endregion PROPERTIES


        #region METHODS

        //void Init(OperatorPlayer op, Transform uiCanvas);
        void RegisterUIInventoryInputReceiver(IUIInventoryInputReceiver receiver);
        void UnregisterUIInventoryInputReceiver(IUIInventoryInputReceiver receiver);
        void SetInventoryInputEnabled(bool b);

        #endregion METHODS
    }
}