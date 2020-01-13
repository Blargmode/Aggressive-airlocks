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
	#region not-in-game
	[Obsolete]
	class AirSystem
	{
		//NOTE:
		//If an airlock has the inner door closed and out outer open, and there is air: You can grab that air!
		//Needs to know what tanks were connected to though.

		Program P;
		List<IMyGasTank> tanks;
		List<IMyGasGenerator> generators;
		List<VentInv> vents;
		List<AirlockComponents> connectedAirlocks = new List<AirlockComponents>();

		bool generatorsEnabled = false;

		public bool GeneratorsEnabled
		{
			get { return generatorsEnabled; }
			set
			{
				generatorsEnabled = value;
				for (int i = 0; i < generators.Count; i++)
				{
					generators[i].Enabled = value;
				}
				for (int i = 0; i < connectedAirlocks.Count; i++)
				{
					//connectedAirlocks[i].attemptAirScoop = value;
				}
			}
		}

		public AirSystem(Program p, List<IMyGasTank> tanks, List<IMyGasGenerator> generators, List<VentInv> vents)
		{
			P = p;
			this.tanks = tanks;
			this.generators = generators;
			this.vents = vents;

			foreach (var vent in vents)
			{
				if (!connectedAirlocks.Contains(vent.airlockComponents))
				{
					connectedAirlocks.Add(vent.airlockComponents);
				}
			}

			GeneratorsEnabled = false; //Set initial state.
		}

		public void Update()
		{
			double totalFillRatio = 0;
			for (int i = 0; i < tanks.Count; i++)
			{
				totalFillRatio = tanks[i].FilledRatio;
			}
			totalFillRatio = totalFillRatio / tanks.Count;

			//P.Echo("Gens " + GeneratorsEnabled);
			//P.Echo("Fill " + totalFillRatio + "\n");


			if (GeneratorsEnabled == true && totalFillRatio > .7)
			{
				GeneratorsEnabled = false;
			}
			else if (GeneratorsEnabled == false && totalFillRatio < .3 )
			{
				GeneratorsEnabled = true;
			}
		}
		
		public static bool BlockBehindHasInvotory(IMyTerminalBlock startBlock, out IMyTerminalBlock other)
		{

			if (GetAdjacentBlock(startBlock, Base6Directions.Direction.Backward, out other))
			{
				if (other.HasInventory)
				{
					//Yay, search complete;
					return true;
				}
			}
			return false;
		}

		private static bool GetAdjacentBlock(IMyTerminalBlock startBlock, Base6Directions.Direction dir, out IMyTerminalBlock adjacent)
		{
			adjacent = startBlock; //Placeholder

			var grid = startBlock.CubeGrid;

			var gridPos = grid.WorldToGridInteger(startBlock.WorldMatrix.Translation + (startBlock.WorldMatrix.GetDirectionVector(dir) * grid.GridSize));
			var block = grid.GetCubeBlock(gridPos)?.FatBlock as IMyTerminalBlock;

			if (block != null)
			{
				// .... ?!
				adjacent = block;
				return true;
			}
			return false;
		}
	}

	//Workaround becaue vent's dosesnt have inventories.
	class VentInv
	{
		public IMyAirVent vent;
		public IMyTerminalBlock inventory;
		public AirlockComponents airlockComponents;

		public VentInv(IMyAirVent vent, IMyTerminalBlock inventory, AirlockComponents airlockComponents = null)
		{
			this.vent = vent;
			this.inventory = inventory;
			this.airlockComponents = airlockComponents;
		}
	}
	#endregion
}
