
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using System;
using System.Text;
using VRage;
using System.Linq;
using VRage.ModAPI;

namespace ConquestGame
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ButtonPanel), false)] //False is better in most cases
    public class ConquestGame_ButtonPanel : MyGameLogicComponent
    {
        IMyButtonPanel base_button;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base_button = Entity as IMyButtonPanel;
            if (base_button != null)
            {
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            //Only register if "real" grid, i.e not a projector
            if (base_button.CubeGrid.Physics != null)
            {
                MyVisualScriptLogicProvider.ButtonPressedTerminalName += ButtonPressed;
            }
        }

        public void ButtonPressed(string name, int button, long playerId, long blockId)
        {
            if (blockId == base_button.EntityId)
            {
                if (base_button.CustomName.Contains(OPTIONS.VehicleSpawnButtonBlockName))
                {
                    var sync = MyAPIGateway.Multiplayer;
                    sync.SendMessageToServer(OPTIONS.SpawnVehicleRequestHandlerId, Encoding.UTF8.GetBytes(string.Format(MESSAGES.SpawnVehicleRequest + " " + playerId)), true);
                }
            }
        }

        public override void Close()
        {
            MyVisualScriptLogicProvider.ButtonPressedTerminalName -= ButtonPressed;
        }
    }
}