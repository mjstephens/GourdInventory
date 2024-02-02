using UnityEngine;

namespace GalaxyGourd.Inventory
{
    [CreateAssetMenu(
        fileName = "DAT_ItemCollectionSerialized", 
        menuName = "RPG/Serialized Data/ItemCollection")] 
    public class DataConfigSerializedItemCollection : DataConfigItemCollection
    {
        [SerializeField] public DataItemCollection Data;
        
        public override DataItemCollection GetCollection(
            DataConfigUIInventoryGrid target, 
            int baseSubgrid = 0,
            GridItemsArrangementType arrangementType = GridItemsArrangementType.Scattered)
        {
            // Data already has grid positioning information - just pass back the data as-is
            return Data;
        }
    }
}