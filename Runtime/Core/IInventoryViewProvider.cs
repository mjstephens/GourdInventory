namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Serves as bridge interface between the inventory classes and the player/ui classes that need to interact with them
    /// </summary>
    public interface IInventoryViewProvider
    {
        DataItemInventoryUI GetItemConfig(string configID);
        void OnInventoryOpened();
    }
}