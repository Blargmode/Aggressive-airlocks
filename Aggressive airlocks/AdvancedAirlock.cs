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
	
	//TODO: Manual mode should only affect auto closing, not opening.
	//As it is now, you end up wiht a locked door and an unlocked door, both closed

	//TODO: Generators doesnt turn off if required on script update.

	class AdvancedAirlock : AirManagedAirlock
	{ 
		bool innerOpenRequest = false;
		bool outerOpenRequest = false;
		
		public AdvancedAirlock(AirlockComponents components) : base(components)
		{
			foreach (var door in components.outer)
			{
				door.SubscribeFunc(OuterDoorAction);
			}
			foreach (var door in components.inner)
			{
				door.SubscribeFunc(InnerDoorAction);
			}
		}

		public override void Update()
		{
			base.Update();
			
			if (AirlockState == AirlockState2.AwatingTotalLock)
			{
				if (innerOpenRequest)
				{
					if (OuterOpenCount <= 0) 
					{
						if (components.P.inAtmo)
						{
							EnableVents(false);
						}
						else
						{
							Depressurize(false);
						}
						timeout = components.P.Time + ventDeadline;
						startOxygenLevel = components.extendedVents[0].GetOxygenLevel();
						AirlockState = AirlockState2.Pressurizing;
					}
				}
				if (outerOpenRequest)
				{
					if (InnerOpenCount <= 0)
					{
						if (components.P.inAtmo && !attemptAirScoop)
						{
							EnableVents(false);
						}
						else
						{
							Depressurize(true);
						}
						timeout = components.P.Time + ventDeadline;
						startOxygenLevel = components.extendedVents[0].GetOxygenLevel();
						AirlockState = AirlockState2.Depressurizing;
					}
				}
			}

			if (AirlockState == AirlockState2.Pressurizing)
			{
				if (innerOpenRequest)
				{
					
					if(startOxygenLevel > 0.8)
					{
						//Room already pressurized. We might be in atmo?
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
						innerOpenRequest = false;
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
				}
				StatusService(AirlockState);
			}

			if (AirlockState == AirlockState2.Depressurizing)
			{
				currentOxygenLevel = components.extendedVents[0].GetOxygenLevel();
				if (components.P.Time > timeout)
				{
					if (Math.Abs(currentOxygenLevel - startOxygenLevel) < components.oxygenDifferenceRequired)
					{
						//If difference in odygen level doesnt reach expectations: spoof the oxygen reading forceing the airlock to cycle.
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
				//Regarding outside oxygen level. Only depressurize down to outside oxygen level. That will adapt to outside conditions.
				//Removed due to Sensor vent not working. Game broken.
				// || (components.P.outsideOxygenLevel != -1 && currentOxygenLevel <= components.P.outsideOxygenLevel)
				if (currentOxygenLevel < 0.1 || tanksFullSkipDepressurization || components.P.inAtmo || maybeAtmoSkipDepressurization)
				{
					if (attemptAirScoop == false)
					{
						EnableVents(false); //Prevents sucking in atmosphere
					}
					if (outerOpenRequest)
					{
						outerOpenRequest = false;
						EnableDoors(components.outer, true);
						OpenAll(components.outer);
						AirlockState = AirlockState2.OuterOpen;
					}
					else
					{
						EnableDoors(components.outer, true);
						EnableDoors(components.inner, true);
						AirlockState = AirlockState2.Neutral;
					}
					if (components.P.Time > timeout)
					{
						errorStatus = "Depressurization failed";
					}
					else
					{
						errorStatus = "";
					}
					timeout = TimeSpan.MaxValue;
				}
				StatusService(AirlockState);
			}

			if (AirlockState == AirlockState2.Unknown)
			{
				EnableDoors(components.outer, true);
				EnableDoors(components.inner, true);
				
				if(InnerOpenCount <= 0 && OuterOpenCount <= 0)
				{
					Depressurize(true);
					AirlockState = AirlockState2.Neutral;
				}
			}

		}

		public override void OuterDoorAction(ExtendedDoor door)
		{
			base.OuterDoorAction(door);

			if (AirlockState == AirlockState2.Neutral)
			{ 
				//If opening, request everything to close.
				if (door.door.Status == DoorStatus.Opening && !door.ProgramOpening)
				{
					innerOpenRequest = true;
					SendLockRequest(components.inner);
					SendLockRequest(components.outer);
					AirlockState = AirlockState2.AwatingTotalLock;
				}
			}

			if (AirlockState == AirlockState2.OuterOpen)
			{
				if (OuterOpenCount <= 0)
				{
					EnableDoors(components.inner, true);
					AirlockState = AirlockState2.Neutral;
				}
			}
		}

		public override void InnerDoorAction(ExtendedDoor door)
		{
			base.InnerDoorAction(door);

			if (AirlockState == AirlockState2.Neutral)
			{
				//If opening, request everything to close.
				if (door.door.Status == DoorStatus.Opening && !door.ProgramOpening)
				{
					outerOpenRequest = true;
					Depressurize(false); //Set to false while inner doors are open.
					SendLockRequest(components.inner);
					SendLockRequest(components.outer);
					AirlockState = AirlockState2.AwatingTotalLock;
				}
			}

			if (AirlockState == AirlockState2.InnerOpen)
			{
				if (InnerOpenCount <= 0)
				{
					Depressurize(true);
					//timeout = components.P.Time + TimeSpan.FromSeconds(components.secondsBeforeTimeout);
					timeout = components.P.Time + ventDeadline;
					startOxygenLevel = components.extendedVents[0].GetOxygenLevel();
					AirlockState = AirlockState2.Depressurizing;
				}
			}
		}
		
		public override void StatusService(AirlockState2 state)
		{
			//Lights
			switch (state)
			{
				case AirlockState2.Neutral:
					components.SetLightsIdle();
					break;
				case AirlockState2.AwatingTotalLock:
					components.SetLightsWorking();
					if (innerOpenRequest)
					{
						components.TriggerInnerTimers();
					}
					if (outerOpenRequest)
					{
						components.TriggerOuterTimers();
					}
					break;
				case AirlockState2.Unknown:
					components.SetLightsWorking();
					break;
			}

			//LCD
			if (components.statusDisplay == null) return;
			switch (state)
			{
				case AirlockState2.Neutral:
					if (errorStatus.Length > 0)
					{
						components.statusDisplay.Update(errorStatus, true);
					}
					else
					{
						components.statusDisplay.Update("Ready");
					}
					break;

				case AirlockState2.AwatingTotalLock:
					components.statusDisplay.Update("Locking doors");
					break;

				case AirlockState2.Depressurizing:
					components.statusDisplay.Update("Depressurizing");
					break;

				case AirlockState2.Pressurizing:
					components.statusDisplay.Update("Pressurizing");
					break;

				case AirlockState2.OuterOpen:
					if (errorStatus.Length > 0)
					{
						components.statusDisplay.Update(errorStatus, true);
					}
					else
					{
						components.statusDisplay.Update("Outer open");
					}
					break;

				case AirlockState2.InnerOpen:
					if (errorStatus.Length > 0)
					{
						components.statusDisplay.Update(errorStatus, true);
					}
					else
					{
						components.statusDisplay.Update("Inner open");
					}
					break;

				case AirlockState2.Unknown:
					components.statusDisplay.Update("Setup in progress");
					break;

				default:
					components.statusDisplay.Update("No idea what's going on");
					break;
			}
		}
	}
	#endregion
}
