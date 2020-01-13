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
	
	class ExtendedDoor
	{
		//Event is called if DoorStatus changes.
		
		Program P;
		public IMyDoor door;
		TimeSpan autoCloseTime = TimeSpan.MaxValue; //MaxValue acts as never.
		public float timeOpenExiting; //How long before the door auto closes
		public float timeOpenEntering;
		public bool autoClose;
		public bool ProgramOpening { get; private set; } //if the program requested the opening
		public bool ProgramClosing { get; private set; } //if the program requested the closing
		List<Action> EventActions = new List<Action>();
		List<Action<ExtendedDoor>> EventFuncs = new List<Action<ExtendedDoor>>();
		DoorStatus lastStatus;
		public bool isHangarDoor = false;
		public bool isManualDoor = false;
		private float autoCloseInSecondsOnceOpen = -1; //Used to trigger auto close once door reaches open status. This way the time the door takes to open doesn't matter, autoclose triggers after that.
		public bool lockRequest = false;

		public ExtendedDoor(Program p, IMyDoor door, bool autoClose = true, float timeOpenEntering = .5f, float timeOpenExiting = 2f)
		{
			P = p;
			this.door = door;
			this.autoClose = autoClose;
			this.timeOpenEntering = timeOpenEntering;
			this.timeOpenExiting = timeOpenExiting;
			lastStatus = door.Status;

			if (door is IMyAirtightHangarDoor)
			{
				isHangarDoor = true;
			}
			if(General.ContainsExact((string)P.settings[ID.ManualTag].Value , door.CustomName))
			{
				this.autoClose = false;
				isManualDoor = true;
			}
		}

		public void Update()
		{			
			if (autoClose)
			{
				if (P.Time > autoCloseTime)
				{
					//Close door after set time
					door.CloseDoor();
					ProgramClosing = true;
					autoCloseTime = TimeSpan.MaxValue;
				}
				else if (door.Status == DoorStatus.Open && autoCloseTime == TimeSpan.MaxValue)
				{
					//Queue door closing
					//Don't need to check if the program or the user is opening the door, as the program will set auto close on its own when opening a door 
					if (timeOpenEntering >= 0)
					{
						autoCloseTime = P.Time + TimeSpan.FromSeconds(timeOpenEntering);
					}
				}
			}

			if (lockRequest && door.Status == DoorStatus.Closed)
			{
				door.Enabled = false;
				lockRequest = false;
			}
			
			//Check if door status has changed. If it has, trigger event.
			if (door.Status != lastStatus)
			{
				//Launch auto close time once door is open.
				if (autoClose && autoCloseInSecondsOnceOpen >= 0 && door.Status == DoorStatus.Open)
				{
					autoCloseTime = P.Time + TimeSpan.FromSeconds(autoCloseInSecondsOnceOpen);
					autoCloseInSecondsOnceOpen = -1;
				}

				//reset ProgramOpening once the door is closed again. Have it remain while closed, so what we know who closed it.
				if (door.Status == DoorStatus.Closed)
				{
					ProgramOpening = false;
				}
				//reset ProgramClosing once the door is open again. Have it remain while open, so what we know who opened it.
				if (door.Status == DoorStatus.Open)
				{
					ProgramOpening = false;
				}

				//Trigger event
				foreach (var action in EventActions)
				{
					action();
				}
				foreach (var func in EventFuncs)
				{
					func(this);
				}
			}
			
			lastStatus = door.Status;
		}

		public void Subscribe(Action action)
		{
			EventActions.Add(action);
		}

		public void SubscribeFunc(Action<ExtendedDoor> func)
		{
			EventFuncs.Add(func);
		}

		//Opens a door programatically. As opposed to the user opening the door
		//Aka, set the programResponsible variable when opening and set a time to close.
		public void ProgramOpen()
		{
			door.OpenDoor();
			ProgramOpening = true;
			autoCloseInSecondsOnceOpen = timeOpenExiting;	//used to be below line but that didn't wait for the door to open before starting the timer.
															//Used to be a bool but now you can optinally send other values than default to it to keep it open loonger.
									  //autoCloseTime = P.Time + TimeSpan.FromSeconds(timeOpenExiting);
		}
		public void ProgramOpen(float seconds)
		{
			door.OpenDoor();
			ProgramOpening = true;
			autoCloseInSecondsOnceOpen = seconds;	//used to be below line but that didn't wait for the door to open before starting the timer.
													//Used to be a bool but now you can optinally send other values than default to it to keep it open loonger.
													//autoCloseTime = P.Time + TimeSpan.FromSeconds(timeOpenExiting);
		}
	}
	#endregion
}
