using System.Collections.Generic;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Creates item collections from various inputs
    /// </summary>
    public static class ItemCollectionCompiler
    {
        #region METHODS
        
        /// <summary>
        /// Compiles an item collection from a list of inventory item configs, allowing us to populate a collection for use
        /// in an inventory grid
        /// </summary>
        public static DataItemCollection CompileCollection(
            List<(DataItemInventoryUI, int)> itemsForDistribution,
            List<DataGridOccupant> currentOccupants,
            DataConfigUIInventoryGrid target,
            int baseSubgrid = 0, // The "child subgrid" property
            GridItemsArrangementType arrangementType = GridItemsArrangementType.Scattered)
        {
            // Distribute items
            ItemGridAutoAddResults results = target.GetAutoAddResults(itemsForDistribution);
            if (results == ItemGridAutoAddResults.FullyFits)
            {
                IEnumerable<IInventoryOccupant> arrangedItems = 
                    target.GetGridItemDistribution(itemsForDistribution, arrangementType);
                
                foreach (IInventoryOccupant item in arrangedItems)
                {
                    // Create and add this new occupant
                    currentOccupants.Add(new DataGridOccupant()
                    {
                        ItemConfigID = item.Data.ConfigID,
                        ItemInstanceID = item.InstanceID,
                        GridPosition = item.GridPosition,
                        ParentSubgrid = baseSubgrid,
                        ChildSubgrid = item.ChildSubgrid,
                    });
                }
            }
            
            DataItemCollection data = new DataItemCollection()
            {
                Occupants = currentOccupants.ToArray()
            };

            return data;
        }

        /// <summary>
        /// Compiles an item collection from a list of inventory occupants, allowing us to serialize grid items from an inventory
        /// </summary>
        public static DataItemCollection CompileCollection(
            List<IInventoryOccupant> occupants, 
            int baseIndex = 0)
        {
            List<DataGridOccupant> data = new();
            foreach (IInventoryOccupant item in occupants)
            {
                // If this item is a container with items inside
                if (item.SubOccupants is { Count: > 0 })
                {
                    // Save container item
                    data.Add(new DataGridOccupant()
                    {
                        ItemConfigID = item.Data.ConfigID,
                        ItemInstanceID = item.InstanceID,
                        GridPosition = item.GridPosition,
                        ParentSubgrid = baseIndex,
                        ChildSubgrid = baseIndex + 1,
                    });
                    
                    // Get a new collection for the container
                    DataItemCollection subCollection = CompileCollection(item.SubOccupants, baseIndex + 1);
                    
                    // Save all those items into our data
                    data.AddRange(subCollection.Occupants);
                }
                else
                {
                    data.Add(new DataGridOccupant()
                    {
                        ItemConfigID = item.Data.ConfigID,
                        ItemInstanceID = item.InstanceID,
                        GridPosition = item.GridPosition,
                        ParentSubgrid = baseIndex,
                        ChildSubgrid = 0,
                    });
                }
            }

            return new DataItemCollection {Occupants = data.ToArray()};
        }

        #endregion METHODS
    }
}