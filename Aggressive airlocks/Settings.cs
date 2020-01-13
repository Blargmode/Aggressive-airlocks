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
	//Setting id
	public enum ID
	{
		AutoCloseDelayExiting,
		AutoCloseDelayEntering,
		AutoCloseDelayRegularDoors,
		Timeout,
		AirlockTag,
		HangarTag,
		SensorTag,
		IgnoreTag,
		ManualTag,
		EnableRegularDoors,
		EnableTinyAirlocks,
		EnableSmartAirlocks,
		EnableGroupAirlocks,
		DefaultLampColor,
		ChangingLampColor,
		ShowProblemsOnHud,
		OxygenDifference,
		InAtmo,
		InAtmoDisableAltitude
	}

	public enum Str
	{
		ScriptName,
		AutoCloseDelayExiting,
		AutoCloseDelayEntering,
		AutoCloseDelayRegularDoors,
		Timeout,
		AirlockTag,
		HangarTag,
		SensorTag,
		IgnoreTag,
		ManualTag,
		EnableRegularDoors,
		EnableTinyAirlocks,
		EnableSmartAirlocks,
		EnableGroupAirlocks,
		DefaultLampColor,
		ChangingLampColor,
		ShowProblemsOnHud,
		Settings,
		SettingsInstructions,
		CommandsInstructions,
		OxygenDifference,
		SmartAirlock,
		GroupAirlock,
		Hangar,
		Error,
		SetupLog,
		SeeCustomData,
		ArgumentNotUnderstood,
		InAtmo,
		InAtmoDisableAltitude
	}

	class Settings
	{
		public static void ParseSettings(string data, Dictionary<ID, Setting> settings)
		{
			if (data.Length == 0) return;

			var lines = data.Split('\n');
			for (int i = 0; i < lines.Length; i++)
			{
				var keys = new List<ID>(settings.Keys);
				foreach(var key in keys)
				{
					if (lines[i].StartsWith(settings[key].Text)){
						var parts = lines[i].Split(new char[] { ':' }, 2);
						if (!string.IsNullOrEmpty(parts[1]))
						{
							var val = parts[1].Trim();

							if (settings[key].Value is bool)
							{
								if (val.ToLower() == "yes")
								{
									//If the value is yes, it's a true bool
									settings[key].Value = true;
								}
								else if (val.ToLower() == "no")
								{
									//If the value is no, it's a false bool
									settings[key].Value = false;
								}
								/*
								else
								{
									SettingsProblemIllegible(key, Strings.HasToBeBool);
								}
								*/
							}
							else if (settings[key].Value is int)
							{
								int temp = 0;
								if (int.TryParse(val, out temp))
								{
									settings[key].Value = temp;
								}
								/*
								else
								{
									SettingsProblemIllegible(key, Strings.HasToBeNumber);
								}
								*/
							}
							else if (settings[key].Value is float)
							{
								float temp = 0;
								if (float.TryParse(val, out temp))
								{
									settings[key].Value = temp;
								}
								/*
								else
								{
									SettingsProblemIllegible(key, Strings.HasToBeNumber);
								}
								*/
							}
							else if (settings[key].Value is double)
							{
								double temp = 0;
								if (double.TryParse(val, out temp))
								{
									settings[key].Value = temp;
								}
								/*
								else
								{
									SettingsProblemIllegible(key, Strings.HasToBeNumber);
								}
								*/
							}
							else if (settings[key].Value is string)
							{
								if (!string.IsNullOrEmpty(val) && !string.IsNullOrWhiteSpace(val))
								{
									settings[key].Value = val;
								}
								/*
								else
								{
									SettingsProblemIllegible(key, Strings.HasToBeString);
								}
								*/
							}
							else if (settings[key].Value is Color)
							{
								if (!string.IsNullOrEmpty(val) && !string.IsNullOrWhiteSpace(val))
								{
									val = val.ToLower();
									if (val.Contains("r:") && val.Contains("g:") && val.Contains("b:"))
									{
										var split = val.Split(',');
										if (split.Length == 3)
										{
											for (int j = 0; j < split.Length; j++)
											{
												split[j] = System.Text.RegularExpressions.Regex.Replace(split[j], "[^0-9.]", "");
											}
											Color color;
											if (General.TryParseRGB(split, out color))
											{
												settings[key].Value = color;
											}
											/*
											else
											{
												SettingsProblemIllegible(key, Strings.HasToBeColor);
											}
											*/
										}
										/*
										else
										{
											SettingsProblemIllegible(key, Strings.HasToBeColor);
										}
										*/
									}
									/*
									else
									{
										SettingsProblemIllegible(key, Strings.HasToBeColor);
									}
									*/
								}
								/*
								else
								{
									SettingsProblemIllegible(key, Strings.HasToBeColor);
								}
								*/
							}
						}
					}
				}
			}
		}

		public static string GenerateCustomData(Dictionary<ID, Setting> settings, FixedWidthText text)
		{
			foreach (var setting in settings.Values)
			{
				for (int i = 0; i < setting.SpaceAbove; i++)
				{
					text.AppendLine();
				}
				text.AppendLine(setting.Text + ": " + SettingToString(setting.Value));
			}
			return text.GetText();
		}

		private static string SettingToString(object input)
		{
			if (input is bool)
			{
				return (bool)input ? "yes" : "no";
			}
			if (input is Color)
			{
				var color = (Color)input;
				return "R:" + color.R + ", G:" + color.G + ", B:" + color.B;
			}

			return input.ToString();
		}

		//Change a specific setting
		public static string ChangeInCustomData(string data, string settingText, object newValue)
		{
			var lines = data.Split('\n');

			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].StartsWith(settingText))
				{
					var parts = lines[i].Split(new char[] { ':' }, 2);
					if (!string.IsNullOrEmpty(parts[1]))
					{
						parts[1] = " " + SettingToString(newValue);
						lines[i] = parts[0] + ":" + parts[1];
						return string.Join("\n", lines);
					}
				}
			}
			return data;
		}
	}

	public class Setting
	{
		public string Text;
		public object Value { get; set; }
		public int SpaceAbove;

		public Setting(string text, object value, int spaceAbove = 0)
		{
			Text = text;
			Value = value;
			SpaceAbove = spaceAbove; //Stylistic for the print
		}
	}
	#endregion
}
