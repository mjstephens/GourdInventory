using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Class designed to contain input-related things for inventory grids; keeps actual inventory class less complex
    /// </summary>
    public class ActionMapListenerUIInventory : MonoBehaviour
    {
        #region VARIABLES

        [Header("Input")]
        [SerializeField] private InputActionMap _inputMap;
        
        // protected override string MapName => "UIInventory";
        // protected override bool EnabledByDefault => false;
        // protected override DataInputValuesUIInventory Data => _data;
        
        private readonly List<IUIInventoryInputReceiver> _receivers = new ();
        private DataInputValuesUIInventory _data;

        #endregion VARIABLES


        #region INITIALIZATION

        private void OnEnable()
        {
            _inputMap.Enable();
            _inputMap.actionTriggered += OnActionTriggered;
        }

        private void OnDisable()
        {
            _inputMap.actionTriggered -= OnActionTriggered;
            _inputMap.Disable();
        }

        #endregion INITIALIZATION


        #region REGISTRATION

        public void RegisterReceiver(IUIInventoryInputReceiver receiver)
        {
            _receivers.Add(receiver);
        }
        
        public void UnregisterReceiver(IUIInventoryInputReceiver receiver)
        {
            _receivers.Remove(receiver);
        }

        #endregion REGISTRATION
        
 
        #region INVENTORY INPUT

        private void Update()
        {
            foreach (IUIInventoryInputReceiver receiver in _receivers)
            {
                if (!receiver.CanReceiveInput)
                    continue;

                receiver.MobileHoldFlag = _data.MobileHoldFlag;
                if (_data.GrabOrReleaseItem) receiver.OnInputGrabOrDropItem();
                if (_data.RotateItemRight) receiver.OnInputRotateItemRight();
                if (_data.RotateItemLeft) receiver.OnInputRotateItemLeft();
                if (_data.GrabItemSplitStack) receiver.OnInputGrabItemSplitStack();
                if (_data.CancelItemGrab) receiver.OnInputGrabItemCancel();
            }
        }

        private void LateUpdate()
        {
            ResetData();
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            switch (context.action.name)
            {
                case "GridRotateRight": OnGridRotateItemRight(context); break;
                case "GridRotateLeft": OnGridRotateItemLeft(context); break;
                case "GrabItem": OnGridGrabItem(context); break;
                case "GrabItemSplitStack": OnGridGrabItemSplitStack(context); break;
                case "CycleGrabbedStackCount": OnCycleGrabbedStackCount(context); break;
                case "CancelItemGrab": OnGridCancelItemGrab(context); break;
            }
        }

        private void OnGridRotateItemRight(InputAction.CallbackContext action)
        {
            _data.RotateItemRight = action.performed;
        }
        
        private void OnGridRotateItemLeft(InputAction.CallbackContext action)
        {
            _data.RotateItemLeft = action.performed;
        }
        
        private void OnGridGrabItem(InputAction.CallbackContext action)
        {
            Debug.Log("Grab0");

            _data.GrabOrReleaseItem = action.performed;
        }
        
        private void OnGridGrabItemSplitStack(InputAction.CallbackContext action)
        {
            _data.GrabItemSplitStack = action.performed;
        }
        
        private void OnCycleGrabbedStackCount(InputAction.CallbackContext action)
        {
            _data.CycleGrabbedStackCount = action.performed;
        }

        private void OnGridCancelItemGrab(InputAction.CallbackContext action)
        {
            _data.CancelItemGrab = action.performed;
        }

        private void ResetData()
        {
            _data.RotateItemRight = false;
            _data.RotateItemLeft = false;
            _data.GrabOrReleaseItem = false;
            _data.GrabItemSplitStack = false;
            _data.CycleGrabbedStackCount = false;
            _data.CancelItemGrab = false;
        }

        #endregion INVENTORY INPUT
    }
}