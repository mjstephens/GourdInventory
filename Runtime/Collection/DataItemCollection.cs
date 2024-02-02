using System;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Contains all of the items within a given inventory instance.
    /// </summary>
    [Serializable]
    public struct DataItemCollection
    {
        public DataGridOccupant[] Occupants;
    }
}