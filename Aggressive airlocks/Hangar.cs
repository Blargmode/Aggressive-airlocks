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

	//DONE:
	//Clever way to know if all doors are closed without extra loops
	//Currently calling AllClosed() loops thrugh everything. There's got to be a better way to keep track of that.

	//DONE:
	//Remove requirement for inner door.

	//DONE:
	//For the manual tag, add the doors to the same bunch that hangar doors are. So when the command to close hangar doors is issued, manual doors close as well.
	
	class Hangar : AirManagedAirlock
	{
		bool lastAttemptScoop = false;
		
		public Hangar(AirlockComponents components) : base(components)
		{
			foreach (var door in components.outer)
			{
				door.SubscribeFunc(OuterDoorAction);
				//Disable auto close for hangar doors.
				if (door.isHangarDoor)
				{
					door.timeOpenEntering = -1;
					door.timeOpenExiting = -1;
					door.autoClose = false;
				}
			}
			foreach (var door in components.inner)
			{
				door.SubscribeFunc(InnerDoorAction);
				//Disable auto close for hangar doors.
				if (door.isHangarDoor)
				{
					door.timeOpenEntering = -1;
					door.timeOpenExiting = -1;
					door.autoClose = false;
				}
			}
			foreach (var vent in components.extendedVents)
			{
				vent.SubscribeFunc(VentAction);
			}
		}

		public override void Update()
		{
			base.Update();



			if (AirlockState == AirlockState2.AwatingOuterLock)
			{
				if (OuterOpenCount <= 0)
				{
					Depressurize(false);
					timeout = components.P.Time + TimeSpan.FromSeconds(components.secondsBeforeTimeout);
					AirlockState = AirlockState2.Pressurizing;
				}
			}
			else if (AirlockState == AirlockState2.AwatingInnerLock)
			{
				if (InnerOpenCount <= 0)
				{
					Depressurize(true);
					timeout = components.P.Time + TimeSpan.FromSeconds(components.secondsBeforeTimeout);
					AirlockState = AirlockState2.Depressurizing;
				}
			}



			if (AirlockState == AirlockState2.Depressurizing)
			{
				currentOxygenLevel = components.extendedVents[0].GetOxygenLevel();
				if (components.P.Time > timeout)
				{
					if (Math.Abs(currentOxygenLevel - startOxygenLevel) < components.oxygenDifferenceRequired)
					{
						//If difference in oxygen level doesnt reach expectations: spoof the oxygen reading forceing the airlock to cycle.
						currentOxygenLevel = 0;
						timeout = TimeSpan.MaxValue;
					}
					else
					{
						//Restart the test
						startOxygenLevel = currentOxygenLevel;
						timeout = components.P.Time + ventDeadline;
					}
				}

				if (currentOxygenLevel < 0.1 || tanksFullSkipDepressurization || components.P.inAtmo || maybeAtmoSkipDepressurization)
				{
					EnableDoors(components.outer, true);
					OpenAll(components.outer);
					EnableVents(false);
					if (components.P.Time > timeout)
					{
						errorStatus = "Depressurization failed";
					}
					else
					{
						errorStatus = "";
					}
					timeout = TimeSpan.MaxValue;
					AirlockState = AirlockState2.OuterOpen;
				}
				StatusService(AirlockState);
			}
			else if (AirlockState == AirlockState2.Pressurizing)
			{
				if (startOxygenLevel > 0.8)
				{
					//Rom already pressurized. We might be in atmo?
					maybeAtmoSkipDepressurization = true;
				}
				else
				{
					maybeAtmoSkipDepressurization = false;
				}

				currentOxygenLevel = components.extendedVents[0].GetOxygenLevel();
				if (components.P.Time > timeout)
				{
					if (Math.Abs(currentOxygenLevel - startOxygenLevel) < components.oxygenDifferenceRequired)
					{
						//If difference in oxygen level doesnt reach expectations: spoof the oxygen reading forceing the airlock to cycle.
						currentOxygenLevel = 1;
						timeout = TimeSpan.MaxValue;
					}
					else
					{
						//Restart the test
						startOxygenLevel = currentOxygenLevel;
						timeout = components.P.Time + ventDeadline;
					}
				}
				if (currentOxygenLevel > 0.9 || components.P.inAtmo)
				{
					EnableDoors(components.inner, true);
					OpenAll(components.inner);
					if (components.P.Time > timeout)
					{
						errorStatus = "Pressurization failed";
					}
					else
					{
						errorStatus = "";
					}
					timeout = TimeSpan.MaxValue;
					AirlockState = AirlockState2.InnerOpen;
				}
				StatusService(AirlockState);
			}

			if (AirlockState == AirlockState2.Unknown)
			{
				if (components.extendedVents[0].Depressurize)
				{
					Depressurize(true);
					EnableVents(false);
					if (InnerOpenCount <= 0)
					{
						AirlockState = AirlockState2.OuterOpen;
					}
					else
					{
						SendLockRequest(components.inner);
						CloseAll(components.inner, true);
						AirlockState = AirlockState2.AwatingInnerLock;
					}
				}
				else
				{
					Depressurize(false);
					if (OuterOpenCount <= 0)
					{
						AirlockState = AirlockState2.InnerOpen;
					}
					else
					{
						SendLockRequest(components.outer);
						CloseAll(components.outer, true);
						AirlockState = AirlockState2.AwatingOuterLock;
					}
				}
			}

			if (AirlockState == AirlockState2.OuterOpen)
			{
				//Air scoops
				if (attemptAirScoop)
				{
					if (attemptAirScoop != lastAttemptScoop)
					{
						EnableVents(true);
						lastAttemptScoop = attemptAirScoop;
					}
				}
				else if (attemptAirScoop == false)
				{
					if (attemptAirScoop != lastAttemptScoop)
					{
						EnableVents(false);
						lastAttemptScoop = attemptAirScoop;
					}
				}

				//Deprecated? Atmo mode isnt removed completely as of writing, but it's being chagned to use atmo maybe.
				//Shut inside doors if outside oxygen changes.
				if (components.P.inAtmoChanged)
				{
					components.P.Me.CustomData += "\n in atmo changed: " + components.P.inAtmo;
					if (components.P.inAtmo == false)
					{
						CloseAll(components.inner, true);
						SendLockRequest(components.inner);
						AirlockState = AirlockState2.AwatingInnerLock;
					}
					else
					{
						EnableDoors(components.inner, true);
					}
				}
			}

		}

		public override void OuterDoorAction(ExtendedDoor door)
		{
			base.OuterDoorAction(door);
		}

		public override void InnerDoorAction(ExtendedDoor door)
		{
			base.InnerDoorAction(door);
		}

		public void VentAction(ExtendedAirvent vent)
		{
			if (vent.Depressurize)
			{
				//Open to outside.
				vent.Depressurize = false; //Undo users change until door status is correct
				SendLockRequest(components.inner);
				CloseAll(components.inner, true);
				AirlockState = AirlockState2.AwatingInnerLock;
			}
			else
			{
				//Open to inside.
				vent.Depressurize = true; //Undo users change until door status is correct
				SendLockRequest(components.outer);
				CloseAll(components.outer, true);
				AirlockState = AirlockState2.AwatingOuterLock;
			}
		}


		public override void StatusService(AirlockState2 state)
		{
			//Lights and timers
			switch (state)
			{
				case AirlockState2.InnerOpen:
				case AirlockState2.OuterOpen:
					components.SetLightsIdle();
					break;
				case AirlockState2.AwatingInnerLock:
					components.TriggerOuterTimers();
					components.SetLightsWorking();
					break;
				case AirlockState2.AwatingOuterLock:
					components.TriggerInnerTimers();
					components.SetLightsWorking();
					break;
				case AirlockState2.Unknown:
					components.SetLightsWorking();
					break;
			}

			//LCD
			if (components.statusDisplay == null) return;
			switch (state)
			{
				case AirlockState2.InnerOpen:
					if (errorStatus.Length > 0)
					{
						components.statusDisplay.Update(errorStatus, true);
					}
					else
					{
						if (components.P.inAtmo)
						{
							components.statusDisplay.Update("Inner open - Atmo mode");
						}
						else
						{
							components.statusDisplay.Update("Inner open");
						}
					}
					break;

				case AirlockState2.OuterOpen:
					if (errorStatus.Length > 0)
					{
						components.statusDisplay.Update(errorStatus, true);
					}
					else
					{
						if (components.P.inAtmo)
						{
							components.statusDisplay.Update("Outer open - Atmo mode");
						}
						else
						{
							components.statusDisplay.Update("Outer open");
						}
					}
					break;

				case AirlockState2.AwatingOuterLock:
					components.statusDisplay.Update("Locking outer");
					break;

				case AirlockState2.AwatingInnerLock:
					components.statusDisplay.Update("Locking inner");
					break;

				case AirlockState2.Pressurizing:
					components.statusDisplay.Update("Pressurizing");
					break;

				case AirlockState2.Depressurizing:
					components.statusDisplay.Update("Depressurizing");
					break;

				case AirlockState2.Unknown:
					components.statusDisplay.Update("Setup in progress");
					break;

				default:
					break;
			}
		}
	}
	#endregion
}
