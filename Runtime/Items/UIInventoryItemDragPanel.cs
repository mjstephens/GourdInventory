using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Simple class used by others to locate the rect for re-parenting dragging items
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIInventoryItemDragPanel : MonoBehaviour
    {
        #region VARIABLES

        public RectTransform Rect { get; private set; }

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        private void Awake()
        {
            Rect = GetComponent<RectTransform>();
        }

        #endregion INITIALIZATION


        #region ADJUST

        public void BumpToHierarchyBottom()
        {
            Rect.SetAsLastSibling();
        }

        #endregion ADJUST
    }
}