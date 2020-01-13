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

	enum AirlockState2
	{
		Neutral,
		InnerOpen,
		OuterOpen,
		Pressurizing,
		Depressurizing,
		AwatingInnerLock,
		AwatingOuterLock,
		AwatingTotalLock,
		Unknown
	}

	abstract class AirManagedAirlock
	{
		public AirlockComponents components;

		public List<ExtendedDoor> outerLockRequest = new List<ExtendedDoor>(); //All doors that have a close request
		public List<ExtendedDoor> innerLockRequest = new List<ExtendedDoor>(); //All doors that have a close request

		public bool outerChange = false;
		public bool innerChange = false;
		public bool ventChange = false;

		public TimeSpan timeout = TimeSpan.MaxValue;

		public TimeSpan ventDeadline;
		public double startOxygenLevel = 0; //Used for sensing if (de)pressurtization is working.
		public double currentOxygenLevel = 0; //Instead of initializing a new every time.

		public bool maybeAtmoSkipDepressurization = false;

		bool generatorsEnabled = false;
		public bool GeneratorsEnabled
		{
			get { return generatorsEnabled; }
			set
			{
				generatorsEnabled = value;
				if (components.generators != null)
				{
					for (int i = 0; i < components.generators.Count; i++)
					{
						components.generators[i].Enabled = value;
					}
				}
				if (components.farms != null)
				{
					for (int i = 0; i < components.farms.Count; i++)
					{
						components.farms[i].Enabled = value;
					}
				}
			}
		}
		public bool attemptAirScoop = false;
		public bool tanksFullSkipDepressurization = false;

		AirlockState2 airlockState = AirlockState2.Unknown;
		public AirlockState2 AirlockState
		{
			get
			{
				return airlockState;
			}
			set
			{
				airlockState = value;
				StatusService(value);
			}
		}

		int outerOpenCount = 0;
		public int OuterOpenCount
		{
			get
			{
				return outerOpenCount;
			}
			set
			{
				outerOpenCount = value;
				if(outerOpenCount < 0)
				{
					//Something is wrong. Recount openness.
					CalcOpenCount();
				}
			}
		}
		int innerOpenCount = 0;
		public int InnerOpenCount
		{
			get
			{
				return innerOpenCount;
			}
			set
			{
				innerOpenCount = value;
				if (innerOpenCount < 0)
				{
					//Something is wrong. Recount openness.
					CalcOpenCount();
				}
			}
		}

		public string errorStatus = "";

		public AirManagedAirlock(AirlockComponents components)
		{
			this.components = components;

			ventDeadline = TimeSpan.FromSeconds(components.secondsBeforeTimeout);

			GeneratorsEnabled = false;

			CalcOpenCount();
		}

		//Override this
		public virtual void Update()
		{
			outerChange = false;
			innerChange = false;
			ventChange = false;
			

			for (int i = 0; i < components.outer.Count; i++)
			{
				components.outer[i].Update();
			}
			for (int i = 0; i < components.inner.Count; i++)
			{
				components.inner[i].Update();
			}
			for (int i = 0; i < components.extendedVents.Count; i++)
			{
				components.extendedVents[i].Update();
			}



			if (components.tanks != null)
			{
				//air system
				double totalFillRatio = 0;
				for (int i = 0; i < components.tanks.Count; i++)
				{
					totalFillRatio = components.tanks[i].FilledRatio;
				}
				totalFillRatio = totalFillRatio / components.tanks.Count;
				
				//Keep oxygen tanks at 95% or lower.
				if(totalFillRatio > .95)
				{
					tanksFullSkipDepressurization = true;
				}
				else
				{
					tanksFullSkipDepressurization = false;
				}

				//Enable scoops if below 65%;
				if (totalFillRatio > .7)
				{
					attemptAirScoop = false;
				}
				else if(totalFillRatio < .65)
				{
					attemptAirScoop = true;
				}

				if (GeneratorsEnabled == true && totalFillRatio > .7)
				{
					GeneratorsEnabled = false;
				}
				else if (GeneratorsEnabled == false && totalFillRatio < .3)
				{
					GeneratorsEnabled = true;
				}
			}
		}


		public virtual void OuterDoorAction(ExtendedDoor door)
		{
			if(door.door.Status == DoorStatus.Opening)
			{
				OuterOpenCount++;
			}
			else if(door.door.Status == DoorStatus.Closed)
			{
				OuterOpenCount--;
			}
		}

		public virtual void InnerDoorAction(ExtendedDoor door)
		{
			if (door.door.Status == DoorStatus.Opening)
			{
				InnerOpenCount++;
			}
			else if (door.door.Status == DoorStatus.Closed)
			{
				InnerOpenCount--;
			}
		}


		//Lock requiset managed by the airlock
		public void RequestLockOuter()
		{
			outerLockRequest.AddList(components.outer);
		}

		//Lock requiset managed by the airlock
		public void RequestLockInner()
		{
			innerLockRequest.AddList(components.inner);
		}

		public void EnableDoors(List<ExtendedDoor> doors, bool enabled)
		{
			for (int i = 0; i < doors.Count; i++)
			{
				doors[i].door.Enabled = enabled;
			}
		}

		//Lock request managed by the door
		//force overrides inAtmo check.
		public void SendLockRequest(List<ExtendedDoor> doors, bool force = false)
		{
			if (!force && components.P.inAtmo) return; //Dont do it if in atmo.

			for (int i = 0; i < doors.Count; i++)
			{
				doors[i].lockRequest = true;
			}
		}

		public void Depressurize(bool depressurize)
		{
			for (int i = 0; i < components.extendedVents.Count; i++)
			{
				components.extendedVents[i].Enabled = true;
				components.extendedVents[i].Depressurize = depressurize;
			}
		}

		public void EnableVents(bool enable)
		{
			for (int i = 0; i < components.extendedVents.Count; i++)
			{
				components.extendedVents[i].Enabled = enable;
			}
		}

		public void OpenAll(List<ExtendedDoor> doors)
		{
			for (int i = 0; i < doors.Count; i++)
			{
				if (!doors[i].isManualDoor)
				{
					doors[i].ProgramOpen();
				}
			}
		}

		//onlyManualDoors was onlyHangarDoors, but hangar doors are manual, so, makes sense.
		public void CloseAll(List<ExtendedDoor> doors, bool onlyManualDoors = false)
		{
			for (int i = 0; i < doors.Count; i++)
			{
				if (!onlyManualDoors || doors[i].isManualDoor || doors[i].isHangarDoor)
				{
					doors[i].door.CloseDoor();
				}
			}
		}
		
		/*
		[Obsolete]
		public bool AllClosed(List<ExtendedDoor> doors)
		{
			for (int i = 0; i < doors.Count; i++)
			{
				if (doors[i].door.Status != DoorStatus.Closed)
				{
					return false;
				}
			}
			return true;
		}

		[Obsolete]
		public void OuterAction()
		{
			outerChange = true;
		}
		[Obsolete]
		public void InnerAction()
		{
			innerChange = true;
		}
		[Obsolete]
		public void VentAction()
		{
			ventChange = true;
		}
		*/

		public void CalcOpenCount()
		{
			outerOpenCount = 0;
			for (int i = 0; i < components.outer.Count; i++)
			{
				if(components.outer[i].door.Status != DoorStatus.Closed)
				{
					outerOpenCount++;
				}
			}
			innerOpenCount = 0;
			for (int i = 0; i < components.inner.Count; i++)
			{
				if (components.inner[i].door.Status != DoorStatus.Closed)
				{
					innerOpenCount++;
				}
			}
		}

		//Override this
		public abstract void StatusService(AirlockState2 state);
	}
	#endregion
}
