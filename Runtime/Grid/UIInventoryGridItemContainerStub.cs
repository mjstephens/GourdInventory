using System.Collections.Generic;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    public class UIInventoryGridItemContainerStub : UIInventoryGridItemStub
    {
        #region VARIABLES

        /// <summary>
        /// The index of the grid that this container opens
        /// </summary>
        internal int SubgridIndex { get; }
        
        #endregion VARIABLES
        
        
        #region CONSTRUCTION

        public UIInventoryGridItemContainerStub(
            DataItemContainerInventoryUI config, 
            DataItemGridPosition dataPosition,
            int gridWidth,
            int subgridIndex) 
            : base(config, dataPosition, gridWidth)
        {
            SubgridIndex = subgridIndex;
        }
        
        #endregion CONSTRUCTION


        #region SUBITEMS

        internal void AddSubItem(IInventoryOccupant item)
        {
            if (SubOccupants == null)
            {
                SubOccupants = new List<IInventoryOccupant>();
            }

            SubOccupants.Add(item);
        }
        
        internal void AddSubItems(List<IInventoryOccupant> items)
        {
            if (SubOccupants == null)
            {
                SubOccupants = new List<IInventoryOccupant>();
            }
            
            SubOccupants.AddRange(items);
        }

        #endregion SUBITEMS
    }
}