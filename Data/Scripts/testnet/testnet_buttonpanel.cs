using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;

namespace testnet_buttonpanel
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ButtonPanel), true)]
    public class Testnet_Panel : MyGameLogicComponent
    {
        MyObjectBuilder_EntityBase objectBuilder;
        IMyButtonPanel Button;


        public override void Close()
        {
            Sandbox.Game.MyVisualScriptLogicProvider.ButtonPressedEntityName -= ButtonPressedEntityName;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            this.objectBuilder = objectBuilder;
            Button = Entity as IMyButtonPanel;
            Sandbox.Game.MyVisualScriptLogicProvider.ButtonPressedEntityName += ButtonPressedEntityName;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return objectBuilder;
        }

        public void ButtonPressedEntityName(System.String name, System.Int32 button, System.Int64 playerId, System.Int64 blockId)
        {
            Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage(name);
            // if (name.Contains("testnet_button"))
            // {
                var sync = MyAPIGateway.Multiplayer;
                sync.SendMessageToServer(5289, Encoding.UTF8.GetBytes(string.Format("ButtonPushSpawner " + playerId)), true);
            //}
        }
    }
}