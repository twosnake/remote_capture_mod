using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sandbox.Game.Entities;
using Sandbox.Common;
using Sandbox.ModAPI;
using Sandbox.Definitions;
using VRage.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ConquestGame
{
    using BlocksDict = Dictionary<long, IMyCubeBlock>;

    class ConquestGameModeTeamsResourceNode
    {
        //private IMyFaction Faction;
        private MyCubeGrid Grid;
        private BlocksDict CargoBlocks;
        public int GridId { get; }

        public ConquestGameModeTeamsResourceNode(int gridId, MyCubeGrid grid) {
            if (grid == null) {
                throw new System.Exception("Resource Node Grid is null");
            }
            GridId = gridId;
            Grid = grid;
            CargoBlocks = getCargoBlocks();

            Debug.d(CargoBlocks.Count.ToString());

            if (CargoBlocks.Count != 0) {

                var cargoBlock = CargoBlocks.First().Value as IMyCubeBlock;
                var offset = cargoBlock.WorldMatrix;
                offset += MatrixD.CreateFromAxisAngle(offset.Up, ConquestGameHelper.deg2rad(-90));
                offset += MatrixD.CreateFromAxisAngle(offset.Right, ConquestGameHelper.deg2rad(90));
                var pos = new MyPositionAndOrientation(offset);

                var safeZone = ConquestGameHelper.CreateSafezone(OPTIONS.ResourceNodeGridName+" "+GridId, pos);
            }
        }

        private BlocksDict getCargoBlocks() {

            BlocksDict blocks = new BlocksDict();

            foreach(IMySlimBlock slim in Grid.GetBlocks())
            {
                if (slim.FatBlock == null) {
                    continue;
                }
                var block = (IMyCubeBlock) slim.FatBlock;
                if (block is IMyCargoContainer) {
                    blocks.Add(block.EntityId, block);
                }
            }
            return blocks;
        }
    }
}