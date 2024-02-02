namespace GalaxyGourd.Inventory
{
    public interface IUIInventoryInputReceiver
    {
        #region PROPERTIES

        bool CanReceiveInput { get; }
        bool MobileHoldFlag { get; set; }

        #endregion PROPERTIES


        #region METHODS

        void OnInputAutoEquip(BoolAction input);
        void OnInputRotateItemRight();
        void OnInputRotateItemLeft();
        void OnInputGrabOrDropItem();
        void OnInputGrabItemSplitStack();
        void OnInputGrabItemCancel();
        
        #endregion METHODS
    }
}