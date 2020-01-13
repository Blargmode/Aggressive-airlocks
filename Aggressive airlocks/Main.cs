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
namespace IngameScript
{
	public class Program : MyGridProgram
	{
		#endregion
		#region in-game

		//Game bugs in my way:
		//* Inventory.IsConnectedTo does not work. Everything is connected to everything apparantly.
		//* Oxygen level in the bounding box of ships arn't updated reliably. You can be 1/3 of the way to the moon and still be able to breathe.
		//* OxygenFarm has no Enabled property.

		//BUGS:


		//Idea: 
		//Find out how many updates you can skip and still see all 4 states of a door. Then divide all doors by that number: Reducing the number of doors being updated each run.
		//Also with the event system we could probably have all doors in one list to mabe update dividing easier. Lets wait and see for the more advanced airlocks


		//NOPE: New air system idea. //Can't: Game is too buggy.
		//Tag oxygen generators and oxygen tanks with #AL for them to operate smart. I.E. turn off generation if oxygen is > 50%.
		//Air systems can operate independently.
		//Maybe even suck in air through vents in atmosphere?

			

		//Idea: Halfway implemented. Atmo mode turns off above altitude.
		//For detecting atmo
		//The vents can already detect atmo. This could disable the regular airlock features. But, when do we tun on again? The old method was using a sensor vent. But we could also check
		//altitude when disabling the airlokc feature, then if altitude rises above that value, enable airlocks again.
		
		//Idea:
		//Since everything is fucking broken:
		//Sensor vent doesn't work, so: 
		//Manualy send command "atmo": this will set the system in atmo mode.
		//Manually send it again to turn atmo mode off.
		//Show atmo mode in detailed info.
		//Show atmo mode on LCDs? As the state maybe.
		//Automatically turn off atmo mode if ship rises about set altitude above sealevel.
		//Hangar atmo mode: All doors unlocked at all times.
		//Advanced airlock atmo mode: Vent disabled, goes directly to openign the other door without depressurizing.
		//Tiny airlock atmo mode: Opens other immediately when first is opened.
		//Simple Group: simply never disables anything.
		

		public TimeSpan Time { get; private set; }

		ExecutionTime ExeTime;

		ulong runCount = 0;

		List<ExtendedDoor> ExtendedDoors = new List<ExtendedDoor>();
		List<TinyAirlock> TinyAirlocks = new List<TinyAirlock>();
		List<AdvancedAirlock> AdvancedAirlocks = new List<AdvancedAirlock>();
		List<Hangar> Hangars = new List<Hangar>();
		List<SimpleGroupAirlock> SimpleGroupAirlocks = new List<SimpleGroupAirlock>();
		//List<AirSystem> AirSytems = new List<AirSystem>();
		List<IMyTextPanel> StatusLCDs = new List<IMyTextPanel>();
		

		/*
		private IMyAirVent sensorVent;
		public float outsideOxygenLevel { get; private set; } = -1;
		private float lastOutsideOxygenLevel = -1;
		public bool outsideOxygenChange { get; private set; } = false;
		public readonly float oxygenLevelThreshold = .5f;
		*/
		public IMyShipController shipController; //used for finding if we're in a planets gravity well.
		public double altitude = 0;
		public bool altitudeAccurate { get; private set; } = false; //Wheter altitude was measured this run.

		public bool inAtmo { get; private set; } = false;
		public bool inAtmoChanged = true;

		public IMyTextPanel debugLCD;
		

		string airlockTag = "#AL";
		string ignoreTag = "#Ignore";
		string hangartag = "#Hangar";
		//string sensorTag = "#Sensor";
		float timeOpenExiting;
		float timeOpenEntering;
		double inAtmoDisableAltitude = 5000;

		public Dictionary<ID, Setting> settings;
		public Dictionary<Str, string> strings;
		private List<TimedMessage> timedMessages = new List<TimedMessage>();
		
		string detailedInfoText = "";
		string finalDetailedInfoText = "";

		int doorCount = 0;
		int tinyAirlockCount = 0;
		int smartAirlockCount = 0;
		int groupAirlockCount = 0;
		int hangarCount = 0;
		int simpleGroupCount = 0;
		int hangarAirSystemsCount = 0;
		int groupAirlockAirSystemsCount = 0;

		IEnumerator<bool> InitStateMachine;
		int MaxInstructionsPerTick;
		bool Initialized = false;

		int initCounter = 0; //For looks when program is starting on a hunge grid.

		/* Quick program to monitor oxygen level
		IMyTextPanel lcd;
		IMyAirVent vent;

		public Program()
		{
			Runtime.UpdateFrequency = UpdateFrequency.Update10;
			lcd = GridTerminalSystem.GetBlockWithName("Outside oxygen panel") as IMyTextPanel;
			vent = GridTerminalSystem.GetBlockWithName("Outside oxygen vent") as IMyAirVent;
		}
		public void Main()
		{
			lcd.WritePublicText(vent.GetOxygenLevel().ToString());
		}
		*/

		public Program()
		{
			//Automatically run programmable block every 10th tick
			//Runtime.UpdateFrequency = UpdateFrequency.Update10;
			Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;

			ExeTime = new ExecutionTime(this);

			MaxInstructionsPerTick = Runtime.MaxInstructionCount / 4; //Limit how much the script can do on a single tick

			InitStateMachine = Init();

			debugLCD = GridTerminalSystem.GetBlockWithName("ALDEBUG") as IMyTextPanel;
			if(debugLCD != null)
			{
				debugLCD.ShowPublicTextOnScreen();
			}
			

			#region Strings

			//New way to store text
			//This can be done easily programatically unlike the old way of a Strings class with constants.
			strings = new Dictionary<Str, string>
			{
				{ Str.ScriptName, "Aggressive airlocks" },
				{ Str.AirlockTag, "Airlock tag" },
				{ Str.HangarTag, "Hangar tag" },
				{ Str.SensorTag, "Oxygen sensor tag" },
				{ Str.IgnoreTag, "Ignore door tag" },
				{ Str.ManualTag, "Manual door tag" },
				{ Str.AutoCloseDelayExiting, "Auto close delay exiting (s)" },
				{ Str.AutoCloseDelayEntering, "Auto close delay entering (s)" },
				{ Str.AutoCloseDelayRegularDoors, "Auto close delay regular doors (s)" },
				{ Str.Timeout, "[Advanced] Timeout (s)" },
				{ Str.EnableRegularDoors, "Auto close regular doors" },
				{ Str.EnableTinyAirlocks, "Enable Tiny Airlocks" },
				{ Str.EnableSmartAirlocks, "Enable Smart Airlocks" },
				{ Str.EnableGroupAirlocks, "Enable Group Airlocks (incl. Simple and Hangar)" },
				{ Str.DefaultLampColor, "Airlock free light color" },
				{ Str.ChangingLampColor, "Airlock in use light color" },
				{ Str.ShowProblemsOnHud, "Show airlocks with problems on HUD" },
				{ Str.Settings, "settings" },
				{ Str.SettingsInstructions, "To change settings: Edit the value after the colon, then send the command 'Update' to the script." },
				{ Str.CommandsInstructions, "To send a command, enter it as an argument in the programmable block and press run. (Can also be done via an action, e.g. in a button)." },
				{ Str.OxygenDifference, "[Advanced] Timeout oxygen delta (%)" },
				{ Str.SmartAirlock, "Smart airlock" },
				{ Str.GroupAirlock, "Airlock" },
				{ Str.Hangar, "Hangar" },
				{ Str.Error, "Error" },
				{ Str.SetupLog, "Setup log" },
				{ Str.SeeCustomData, "Settings: See custom data." },
				{ Str.ArgumentNotUnderstood, "was not understood. Avalible arguments are: 'update', 'atmo', 'atmo on', 'atmo off'." },
				{ Str.InAtmo, "Atmosphere mode enabled" },
				{ Str.InAtmoDisableAltitude, "Auto disable atmo mode above (m)" }
			};

			#endregion
		}

		IEnumerator<bool> Init() {
			
			#region Settings
			//Default settings
			settings = new Dictionary<ID, Setting>
			{
				{ ID.AirlockTag, new Setting(strings[Str.AirlockTag], "#AL") },
				{ ID.HangarTag, new Setting(strings[Str.HangarTag], "#Hangar") },
				{ ID.IgnoreTag, new Setting(strings[Str.IgnoreTag], "#Ignore") },
				{ ID.ManualTag, new Setting(strings[Str.ManualTag], "#Manual") },
				{ ID.AutoCloseDelayEntering, new Setting(strings[Str.AutoCloseDelayEntering], 0.5f, 1) },
				{ ID.AutoCloseDelayExiting, new Setting(strings[Str.AutoCloseDelayExiting], 2.0f) },
				{ ID.Timeout, new Setting(strings[Str.Timeout], 2f) },
				{ ID.OxygenDifference, new Setting(strings[Str.OxygenDifference], 20f) },
				{ ID.EnableRegularDoors, new Setting(strings[Str.EnableRegularDoors], true, 1) },
				{ ID.DefaultLampColor, new Setting(strings[Str.DefaultLampColor], Color.Green, 1) },
				{ ID.ChangingLampColor,  new Setting(strings[Str.ChangingLampColor], Color.Violet) }
			};

			//Removed: { ID.InAtmo, new Setting(strings[Str.InAtmo], false, 1) },
			//Removed: { ID.InAtmoDisableAltitude, new Setting(strings[Str.InAtmoDisableAltitude], 5000.0) },
			//Removed: { ID.ShowProblemsOnHud, new Setting(strings[Str.ShowProblemsOnHud], true, 1) }

			Settings.ParseSettings(Me.CustomData, settings);

			var text = new FixedWidthText(70);
			text.AppendLine(strings[Str.ScriptName] + " " + strings[Str.Settings]);
			text.AppendLine("----------------------------------------------------------------------");
			text.AppendLine(strings[Str.SettingsInstructions]);
			text.AppendLine(strings[Str.CommandsInstructions]);
			text.AppendLine("----------------------------------------------------------------------");
			text.AppendLine();

			//Me.CustomData = Settings.GenerateCustomData(settings, text);
			//Printing happens last in the init this function.

			Settings.GenerateCustomData(settings, text);

			text.AppendLine();
			text.AppendLine();
			text.AppendLine(strings[Str.SetupLog]);
			text.AppendLine("----------------------------------------------------------------------");
			
			//Some likely completly usless cashing
			hangartag = (string) settings[ID.HangarTag].Value;
			airlockTag = (string) settings[ID.AirlockTag].Value;
			hangartag = (string) settings[ID.HangarTag].Value;
			ignoreTag = (string) settings[ID.IgnoreTag].Value;
			//sensorTag = (string)settings[ID.SensorTag].Value;
			timeOpenExiting = (float) settings[ID.AutoCloseDelayExiting].Value;
			timeOpenEntering = (float) settings[ID.AutoCloseDelayEntering].Value;
			//inAtmo = (bool)settings[ID.InAtmo].Value;
			//inAtmoDisableAltitude = (double)settings[ID.InAtmoDisableAltitude].Value;

			#endregion



			#region Reset vars

			doorCount = 0;
			tinyAirlockCount = 0;
			smartAirlockCount = 0;
			groupAirlockCount = 0;
			hangarCount = 0;
			simpleGroupCount = 0;
			hangarAirSystemsCount = 0;
			groupAirlockAirSystemsCount = 0;

			ExtendedDoors.Clear();
			TinyAirlocks.Clear();
			AdvancedAirlocks.Clear();
			Hangars.Clear();
			SimpleGroupAirlocks.Clear();
			//AirSytems.Clear();
			StatusLCDs.Clear();

			var allBlocks = new List<IMyTerminalBlock>();
			var allDoors = new List<IMyDoor>();
			var allVents = new List<IMyAirVent>();
			//var allVentInvs = new List<VentInv>();
			var allGasGenerators = new List<IMyGasGenerator>();
			var allOxygenTanks = new List<IMyGasTank>();
			var allLCDs = new List<IMyTextPanel>();

			#endregion



			#region Get all blocks

			GridTerminalSystem.GetBlocks(allBlocks);

			for (int i = allBlocks.Count - 1; i >= 0; i--)
			{
				if (OverInstructionLimit()) yield return true;

				//Remove block conditions
				if (!allBlocks[i].IsSameConstructAs(Me))
				{
					allBlocks.RemoveAt(i);
					continue;
				}
				if (General.HasTag(ignoreTag, allBlocks[i].CustomName))
				{
					allBlocks.RemoveAt(i);
					continue;
				}


				if (allBlocks[i] is IMyDoor)
				{
					allDoors.Add(allBlocks[i] as IMyDoor);
				}
				else if(allBlocks[i] is IMyAirVent)
				{
					/*
					if (General.ContainsExact(sensorTag, allBlocks[i].CustomName))
					{
						sensorVent = allBlocks[i] as IMyAirVent;
					}
					else
					{
						allVents.Add(allBlocks[i] as IMyAirVent);
					}
					*/
					allVents.Add(allBlocks[i] as IMyAirVent);
				}
				else if (allBlocks[i] is IMyTextPanel)
				{
					allLCDs.Add(allBlocks[i] as IMyTextPanel);
				}
				/*
				else if (allBlocks[i] is IMyGasTank)
				{
					if(allBlocks[i].BlockDefinition.SubtypeId == "")
					{
						allOxygenTanks.Add(allBlocks[i] as IMyGasTank);
					}
				}
				else if (allBlocks[i] is IMyGasGenerator)
				{
					allGasGenerators.Add(allBlocks[i] as IMyGasGenerator);
				}
				*/
				/*
				else if (allBlocks[i] is IMyShipController && shipController == null)
				{
					shipController = allBlocks[i] as IMyShipController;
				}
				*/
			}

			#endregion


			
			//1. Find all grouped airlocks and remove doors from main list. Use RemoveFast. Order of doors isn't important.
			#region Setup Groups

			List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
			GridTerminalSystem.GetBlockGroups(groups);

			if(groups.Count > 0)
			{
				foreach (var group in groups)
				{
					if (OverInstructionLimit()) yield return true;

					bool hasHangarTag = false;

					var blocks = new List<IMyTerminalBlock>();


					var outer = new List<IMyDoor>();
					var inner = new List<IMyDoor>();
					var vents = new List<IMyAirVent>();
					var panels = new List<IMyTextPanel>();
					var soundBlocks = new List<IMySoundBlock>();
					var lights = new List<IMyLightingBlock>();
					var tanks = new List<IMyGasTank>();
					var generators = new List<IMyGasGenerator>();
					var oxygenFarms = new List<IMyFunctionalBlock>();
					var innerTimers = new List<IMyTimerBlock>();
					var outerTimers = new List<IMyTimerBlock>();

					group.GetBlocks(blocks);

					foreach (var block in blocks)
					{
						//Remove block conditions
						if (!block.IsSameConstructAs(Me))
						{
							continue;
						}
						if (General.HasTag(ignoreTag, block.CustomName))
						{
							continue;
						}

						if (block is IMyDoor)
						{
							if (General.HasTag(airlockTag, block.CustomName))
							{
								outer.Add(block as IMyDoor);
							}
							else if (General.HasTag(hangartag, block.CustomName))
							{
								outer.Add(block as IMyDoor);
								hasHangarTag = true;
							}
							else
							{
								inner.Add(block as IMyDoor);
							}
						}
						else if (block is IMyAirVent)
						{
							vents.Add(block as IMyAirVent);
						}
						else if (block is IMyTextPanel)
						{
							panels.Add(block as IMyTextPanel);
							allLCDs.Remove(block as IMyTextPanel);
						}
						else if (block is IMySoundBlock)
						{
							soundBlocks.Add(block as IMySoundBlock);
						}
						else if (block is IMyLightingBlock)
						{
							lights.Add(block as IMyLightingBlock);
						}
						else if (block is IMyGasTank)
						{
							if (block.BlockDefinition.SubtypeId == "")
							{
								tanks.Add(block as IMyGasTank);
							}
						}
						else if (block is IMyGasGenerator)
						{
							generators.Add(block as IMyGasGenerator);
						}
						else if (block is IMyOxygenFarm)
						{
							oxygenFarms.Add(block as IMyFunctionalBlock);
						}
						else if(block is IMyTimerBlock)
						{
							if (General.HasTag(airlockTag, block.CustomName) || General.HasTag(hangartag, block.CustomName))
							{
								outerTimers.Add(block as IMyTimerBlock);
							}
							else
							{
								innerTimers.Add(block as IMyTimerBlock);
							}
						}
					}


					if (vents.Count > 0)
					{
						//Has vents: Either Advanced or Hangar

						

						if (!hasHangarTag && inner.Count > 0 && outer.Count > 0)
						{
							var data = new AirlockComponents(this, ExtendDoors(outer), ExtendDoors(inner), ExtendVents(vents));
							if (tanks.Count > 0)
							{
								data.tanks = tanks;
								if (generators.Count > 0) data.generators = generators;
								if (oxygenFarms.Count > 0) data.farms = oxygenFarms;
								groupAirlockAirSystemsCount++;
							}
							if (lights.Count > 0)
							{
								data.lights = lights;
							}
							if (panels.Count > 0)
							{
								data.statusDisplay = new AirlockStatusDisplay(this, panels, strings[Str.GroupAirlock] + " " + AdvancedAirlocks.Count + 1);
								//AdvancedAirlocks.Count need +1 because we havn't added the airlock yet.
							}
							if(outerTimers.Count > 0)
							{
								data.outerTimers = outerTimers;
							}
							if (innerTimers.Count > 0)
							{
								data.innerTimers = innerTimers;
							}

							AdvancedAirlocks.Add(new AdvancedAirlock(data));
							groupAirlockCount++;
							//CheckForVentInv(allVentInvs, vents, data);
							text.AppendLine("\n> Advanced airlock " + AdvancedAirlocks.Count + " added");
							text.AppendLine("Type: Group airlock");
							text.AppendLine($"{outer.Count} Outer door(s): {string.Join(", ", outer.Select(r => r.CustomName))}");
							text.AppendLine($"{inner.Count} Inner door(s): {string.Join(", ", inner.Select(r => r.CustomName))}");
							text.AppendLine($"{vents.Count} Airvents: {string.Join(", ", vents.Select(r => r.CustomName))}");
							text.AppendLine($"{tanks.Count} Oxygen tanks: {string.Join(", ", tanks.Select(r => r.CustomName))}");
							text.AppendLine($"{generators.Count} Oxygen generators: {string.Join(", ", generators.Select(r => r.CustomName))}");
							text.AppendLine($"{oxygenFarms.Count} Oxygen farms: {string.Join(", ", oxygenFarms.Select(r => r.CustomName))}");
							text.AppendLine($"{panels.Count} LCDs: {string.Join(", ", panels.Select(r => r.CustomName))}");
							text.AppendLine($"{lights.Count} Lights: {string.Join(", ", lights.Select(r => r.CustomName))}");
							text.AppendLine($"{outerTimers.Count} Outer timers: {string.Join(", ", outerTimers.Select(r => r.CustomName))}");
							text.AppendLine($"{innerTimers.Count} Inner timers: {string.Join(", ", innerTimers.Select(r => r.CustomName))}");
						}
						else if (hasHangarTag && outer.Count > 0)
						{
							var data = new AirlockComponents(this, ExtendDoors(outer), ExtendDoors(inner), ExtendVents(vents));
							if (tanks.Count > 0)
							{
								data.tanks = tanks;
								if (generators.Count > 0) data.generators = generators;
								if (oxygenFarms.Count > 0) data.farms = oxygenFarms;
								hangarAirSystemsCount++;
							}
							if (lights.Count > 0)
							{
								data.lights = lights;
							}
							if (panels.Count > 0)
							{
								data.statusDisplay = new AirlockStatusDisplay(this, panels, strings[Str.Hangar] + " " + Hangars.Count + 1);
							}
							if (outerTimers.Count > 0)
							{
								data.outerTimers = outerTimers;
							}
							if (innerTimers.Count > 0)
							{
								data.innerTimers = innerTimers;
							}

							Hangars.Add(new Hangar(data));
							hangarCount++;
							//CheckForVentInv(allVentInvs, vents, data);
							text.AppendLine("\n> Hangar " + Hangars.Count + " added");
							text.AppendLine("Type: Hangar");
							text.AppendLine($"{outer.Count} Outer door(s): {string.Join(", ", outer.Select(r => r.CustomName))}");
							text.AppendLine($"{inner.Count} Inner door(s): {string.Join(", ", inner.Select(r => r.CustomName))}");
							text.AppendLine($"{vents.Count} Airvents: {string.Join(", ", vents.Select(r => r.CustomName))}");
							text.AppendLine($"{tanks.Count} Oxygen tanks: {string.Join(", ", tanks.Select(r => r.CustomName))}");
							text.AppendLine($"{generators.Count} Oxygen generators: {string.Join(", ", generators.Select(r => r.CustomName))}");
							text.AppendLine($"{oxygenFarms.Count} Oxygen farms: {string.Join(", ", oxygenFarms.Select(r => r.CustomName))}");
							text.AppendLine($"{panels.Count} LCDs: {string.Join(", ", panels.Select(r => r.CustomName))}");
							text.AppendLine($"{lights.Count} Lights: {string.Join(", ", lights.Select(r => r.CustomName))}");
							text.AppendLine($"{outerTimers.Count} Outer timers: {string.Join(", ", outerTimers.Select(r => r.CustomName))}");
							text.AppendLine($"{innerTimers.Count} Inner timers: {string.Join(", ", innerTimers.Select(r => r.CustomName))}");
						}
							
						//Remove doors and vents from main lists
						allDoors = allDoors.Except(outer).Except(inner).ToList();
						allVents = allVents.Except(vents).ToList();
						
					}
					else
					{
						if (outer.Count > 0) { 
							//Simple airlock
							bool includesHangarDoors = false;
							foreach (var door in outer)
							{
								if (door is IMyAirtightHangarDoor)
								{
									includesHangarDoors = true;
									break;
								}
							}
							//Simle group airlocks can't include hangardoors. Reason: If you have a group with just hangardoors then that might just be
							//a group of hangardoors. Not an airlock.
							//This could be changed by introducing inner and outer doors to the simple group airlock. Or maybe not. Might still interfere with non-airlocks.
							if (!includesHangarDoors)
							{
								SimpleGroupAirlocks.Add(new SimpleGroupAirlock(this, ExtendDoors(outer), ExtendDoors(inner)));
								simpleGroupCount++;
								text.AppendLine("\n> Simple Group airlock " + SimpleGroupAirlocks.Count + " added");
								text.AppendLine($"{outer.Count} doors");

								allDoors = allDoors.Except(outer).Except(inner).ToList();
								allVents = allVents.Except(vents).ToList();
							}
						}
					}
				}
			}
			#endregion

			

			//2. Find all tiny airlocks and remove doors from main list
			#region Setup Tiny airlocks

			for (int i = 0; i < allDoors.Count; i++)
			{
				if (OverInstructionLimit()) yield return true;

				for (int j = 0; j < allDoors.Count; j++)
				{
					//If doors adjacent (touching)
					if (Vector3I.DistanceManhattan(allDoors[i].Position, allDoors[j].Position) == 1)
					{
						//if doors on same Y level
						if (allDoors[i].Position.Y == allDoors[j].Position.Y)
						{
							if (allDoors[i].Orientation.Left == Base6Directions.GetFlippedDirection(allDoors[j].Orientation.Left))
							{
								TinyAirlocks.Add(new TinyAirlock(this, ExtendDoor(allDoors[i]), ExtendDoor(allDoors[j])));
								tinyAirlockCount++;
								text.AppendLine("\n> Tiny airlock " + TinyAirlocks.Count + " added");

								//Doors found, remove from this list to prevent them ending up as part of another airlock.
								allDoors.RemoveAtFast(j);
								allDoors.RemoveAtFast(i);
								i--;
								break;
							}
						}
					}
				}
			}
			#endregion

			

			//3. Find all smart airlocks and remove doors from main list
			#region Init Smart airlocks

			var outerDoors = new List<IMyDoor>(); //Doors with #AL tag
			var innerDoors = new List<IMyDoor>(); //Doorws witouth #AL tag

			for (int i = 0; i < allDoors.Count; i++)
			{
				if (OverInstructionLimit()) yield return true;

				if (General.HasTag(airlockTag, allDoors[i].CustomName))
				{
					outerDoors.Add(allDoors[i]);
					allDoors.RemoveAtFast(i);
					i--;
				}
				else
				{
					innerDoors.Add(allDoors[i]);
				}
				//if (OverInstructionLimit()) yield return true; //TODO: Reimplement this
			}
			

			if (outerDoors.Count > 0 && innerDoors.Count > 0)
			{
				for (int i = 0; i < outerDoors.Count; i++)
				{
					if (OverInstructionLimit()) yield return true;

					int innerDoorIndex = -1;
					int ventIndex = -1;
					float distance = float.MaxValue; //TODO: change back to int if DistanceManhattan works fine.
					float tempDist; //TODO: change back to int if DistanceManhattan works fine.
					for (int j = 0; j < innerDoors.Count; j++)
					{
						tempDist = Vector3I.DistanceManhattan(outerDoors[i].Position, innerDoors[j].Position);
						//tempDist = Vector3.Distance(outerDoors[i].Position, innerDoors[j].Position);
						if (tempDist > 0 && tempDist < distance)
						{
							distance = tempDist;
							innerDoorIndex = j;
						}
					}
					if (innerDoorIndex >= 0)
					{
						distance = float.MaxValue;
						for (int j = 0; j < allVents.Count; j++)
						{
							tempDist = Vector3I.DistanceManhattan(outerDoors[i].Position, allVents[j].Position);
							//tempDist = Vector3.Distance(outerDoors[i].door.Position, innerDoors[j].door.Position);
							if (tempDist < distance)
							{
								distance = tempDist;
								ventIndex = j;
							}
						}
						if(ventIndex >= 0)
						{
							var data = new AirlockComponents(this, ExtendDoor(outerDoors[i]), ExtendDoor(innerDoors[innerDoorIndex]), ExtendVent(allVents[ventIndex]));
							AdvancedAirlocks.Add(new AdvancedAirlock(data));
							smartAirlockCount++;
							text.AppendLine("\n> Advanced airlock " + AdvancedAirlocks.Count + " added");
							text.AppendLine("Type: Smart airlock");
							text.AppendLine("Outer door: " + outerDoors[i].CustomName);
							text.AppendLine("Inner door: " + innerDoors[innerDoorIndex].CustomName);
							text.AppendLine("Airvent: " + allVents[ventIndex].CustomName);
							//CheckForVentInv(allVentInvs, allVents[ventIndex], data);
							outerDoors.RemoveAtFast(i);
							i--;
							innerDoors.RemoveAtFast(innerDoorIndex);
							allVents.RemoveAtFast(ventIndex);
						}
					}
				}
			}
			allDoors = innerDoors;

			#endregion



			//4. Regular doors remaining.
			#region Regular doors

			if ((bool)settings[ID.EnableRegularDoors].Value)
			{
				//Remove regular doors tagged as Manual, as they would have no action.
				foreach (var item in allDoors)
				{
					if(!General.ContainsExact((string)settings[ID.ManualTag].Value, item.CustomName))
					{
						ExtendedDoors.Add(ExtendDoor(item));
					}
				}
				doorCount = ExtendedDoors.Count;
				text.AppendLine($"\n> {ExtendedDoors.Count} Regular doors added");
			}
			else
			{
				text.AppendLine($"\n> 0 Regular doors added");
			}

			#endregion



			//Setup status LCDs
			for (int i = 0; i < allLCDs.Count; i++)
			{
				//This list should contain all LCDs outside of groups.
				if(General.HasTag(airlockTag, allLCDs[i].CustomName))
				{
					StatusLCDs.Add(allLCDs[i]);
					allLCDs[i].ShowPublicTextOnScreen();
				}
			}



			//Setup air systems
			#region AirSystems

			//TODO.
			//Check all air vents. If any of them has an invnetory behind them, search the grid from there to create an air system.

			//Get all air systems connected to an air vent.
			//allVentsInvs should be filled with all air vents placed on a block with an inventory.
			//Meaning; all air systems are connected to at least one airlock airvent.
			/*
			//Shit's fucked. IsConnectedTo is incredible unreliable. Try the Test() method on a grid with several different converyor systems.
			Echo("VentInvs: " + allVentInvs.Count);
			while (allVentInvs.Count > 0)
			{
				var oxygenGens = new List<IMyGasGenerator>();
				var oxygenTanks = new List<IMyGasTank>();
				var ventInvs = new List<VentInv> { allVentInvs[allVentInvs.Count - 1] };
				allVentInvs.RemoveAt(allVentInvs.Count - 1);
				IMyInventory a = ventInvs[0].inventory.GetInventory();

				for (int i = 0; i < allOxygenTanks.Count; i++)
				{

					if (a.IsConnectedTo(allOxygenTanks[i].GetInventory()))
					{
						Echo(ventInvs[0].inventory.CustomName + " is connected to " + allOxygenTanks[i].CustomName + "\n");
						oxygenTanks.Add(allOxygenTanks[i]);
					}
				}

				for (int i = 0; i < allGasGenerators.Count; i++)
				{
					if (a.IsConnectedTo(allGasGenerators[i].GetInventory()))
					{
						Echo(ventInvs[0].inventory.CustomName + " is connected to " + allGasGenerators[i].CustomName + "\n");
						oxygenGens.Add(allGasGenerators[i]);
					}
				}

				
				for (int i = allVentInvs.Count - 1; i >= 0; i--)
				{
					if (ventInvs[0].inventory.IsConnectedTo(allVentInvs[i].inventory.GetInventory()))
					{
						ventInvs.Add(allVentInvs[i]);
						allVentInvs.RemoveAt(i);
					}
				}
				if (oxygenTanks.Count > 0)
				{
					//Don't need to check for ventInvs.Count > 0 as it's guaranteed to be >= 1.
					//Not checking generators either becasue vent systems can scoop air

					AirSytems.Add(new AirSystem(this, oxygenTanks, oxygenGens, ventInvs));
					Echo("Air system added");
				}
			}
			*/
			#endregion



			//Detaild info - the text area in the programmable block
			#region Detailed info

			Me.CustomData = text.GetText();

			FixedWidthText detailedInfo = new FixedWidthText(30);
			detailedInfo.Clear();
			//detailedInfo.AppendLine("Blarg's " + strings[Str.ScriptName] + DotDotDot());
			detailedInfo.AppendLine();
			detailedInfo.AppendLine(strings[Str.SeeCustomData]);
			detailedInfo.AppendLine();
			detailedInfo.AppendLine("Doors: " + doorCount);
			detailedInfo.AppendLine("Tiny airlocks: " + tinyAirlockCount);
			detailedInfo.AppendLine("Smart airlocks: " + smartAirlockCount);
			detailedInfo.AppendLine("Group airlocks: " + groupAirlockCount + $" ({groupAirlockAirSystemsCount})");
			detailedInfo.AppendLine("Hangars: " + hangarCount + $" ({hangarAirSystemsCount})");
			detailedInfo.AppendLine("Simple group airlocks: " + simpleGroupCount);
			//detailedInfo.AppendLine("Sensor vent: " + (sensorVent != null ? "Yes" : "No"));

			detailedInfoText = detailedInfo.GetText();

			#endregion

			Initialized = true;
			yield return true;
		}

		public void Save()
		{

		}

		//Everything is connected to everything according to this code.
		void Test()
		{
			var oxygenGens = new List<IMyGasGenerator>();
			GridTerminalSystem.GetBlocksOfType(oxygenGens);
			var oxygenTanks = new List<IMyGasTank>();
			GridTerminalSystem.GetBlocksOfType(oxygenTanks);

			var cargo = GridTerminalSystem.GetBlockWithName("Small Cargo Container");

			IMyInventory inv = cargo.GetInventory();
			
			foreach (var tank in oxygenTanks)
			{
				if (inv.IsConnectedTo(tank.GetInventory()))
				{
					Echo(cargo.CustomName + " is connected to " + tank.CustomName + "\n");
				}
			}

			foreach (var gen in oxygenGens)
			{
				if (inv.IsConnectedTo(gen.GetInventory()))
				{
					Echo(cargo.CustomName + " is connected to " + gen.CustomName + "\n");
				}
			}
		}

		void Test2()
		{
			var oxygenTanks = new List<IMyGasTank>();
			GridTerminalSystem.GetBlocksOfType(oxygenTanks);

			var cargo = GridTerminalSystem.GetBlockWithName("Small Cargo Container");

			IMyInventory inv = cargo.GetInventory();

			foreach (var tank in oxygenTanks)
			{
				if (inv.IsConnectedTo(tank.GetInventory()))
				{
					Echo(cargo.CustomName + " is connected to " + tank.CustomName + "\n");
				}
			}
		}

		/*
		int foo = 0;
		List<IMyAirVent> testVents;
		void VentTest()
		{
			switch (foo)
			{
				case 0:
					testVents = new List<IMyAirVent>();
					GridTerminalSystem.GetBlocksOfType(testVents);
					break;

				case 2:
					for (int i = 0; i < testVents.Count; i++)
					{
						testVents[i].GetOxygenLevel();
					}
					break;
				case 3:
					Echo("Runtime: " + Runtime.LastRunTimeMs);
					Echo("Per vent: " + Runtime.LastRunTimeMs / testVents.Count);
					foo = 0;
					break;
			}
			foo++;
		}
		*/
		public void Main(string argument, UpdateType updateType)
		{
			Time = Time + Runtime.TimeSinceLastRun;
			runCount++;

			if (!Initialized)
			{
				initCounter++;
				Echo("Initializing." + new string('.', initCounter));
				if (InitStateMachine != null)
				{
					if (!InitStateMachine.MoveNext())
					{
						InitStateMachine.Dispose();
						InitStateMachine = null;
					}
					else if (!InitStateMachine.Current)
					{
						//If it returns false, abort init.
						InitStateMachine.Dispose();
						InitStateMachine = null;
					}
				}
			}
			else
			{

				// If the update source is from a trigger or a terminal,
				// this is an interactive command.
				if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
				{
					if (Initialized)
					{
						switch (argument.ToLower().Trim())
						{
							case "update":
								initCounter = 0;
								Initialized = false;
								InitStateMachine = Init();
								break;
							case "atmo":
								SetAtmo(!inAtmo);
								break;
							case "atmo off":
								SetAtmo(false);
								break;
							case "atmo on":
								SetAtmo(true);
								break;
							default:
								timedMessages.Add(new TimedMessage(Time + TimeSpan.FromSeconds(7), "'" + argument + "' " + strings[Str.ArgumentNotUnderstood]));
								Me.CustomData += FixedWidthText.Adjust("\n> " + timedMessages.Last().message, 70);
								break;
						}
						//PrintCustomData(); //Moved to update 10, this isn't needed anymore. It will be snappy anyway.
					}
				}


				if ((updateType & UpdateType.Update10) != 0)
				{
					/*
					if (sensorVent != null)
					{
						outsideOxygenLevel = sensorVent.GetOxygenLevel();
						if((outsideOxygenLevel > oxygenLevelThreshold && lastOutsideOxygenLevel <= oxygenLevelThreshold) || (outsideOxygenLevel < oxygenLevelThreshold && lastOutsideOxygenLevel >= oxygenLevelThreshold))
						{
							outsideOxygenChange = true;
						}
						else
						{
							outsideOxygenChange = false;
						}
						lastOutsideOxygenLevel = outsideOxygenLevel;
					}
					*/
					if (shipController != null)
					{
						altitudeAccurate = shipController.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out altitude);
						if(inAtmo && altitudeAccurate && altitude > inAtmoDisableAltitude)
						{
							SetAtmo(false);
						}
					}

					foreach (var door in ExtendedDoors)
					{
						door.Update();
					}
					foreach (var airlock in TinyAirlocks)
					{
						airlock.Update();
					}
					foreach (var airlock in AdvancedAirlocks)
					{
						airlock.Update();
					}
					foreach (var hangar in Hangars)
					{
						hangar.Update();
					}
					foreach (var airlock in SimpleGroupAirlocks)
					{
						airlock.Update();
					}
					//foreach (var airSystem in AirSytems)
					//{
					//	airSystem.Update();
					//}

					if(runCount % 5 == 0)
					{
						PrintCustomData();
					}

					inAtmoChanged = false;
				}
				if ((updateType & UpdateType.Update100) != 0)
				{
					//PrintCustomData();
					UpdateStatusLCD();
				}
			}
			ExeTime.Update();
		}

		private void PrintCustomData()
		{
			finalDetailedInfoText = "Blarg's " + strings[Str.ScriptName] + DotDotDot();

			finalDetailedInfoText += "\nLoad (avg): " + General.ToPercent(ExeTime.GetAvrage(), Runtime.MaxInstructionCount) + ", " + ExeTime.GetAvrageTime().ToString("n2") + "ms";

			//finalDetailedInfoText += "\n\n" + FixedWidthText.Adjust("Atmosphere mode " + (inAtmo ? "enabled" : "disabled") + ". toggle with 'atmo' command.", 30);
			if (inAtmo) finalDetailedInfoText += "\n\nAtmosphere mode enabled";

			if (timedMessages.Count > 0)
			{
				for (int i = timedMessages.Count - 1; i >= 0; i--)
				{
					if (Time > timedMessages[i].expiration)
					{
						timedMessages.RemoveAt(i);
					}
					else
					{
						finalDetailedInfoText += "\n\n" + FixedWidthText.Adjust(timedMessages[i].message, 30);
					}
				}
			}

			finalDetailedInfoText += "\n" + detailedInfoText;

			Echo(finalDetailedInfoText);
			
			if (debugLCD != null)
			{
				debugLCD.WritePublicText(finalDetailedInfoText);
			}
			
		}

		private void UpdateStatusLCD()
		{
			if (StatusLCDs.Count == 0) return;

			string final = "Blarg's " + strings[Str.ScriptName];

			final += "\nLoad (avg): " + General.ToPercent(ExeTime.GetAvrage(), Runtime.MaxInstructionCount) + ", " + ExeTime.GetAvrageTime().ToString("n2") + "ms";

			if (timedMessages.Count > 0)
			{
				for (int i = timedMessages.Count - 1; i >= 0; i--)
				{
					if (Time > timedMessages[i].expiration)
					{
						timedMessages.RemoveAt(i);
					}
					else
					{
						final += "\n\n" + FixedWidthText.Adjust(timedMessages[i].message, 30);
					}
				}
			}

			final += "\n" + detailedInfoText;

			for (int i = 0; i < StatusLCDs.Count; i++)
			{
				StatusLCDs[i].WritePublicText(final);
			}
		}

		public bool OverInstructionLimit()
		{
			return Runtime.CurrentInstructionCount > MaxInstructionsPerTick;
		}

		public void SetAtmo(bool value)
		{
			if(value == true && altitudeAccurate && altitude > inAtmoDisableAltitude)
			{
				return; //Dont allow turning on atmo mode if above certain altitude.
			}
			if (inAtmo == value)
			{
				return; //Nothing changed
			}
			inAtmo = value;
			inAtmoChanged = true;
			//settings[ID.InAtmo].Value = inAtmo;
			//Me.CustomData = Settings.ChangeInCustomData(Me.CustomData, settings[ID.InAtmo].Text, settings[ID.InAtmo].Value);
		}

		//allVentInvs is the list we're adding to. Reference types a nice like that.
		/*
		private void CheckForVentInv(List<VentInv> allVentInvs, List<IMyAirVent> vents, AirlockComponents airlockComponents = null)
		{
			foreach (var vent in vents)
			{
				CheckForVentInv(allVentInvs, vent, airlockComponents);
			}
		}
		private void CheckForVentInv(List<VentInv> allVentInvs, IMyAirVent vent, AirlockComponents airlockComponents = null)
		{
			//Workaround becaue air vents lack inventory. Conveyors do as well.
			//Maybe I could crawl the grid though..
			//Crawl until an inventory is found.
			IMyTerminalBlock blockWithInventory;
			if (AirSystem.BlockBehindHasInvotory(vent, out blockWithInventory))
			{
				allVentInvs.Add(new VentInv(vent, blockWithInventory, airlockComponents));
			}
		}
		*/
		private ExtendedDoor ExtendDoor(IMyDoor door)
		{
			return new ExtendedDoor(this, door, true, timeOpenEntering, timeOpenExiting);
		}
		private List<ExtendedDoor> ExtendDoors(List<IMyDoor> doors)
		{
			var extendedDoors = new ExtendedDoor[doors.Count];
			for (int i = 0; i < doors.Count; i++)
			{
				extendedDoors[i] = ExtendDoor(doors[i]);
			}
			return extendedDoors.ToList();
		}

		private ExtendedAirvent ExtendVent(IMyAirVent vent)
		{
			return new ExtendedAirvent(this, vent);
		}
		private List<ExtendedAirvent> ExtendVents(List<IMyAirVent> vents)
		{
			var extendedVents = new ExtendedAirvent[vents.Count];
			for (int i = 0; i < vents.Count; i++)
			{
				extendedVents[i] = ExtendVent(vents[i]);
			}
			return extendedVents.ToList();
		}

		public string SerializeCustomNameList(List<IMyDoor> list)
		{
			string text = "";
			foreach (var item in list)
			{
				text += item.CustomName + ", ";
			}
			return text;
		}
		public string SerializeCustomNameList(List<IMyAirVent> list)
		{
			string text = "";
			foreach (var item in list)
			{
				text += item.CustomName + ", ";
			}
			return text;
		}
		public string SerializeCustomNameList(List<IMyTextPanel> list)
		{
			string text = "";
			foreach (var item in list)
			{
				text += item.CustomName + ", ";
			}
			return text;
		}
		

		//Divider reduces the rate the dots change. 5 means only every fifth call gets a different return.
		string[] dots = {".", "..", "..."};
		int dotIndex = -1;
		private string DotDotDot()
		{
			dotIndex++;

			if (dotIndex >= dots.Length)
			{
				dotIndex = 0;
			}
			return dots[dotIndex];
		}

		//A message that expires.
		class TimedMessage
		{
			public TimeSpan expiration;
			public string message;

			public TimedMessage(TimeSpan expiration, string message)
			{
				this.expiration = expiration;
				this.message = message;
			}
		}

		#endregion
		#region post-script
	}
}
#endregion