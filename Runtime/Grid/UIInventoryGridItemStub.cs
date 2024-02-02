using System.Collections.Generic;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Used to add items to grids that are not open; since this class contains all the data needed to calculate grid placement, we don't
    /// need to actually instantiate a grid item
    /// </summary>
    public class UIInventoryGridItemStub : IInventoryOccupant
    {
        #region VARIABLES

        public string InstanceID { get; set; }
        public virtual DataItemInventoryUI Data { get; }
        public int ChildSubgrid { get; set; }
        public List<int> OccupyingIndices { get; }
        public DataItemGridPosition GridPosition { get; private set; }
        public List<IInventoryOccupant> SubOccupants { get; set; }

        #endregion VARIABLES


        #region INITIALIZATION

        public UIInventoryGridItemStub(
            DataItemInventoryUI config, 
            DataItemGridPosition dataPosition, 
            int gridWidth)
        {
            Data = config;
            GridPosition = dataPosition;
            
            OccupyingIndices = (this as IInventoryOccupant).GetOccupyingIndices(gridWidth);
        }

        #endregion INITIALIZATION


        #region METHODS

        int IInventoryOccupant.TryAddToStack(int numToAdd)
        {
            // If we cannot add anymore
            int count = GridPosition.StackCount;
            if (count >= Data.StackSize) 
                return numToAdd;

            int neededForFull = Data.StackSize - count;
            if (numToAdd <= neededForFull)
            {
                GridPosition = new DataItemGridPosition()
                {
                    Coords = GridPosition.Coords,
                    Orientation = GridPosition.Orientation,
                    StackCount = GridPosition.StackCount + numToAdd
                };
                
                return 0;
            }
            
            GridPosition = new DataItemGridPosition()
            {
                Coords = GridPosition.Coords,
                Orientation = GridPosition.Orientation,
                StackCount = Data.StackSize
            };
            
            return numToAdd - neededForFull;
        }

        #endregion METHODS
    }
}