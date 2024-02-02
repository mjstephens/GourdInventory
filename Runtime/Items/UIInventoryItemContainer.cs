using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GalaxyGourd.Inventory
{
    /// <summary>
    /// Item class override for items that represent containers (ie bags)
    /// </summary>
    public class UIInventoryItemContainer : UIInventoryItem
    {
        #region VARIABLES

        internal DataItemContainerInventoryUI ContainerConfig => Data as DataItemContainerInventoryUI;
        internal InventoryGridContainer ContainerGrid { get; private set; }

        private IItemReceivable _lastOpenedSource;

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        /// <summary>
        /// We pre-spawn our container's grid for later activation
        /// </summary>
        internal void SpawnContainerGrid(Transform parent)
        {
            // Spawn the container grid
            if (!ContainerGrid)
            {
                ContainerGrid = Instantiate(ContainerConfig.GridObjPrefab, parent).GetComponent<InventoryGridContainer>();
                ContainerGrid.SetRootForContainer(_source);
                ContainerGrid.SetSourceForContainer(_source);
                ContainerGrid.SetItemForContainer(this);
                ContainerGrid.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (!ContainerGrid) 
                return;
            
            Destroy(ContainerGrid);
            ContainerGrid = null;
        }
        
        private void OnDisable()
        {
            if (!ContainerGrid.gameObject.activeInHierarchy)
                return;
            
            // We want to auto-close any subcontainers that are still open
            ContainerGrid.SetInputEnabled(false);
            ContainerGrid.SetClosed();
            
            if (ContainerGrid.ContainerSource is InventoryGrid source)
            {
                source.OnForceContainersClosed();
            }
            
            if (ContainerGrid.ContainerRoot is InventoryGrid root)
            {
                root.SetInputEnabled(true);
            }
        }

        internal void StageContainerGridItems(List<IInventoryOccupant> subitems)
        {
            // This is null when picking up an empty container item prop
            if (subitems == null)
                return;
            
            foreach (IInventoryOccupant item in subitems)
            {
                ContainerGrid.AddStagedItem(item);
            }

            // We want to copy the suboccupants for this container item
            SubOccupants = subitems;
        }

        #endregion INITIALIZATION


        #region CONTAINER

        public override void OnItemTransferredToNewSource(IItemReceivable newSource)
        {
            base.OnItemTransferredToNewSource(newSource);
            
            // We need to set the new source for our container grid
            ContainerGrid.SetRootForContainer(_source);
            ContainerGrid.SetSourceForContainer(_source);
        }

        internal void SetContainerRootDirectly(IItemReceivable root)
        {
            ContainerGrid.SetRootForContainer(root);
        }

        internal IEnumerator OnContainerOpen(IItemReceivable source, float cellSize)
        {
            if (source is InventoryGrid grid)
            {
                ContainerGrid.transform.localScale = Vector3.one;
                ContainerGrid.transform.SetParent(grid.transform);
                ContainerGrid.SetGridSizeManually(cellSize);
                ContainerGrid.transform.position = GridView.transform.position;
                (ContainerGrid.ContainerRoot as InventoryGrid)?.
                    FitRectInsideGridContent(ContainerGrid.GetComponent<RectTransform>());
                ContainerGrid.gameObject.SetActive(true);

                // We want to spawn any staged items we have in this grid
                StartCoroutine(ContainerGrid.InstantiateStagedItems(null));

                // Open the container view
                StartCoroutine(ContainerGrid.CR_OpenContainerView(Data.DisplayName));
                
                // If we are opening after moving the container, we need to resize the items inside
                if (source != _lastOpenedSource)
                {
                    StartCoroutine(ContainerGrid.ResizeItemsForNewContainerSource());
                }
            }
            _lastOpenedSource = source;
            
            // We wait a frame to prevent the container grid from picking up the input used to open the container itself
            yield return null;
            
            ContainerGrid.SetInputEnabled(true);
        }
        
        /// <summary>
        /// When we auto-place an item into this container
        /// </summary>
        internal void OnAutoDropItemIntoContainer(DataDraggingItem item)
        {
            // Even though we're only dropping one item, we need to create this data structure to auto-add it correctly
            List<Tuple<DataItemInventoryUI, int>> items = new()
            {
                new Tuple<DataItemInventoryUI, int>(
                    item.Item.Data, 
                    item.GrabbedStackCount)
            };
            
            // Add the items
            List<IInventoryOccupant> added = ContainerGrid.AutoAddItems(items, false);

            // If we added a container, we want to make sure we add back in the items that were in the container originally
            if (item.Item is UIInventoryItemContainer original && added[0] is UIInventoryItemContainer newContainer)
            {
                newContainer.StageContainerGridItems(GetSubcontainerRecursiveItems(original));
            }
            
            // Animate this bag to show it's receive the item
            StartCoroutine(GridView.CR_BumpItemView());

        }

        internal void OnContainerClose()
        {
            ContainerGrid.SetInputEnabled(false);
            
            // Close the container view
            StartCoroutine(ContainerGrid.CR_CloseContainerView());
        }

        /// <summary>
        /// When we reset an item into this container, and this container is closed
        /// </summary>
        internal void OnItemResetIntoThisClosedContainer(UIInventoryItem item)
        {
            Debug.Log("Returning to original container (now closed!) " + transform.name);
            
            // We need to create a ghost object to visualize this item moving back into the closed container
            
            
            
            IItemReceivable runningSource = ContainerGrid.ContainerSource;
            InventoryItemView runningView = ContainerGrid.ItemSource.GridView;
            while (!runningView.gameObject.activeInHierarchy)
            {
                if (runningSource is InventoryGridContainer container)
                {
                    runningSource = container.ContainerSource;
                    runningView = container.ItemSource.GridView;
                }
                else
                {
                    break;
                }
            }
            
            // Find the next visible container object in this chain
            StartCoroutine(runningView.CR_BumpItemView());
        }

        #endregion CONTAINER


        #region UTILITY

        internal override void DestroyItem()
        {
            // Clear all subitems in this container
            for (int i = ContainerGrid.Items.Count - 1; i >= 0; i--)
            {
                if (ContainerGrid.Items[i] is UIInventoryItem it)
                {
                    it.DestroyItem();
                }
            }
            
            base.DestroyItem();
        }
        
        /// <summary>
        /// When moving or instantiating a copy of a container that has many nested items/subcontainers, we need to reconstruct the
        /// subitems map by iterating through each subcontainer and copying the items inside
        /// </summary>
        private static List<IInventoryOccupant> GetSubcontainerRecursiveItems(UIInventoryItemContainer sourceContainer)
        {
            List<IInventoryOccupant> finalItems = new();
            
            // For each of the original subitems...
            foreach (IInventoryOccupant subItem in sourceContainer.ContainerGrid.Items)
            {
                // Was this subitem itself a nested container?
                if (subItem is UIInventoryItemContainer subContainer)
                {
                    IInventoryOccupant thisNestedContainer = subContainer;
                    thisNestedContainer.SubOccupants = GetSubcontainerRecursiveItems(subContainer);
                    finalItems.Add(thisNestedContainer);
                }
                else
                {
                    finalItems.Add(subItem);
                }
            }

            return finalItems;
        }

        #endregion UTILITY
    }
}