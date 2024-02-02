using UnityEngine;

namespace GalaxyGourd.Inventory
{
    public interface IItemReceivable
    {
        #region PROPERTIES

        RectTransform Rect { get; }

        #endregion PROPERTIES


        #region METHODS

        /// <summary>
        /// When a moving item is dragged onto this receivable, it should assume control
        /// </summary>
        void TransferDraggingTo(DataDraggingItem item);

        /// <summary>
        /// When a moving item is dragged off of this receivable
        /// </summary>
        void TransferDraggingFrom(DataDraggingItem item);

        void ResetItemToSource(DataDraggingItem item, bool snap = false);

        void OnItemTransferredOut(DataDraggingItem item);

        void OnItemResetToWorld();

        bool PointerIsInsideContentRect();

        void RemoveItemExplicit(UIInventoryItem item);

        // CONTAINER
        void OpenContainerItem(UIInventoryItemContainer container);
        void CloseContainerItem();

        #endregion METHODS
    }
}