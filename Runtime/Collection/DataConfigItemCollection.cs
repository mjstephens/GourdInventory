using UnityEngine;

namespace GalaxyGourd.Inventory
{
    public abstract class DataConfigItemCollection : ScriptableObject
    {
        #region DATA

        public abstract DataItemCollection GetCollection(
            DataConfigUIInventoryGrid target, 
            int baseSubgrid = 0,
            GridItemsArrangementType arrangementType = GridItemsArrangementType.Scattered);
        
        #endregion DATA
    }
}