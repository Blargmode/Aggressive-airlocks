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
	class SimpleGroupAirlock
	{
		Program P;
		ExtendedDoor[] outer;
		ExtendedDoor[] inner;

		bool outerChange = false;
		bool innerChange = false;

		int outerOpen = 0;
		int innerOpen = 0;

		bool soloMode = false; //IF all doors have the #AL tag, ie no inner doors: then only one door can be opened at a time.
		
		public SimpleGroupAirlock(Program p, List<ExtendedDoor> outer, List<ExtendedDoor> inner)
		{
			P = p;
			this.outer = outer.ToArray();
			this.inner = inner.ToArray();

			if(this.inner.Length == 0)
			{
				soloMode = true;

				for (int i = 0; i < this.outer.Length; i++)
				{
					this.outer[i].SubscribeFunc(OuterSoloFunc);
					this.outer[i].door.Enabled = true;
				}
			}
			else
			{
				for (int i = 0; i < this.outer.Length; i++)
				{
					this.outer[i].SubscribeFunc(OuterFunc);
					this.outer[i].door.Enabled = true;
				}
				for (int i = 0; i < this.inner.Length; i++)
				{
					this.inner[i].SubscribeFunc(InnerFunc);
					this.inner[i].door.Enabled = true;
				}
			}

			
		}

		public void Update()
		{
			outerChange = false;
			innerChange = false;
			for (int i = 0; i < outer.Length; i++)
			{
				outer[i].Update();
			}
			for (int i = 0; i < inner.Length; i++)
			{
				inner[i].Update();
			}

			if (!soloMode)
			{
				if (outerChange && outerOpen == 0)
				{
					EnableDoors(inner, true);
				}
				if (innerChange && innerOpen == 0)
				{
					EnableDoors(outer, true);
				}
			}
		}

		public void EnableDoors(ExtendedDoor[] doors, bool enable)
		{
			for (int i = 0; i < doors.Length; i++)
			{
				doors[i].door.Enabled = enable;
			}
		}

		public void OuterFunc(ExtendedDoor door)
		{
			outerChange = true;
			if(door.door.Status == DoorStatus.Opening)
			{
				outerOpen++;
				EnableDoors(inner, false);
			}
			if (door.door.Status == DoorStatus.Closed)
			{
				outerOpen--;
			}
		}
		public void InnerFunc(ExtendedDoor door)
		{
			innerChange = true;
			if (door.door.Status == DoorStatus.Opening)
			{
				innerOpen++;
				EnableDoors(outer, false);
			}
			if (door.door.Status == DoorStatus.Closed)
			{
				innerOpen--;
			}
		}

		public void OuterSoloFunc(ExtendedDoor door)
		{
			outerChange = true;
			if (door.door.Status == DoorStatus.Opening)
			{
				for (int i = 0; i < outer.Length; i++)
				{
					if(outer[i] != door)
					{
						outer[i].door.Enabled = false;
					}
				}
			}
			if (door.door.Status == DoorStatus.Closed)
			{
				for (int i = 0; i < outer.Length; i++)
				{
					outer[i].door.Enabled = true;
				}
			}
		}

	}
	#endregion
}
