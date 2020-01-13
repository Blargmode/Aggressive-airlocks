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
	public class ExecutionTime
	{
		Program P;

		const int Size = 10;
		int[] Count;
		int Index;
		DateTime StartTime;
		double[] Times;


		public ExecutionTime(Program p)
		{
			P = p;
			Count = new int[Size];
			Times = new double[Size];
		}

		public void Update()
		{
			//Run last in the tick;
			if (Index >= Size) Index = 0;
			Count[Index] = P.Runtime.CurrentInstructionCount;
			Times[Index] = P.Runtime.LastRunTimeMs;
			Index++;
		}

		public void Start()
		{
			StartTime = DateTime.Now;
		}

		public void End()
		{
			if (Index >= Size) Index = 0;
			Count[Index] = P.Runtime.CurrentInstructionCount;
			Times[Index] = (DateTime.Now - StartTime).TotalMilliseconds;
			Index++;

		}

		public double GetAvrage()
		{
			return Count.Average();
		}

		public int GetPeak()
		{
			return Count.Max();
		}

		public double GetAvrageTime()
		{
			return Times.Average();
		}

		public double GetPeakTime()
		{
			return Times.Max();
		}

	}
	#endregion
}
