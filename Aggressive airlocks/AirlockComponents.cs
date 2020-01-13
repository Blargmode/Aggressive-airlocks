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
	class AirlockComponents
	{
		//Required
		public Program P;
		public List<ExtendedDoor> inner;
		public List<ExtendedDoor> outer;
		public float secondsBeforeTimeout = 2;
		public float oxygenDifferenceRequired = .2f;

		//One of these
		public List<IMyAirVent> vents;
		public List<ExtendedAirvent> extendedVents;

		//Optional
		public AirlockStatusDisplay statusDisplay = null;
		public List<IMyLightingBlock> lights = null;
		public List<IMyGasTank> tanks = null;
		public List<IMyGasGenerator> generators = null;
		public List<IMyFunctionalBlock> farms = null;
		public List<IMyTimerBlock> outerTimers = null;
		public List<IMyTimerBlock> innerTimers = null;

		//Other
		//public bool attemptAirScoop = false; //This bool is set by an AirSystem. IE airlock doesn't know about the oxygen levels but if told should try to scoop up air. It will decide on is own if it's feasable.
		Color neutral;
		Color working;

		public AirlockComponents(Program p, List<ExtendedDoor> outer, List<ExtendedDoor> inner, List<IMyAirVent> vents)
		{
			P = p;
			this.outer = outer;
			this.inner = inner;
			this.vents = vents;
			secondsBeforeTimeout = (float)P.settings[ID.Timeout].Value;
			neutral = (Color)P.settings[ID.DefaultLampColor].Value;
			working = (Color)P.settings[ID.ChangingLampColor].Value;
			float temp = (float)P.settings[ID.OxygenDifference].Value / 100;
			if (temp <= 1 && temp >= 0)
			{
				oxygenDifferenceRequired = temp;
			}
		}
		public AirlockComponents(Program p, List<ExtendedDoor> outer, List<ExtendedDoor> inner, List<ExtendedAirvent> extendedVents)
		{
			P = p;
			this.outer = outer;
			this.inner = inner;
			this.extendedVents = extendedVents;
			secondsBeforeTimeout = (float)P.settings[ID.Timeout].Value;
			neutral = (Color)P.settings[ID.DefaultLampColor].Value;
			working = (Color)P.settings[ID.ChangingLampColor].Value;
			float temp = (float)P.settings[ID.OxygenDifference].Value / 100;
			if (temp <= 1 && temp >= 0)
			{
				oxygenDifferenceRequired = temp;
			}
		}
		public AirlockComponents(Program p, ExtendedDoor outer, ExtendedDoor inner, ExtendedAirvent extendedVents)
		{
			P = p;
			this.outer = new List<ExtendedDoor> { outer };
			this.inner = new List<ExtendedDoor> { inner };
			this.extendedVents = new List<ExtendedAirvent> { extendedVents };
			secondsBeforeTimeout = (float)P.settings[ID.Timeout].Value;
			neutral = (Color)P.settings[ID.DefaultLampColor].Value;
			working = (Color)P.settings[ID.ChangingLampColor].Value;
			float temp = (float)P.settings[ID.OxygenDifference].Value / 100;
			if (temp <= 1 && temp >= 0)
			{
				oxygenDifferenceRequired = temp;
			}
		}

		public void TriggerOuterTimers()
		{
			if(outerTimers != null)
			{
				for (int i = 0; i < outerTimers.Count; i++)
				{
					outerTimers[i].Trigger();
				}
			}
		}

		public void TriggerInnerTimers()
		{
			if (innerTimers != null)
			{
				for (int i = 0; i < innerTimers.Count; i++)
				{
					innerTimers[i].Trigger();
				}
			}
		}

		public void SetLightsWorking()
		{
			if (lights != null)
			{
				for (int i = 0; i < lights.Count; i++)
				{
					lights[i].Color = working;
					lights[i].BlinkIntervalSeconds = 1.2f;
					lights[i].BlinkLength = 40f;
				}
			}
		}
		public void SetLightsIdle()
		{
			if (lights != null)
			{
				for (int i = 0; i < lights.Count; i++)
				{
					lights[i].Color = neutral;
					lights[i].BlinkIntervalSeconds = 0;
				}
			}
		}
	}
	#endregion
}
