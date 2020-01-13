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
	class ExtendedAirvent
	{
		//Event is called if an external source changes depressurization state.

		Program P;
		private IMyAirVent vent;

		private bool lastVentState;
		public bool Depressurize
		{
			get
			{
				return vent.Depressurize;
			}
			set
			{
				//This makes sure the event is only called when something external changed the vent.
				lastVentState = value;
				vent.Depressurize = value;
			}
		}
		public bool CanPressurize
		{
			get { return vent.CanPressurize; }
		}

		public bool Enabled
		{
			get { return vent.Enabled; }
			set { vent.Enabled = value; }
		}

		public string CustomName
		{
			get { return vent.CustomName; }
			set { vent.CustomName = value; }
		}

		public bool ChangedThisUpdate { get; private set; }
		List<Action> EventActions = new List<Action>();
		List<Action<ExtendedAirvent>> EventFuncs = new List<Action<ExtendedAirvent>>();

		public ExtendedAirvent(Program p, IMyAirVent vent)
		{
			P = p;
			this.vent = vent;
			lastVentState = vent.Depressurize;
		}

		public void Update()
		{
			ChangedThisUpdate = false;
			if (vent.Depressurize != lastVentState)
			{
				ChangedThisUpdate = true;
				foreach (var action in EventActions)
				{
					action();
				}
				foreach (var action in EventFuncs)
				{
					action(this);
				}
			}
			lastVentState = vent.Depressurize;
		}

		public float GetOxygenLevel()
		{
			return vent.GetOxygenLevel();
		}

		public void Subscribe(Action action)
		{
			EventActions.Add(action);
		}
		public void SubscribeFunc(Action<ExtendedAirvent> func)
		{
			EventFuncs.Add(func);
		}
	}
	#endregion
}
