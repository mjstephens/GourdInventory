using UnityEngine;
using UnityEngine.EventSystems;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Defines a single one-off inventory slot, such as those used in the character equip component. Note it is up to other
    /// components to coordinate interaction as this class does not directly listen to inventory input
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InventorySingleSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region VARIABLES

        [Header("Meta")]
        [SerializeField] public ItemEquipSlotType Slot;
        
        public UIInventoryItem OccupyingItem { get; protected set; }
        public RectTransform Rect { get; private set; }

        #endregion VARIABLES


        #region INITIALIZATION

        private void Awake()
        {
            Rect = GetComponent<RectTransform>();
        }
        
        #endregion INITIALIZATION


        #region ITEM

        public virtual void SetItem(UIInventoryItem item)
        {
            OccupyingItem = item;
        }

        #endregion ITEM


        #region POINTER EVENTS

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            
        }

        #endregion POINTER EVENTS
    }
}