using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GalaxyGourd.Inventory
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class InventoryItemView : MonoBehaviour
    {
        #region VARIABLES

        [Header("References")]
        [SerializeField] protected Image _icon;
        [SerializeField] protected Image _itemBackground;
        [SerializeField] protected RectTransform _itemBackgroundRect;
        //[SerializeField] private TMP_Text _textStackCount;
        
        [Header("Colors")]
        [SerializeField] protected Color _backgroundColorDefault;
        [SerializeField] protected Color _backgroundColorHovered;
        [SerializeField] protected Color _backgroundColorGrabbed;
        [SerializeField] protected Color _iconColorGhost;
        [SerializeField] protected Color _backgroundColorGhost;
        [SerializeField] protected Color _backgroundColorEquipped;
        
        public RectTransform RectTrans => _transform;
        public InventorySingleSlot Slot { get; private set; }

        protected UIInventoryItem _item;
        protected RectTransform _transform;
        protected bool _isUpdatingPosition;         // Used to tick smooth position

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        internal virtual void InitView(DataItemInventoryUI data, UIInventoryItem item, bool isGhost = false)
        {
            _transform = GetComponent<RectTransform>();
            _item = item;
            _icon.sprite = data.Icon;
            
           // _textStackCount.text = item.StackCount.ToString();
           // _textStackCount.enabled = data.StackSize > 1;

            if (isGhost)
            {
                InitAsGhost();
            }
        }
        
        private void InitAsGhost()
        {
            _icon.color = _iconColorGhost;
            _icon.color = _iconColorGhost;
            _itemBackground.color = _backgroundColorGhost;
            //_textStackCount.text = "";
        }
        
        public void SetForSlot(InventorySingleSlot slot)
        {
            Slot = slot;
            _itemBackground.color = _backgroundColorEquipped;
        }

        #endregion INITIALIZATION


        #region UPDATE

        internal virtual void UpdateItemStackCount(int count)
        {
            //_textStackCount.text = count.ToString();
        }
        
        #endregion UPDATE


        #region TICK

        internal virtual void Tick(float delta)
        {
            if (_isUpdatingPosition)
            {
                TickSmoothPosition(delta);
            }
        }

        internal IEnumerator CR_BumpItemView()
        {
            float originalScale = _icon.transform.localScale.x;
            _icon.transform.localScale *= 1.25f;
            while (Mathf.Abs(_icon.transform.localScale.x - originalScale) > 0.1f)
            {
                _icon.transform.localScale = Vector3.Lerp(
                    _icon.transform.localScale, 
                    Vector3.one * originalScale, 
                    Time.deltaTime * 10);
                yield return null;
            }

            _icon.transform.localScale = Vector3.one * originalScale;
        }
        
        #endregion TICK


        #region POSITION

        internal void SetHoveredPosition(Vector3 point, Camera ssCam, Vector3 offset)
        {
            // We need to translate screen space offset to local space offset
            Vector3 screenSpacePos = ssCam == null ? point : ssCam.WorldToScreenPoint(point);
            screenSpacePos -= offset;
            _transform.position = ssCam == null ? screenSpacePos : ssCam.ScreenToWorldPoint(screenSpacePos);
        }

        public void SetPositionDirectly(Vector3 position)
        {
            _transform.position = position;
            _itemBackgroundRect.anchoredPosition = Vector2.zero;
        }

        private void TickSmoothPosition(float delta)
        {
            if (_itemBackgroundRect.anchoredPosition.sqrMagnitude > 0.1f)
            {
                _itemBackgroundRect.anchoredPosition = Vector2.Lerp(_itemBackgroundRect.anchoredPosition, Vector2.zero, delta * 30);
            }
            else
            {
                _itemBackgroundRect.anchoredPosition = Vector2.zero;
                _isUpdatingPosition = false;
            }
        }

        #endregion POSITION


        #region POINTER STATE

        internal virtual void OnPointerRelease()
        {
            
        }

        internal virtual void OnPointerHover()
        {
            
        }

        internal virtual void OnPointerGrab()
        {
            
        }

        #endregion POINTER STATE


        #region EQUIP

        internal void SetItemIsEquipped(bool b)
        {
            _itemBackground.color = b ? _backgroundColorEquipped : _backgroundColorDefault;
        }

        #endregion EQUIP
    }
}