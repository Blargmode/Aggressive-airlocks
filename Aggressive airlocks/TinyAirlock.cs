#region pre-script
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
#endregion
namespace IngameScript
{
	#region in-game
	class TinyAirlock
	{
		Program P;
		ExtendedDoor door1;
		ExtendedDoor door2;
		//bool door1OpenRequest = false;
		//bool door2OpenRequest = false;
		bool openRequest = false;

		public TinyAirlock(Program p, ExtendedDoor door1, ExtendedDoor door2)
		{
			P = p;
			this.door1 = door1;
			this.door2 = door2;
			door1.Subscribe(Door1Action);
			door2.Subscribe(Door2Action);
		}

		public void Update()
		{
			door1.Update();
			door2.Update();
		}

		public void Door1Action()
		{
			DoorAction(door1, door2);
		}
		public void Door2Action()
		{
			DoorAction(door2, door1);
		}
		private void DoorAction(ExtendedDoor me, ExtendedDoor other)
		{
			if(me.door.Status == DoorStatus.Opening && other.door.Status == DoorStatus.Opening)
			{
				//Something is fucked, close the doors. Has two people attempted to use the same airlock at the same time?
				me.door.CloseDoor();
				other.door.CloseDoor();
			}
			else if(me.door.Status == DoorStatus.Opening && !me.ProgramOpening)
			{
				if (!other.isManualDoor) openRequest = true;
				other.door.Enabled = false;
			}
			else if (me.door.Status == DoorStatus.Closed && other.door.Enabled == false && openRequest)
			{
				openRequest = false;
				other.door.Enabled = true;
				me.door.Enabled = false;
				other.ProgramOpen();
			}
			else if(me.door.Status == DoorStatus.Closed && other.door.Status == DoorStatus.Closed)
			{
				me.door.Enabled = true;
				other.door.Enabled = true;
			}
		}

	}
	#endregion
}
