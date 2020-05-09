using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Common;
using Sandbox.ModAPI;
using Sandbox.Definitions;
using VRage.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ConquestGame
{
    using ResourceList = List<VRage.Game.ModAPI.Ingame.MyInventoryItem>;

    class ConquestGameModeTeamsResourceNodeSpawning
    {
        private IMyCubeBlock CargoBlock;
        private int ResourceNodeId;
        private ResourceList ResourceListing = new ResourceList();

        public ConquestGameModeTeamsResourceNodeSpawning(int resourceNodeId, IMyCubeBlock block) {
            if (block == null) {
                throw new System.Exception("Cargo Node Grid is null");
            }

            CargoBlock = block;
            ResourceNodeId = resourceNodeId;

            snapshotInventoryItems();
        }

        private void snapshotInventoryItems() {

            ResourceListing.Clear();

            var cargoBlock = (VRage.Game.ModAPI.Ingame.IMyInventoryOwner) CargoBlock;
            var inventory = cargoBlock.GetInventory(0);
            inventory.GetItems(ResourceListing);
            foreach(var defaultItem in ResourceListing) {
                (inventory as VRage.Game.ModAPI.IMyInventory).RemoveItems(defaultItem.ItemId, defaultItem.Amount);
            }
        }

        public void AddMoreResources() {

            var cargoBlock = (VRage.Game.ModAPI.Ingame.IMyInventoryOwner) CargoBlock;
            var inventory = cargoBlock.GetInventory(0);
            var currentList = new ResourceList();
            inventory.GetItems(currentList);
            foreach(var defaultItem in ResourceListing) {
                bool found = false;
                foreach(var item in currentList) {
                    if (defaultItem.Type == item.Type) {
                        if (defaultItem.Amount > item.Amount) {

                            // If item already exists with less than default just top up
                            Sandbox.Game.MyVisualScriptLogicProvider.AddToInventory(CargoBlock.Name, defaultItem.Type, item.Amount.ToIntSafe()-defaultItem.Amount.GetHashCode());
                            Debug.d("Item: "+item.Type.ToString()+"\n ItemId: "+item.ItemId.ToString()+"\n Amount: "+item.Amount.ToIntSafe().ToString()+"\n");
                            found = true;
                        }
                    }
                }

                if (!found) {
                    Debug.d("Item: "+defaultItem.Type.ToString()+"\n ItemId: "+defaultItem.ItemId.ToString()+"\n Amount: "+defaultItem.Amount.ToIntSafe().ToString()+"\n");
                    Sandbox.Game.MyVisualScriptLogicProvider.AddToInventory(CargoBlock.Name, defaultItem.Type, defaultItem.Amount.ToIntSafe());
                }
            }


            // BulletproofGlass
            // Computer
            // Construction
            // Detector
            // Display
            // Explosives
            // Girder
            // GravityGenerator
            // InteriorPlate
            // LargeTube
            // Medical
            // MetalGrid
            // Motor
            // PowerCell
            // RadioCommunication
            // Reactor
            // SmallTube
            // SolarCell
            // SteelPlate
            // Thrust
            // var itemDefinition = Sandbox.Game.MyVisualScriptLogicProvider.GetDefinitionId("Component", "Computer");
            // Sandbox.Game.MyVisualScriptLogicProvider.AddToInventory(CargoBlock.Name, itemDefinition, 1);
            // Sandbox.Game.MyVisualScriptLogicProvider.AddToPlayersInventory(0, itemDefinition, 1);
        }
    }
}