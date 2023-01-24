using Isto.Atrio;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VoidChest
{
    public class VoidChest : ScrapChest, IActivatable, IActionOnPickup
    {
        internal static AdvancedItem Item { get; set; }
        internal static AdvancedItem ScrapChestItem { get; set; }

        protected override void Awake()
        {
            pileCount = 1;
            itemPile = new ItemPile(Item, 1);
            base.Awake();
        }

        public new void Activate()
        {
            if (!Glob.VoidChestSaveData.Contains(transform.position))
                Glob.VoidChestSaveData.Add(transform.position);

            gameObject.SetColliderState(true);
            if(_systemContainer == null)
            {
                SetupInAutomationSystem();
                if(_autoSystem.TryGetExistingGridSpace(transform.position, out AutomationGridSpace gridSpace))
                {
                    foreach (IAutomationGridSpaceDisplay display in GetComponents<IAutomationGridSpaceDisplay>())
                        display.SetGridSpace(gridSpace);
                }
            }
        }

        public new void DoPickupAction()
        {
            Glob.VoidChestSaveData.Remove(transform.position);
            base.DoPickupAction();
        }

        public override string GetItemName()
        {
            return "Void Chest";
        }

        [Zenject.Inject]
        public void Inject(IUIMainMenu mainMenu, IUIDisplayMessage uIDisplayMessage, AutomationSystem automationSystem, AutomationItemContainer.Factory containerFactory, AutomationSystemDisplay automationSystemDisplay)
        {
            this._mainMenu = mainMenu;
            this._messageDisplay = uIDisplayMessage;
            this._autoSystem = automationSystem;
            this._containerFactory = containerFactory;
            this._autoSystemDisplay = automationSystemDisplay;
        }

        protected override void HideOutline()
        {
            if (outlines != null)
                base.HideOutline();
        }

        protected override void ShowOutline()
        {
            if (outlines != null)
                base.ShowOutline();
        }

        protected override void SetupInAutomationSystem()
        {
            if (!gameObject.activeInHierarchy)
                return;

            _systemContainer = _containerFactory.Create(new AutomationItemContainerParams(pileCount, pileSize, itemPile.item.itemID, rigidInventory, pullsFromNeighbors));
            _container = _systemContainer.GetInventory();
            if (_autoSystem.TryAddItemProcessor(transform.position, _systemContainer, default(Vector3), false))
            {
                _autoSystemDisplay.AddObjectToAreaDictionary(gameObject);
                for (int i = 0; i < _startingItems.Count; i++)
                    _container.Add(_startingItems[i]);
                _container.InventoryChanged += OnInventoryChanged;
                UpdateStorageIndicator();
            }
        }

        public new void PlayerInteraction(PlayerController player, Controls.UserActions action)
        {
            if (action != Controls.UserActions.Dismantle)
            {
                if (action == Controls.UserActions.Interact)
                {
                    if (Vector3.Distance(player.transform.position, GetInteractionPosition(player.transform.position)) < player.InteractionRange)
                        _mainMenu.OpenStorageMenu(_container, itemPile.item.itemName, null);
                    else
                        player.SetMoveToAndInteractWithItem(this, 0f);
                }
            }
            else
                player.SetPickupItem(this);
        }

        protected new void OnInventoryChanged(object sender, ItemEventArgs e)
        {
            if(e.container.GetTotalNumberOfItems() > 0)
                e.container.RemoveAll(true);
        }

        protected override void OnDestroy()
        {
            if (_container != null)
                _container.InventoryChanged -= OnInventoryChanged;

            base.OnDestroy();
        }

        public override void SetGridSpace(AutomationGridSpace space)
        {
            _systemContainer = (space.itemProcessor as AutomationItemContainer);
            _container = _systemContainer.GetInventory();
            _container.InventoryChanged += OnInventoryChanged;
            Activate();
            UpdateStorageIndicator();
        }
    }
}
