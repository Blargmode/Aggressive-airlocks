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

	enum PanelType
	{
		Corner,
		Text,
		Wide,
		Normal
	}
	
	class AirlockStatusDisplay
	{
		Program P;
		IMyTextPanel[] panels;
		PanelType[] types;
		//double totalTimeout; //Idea: if timeout is > 0 and > totalTimeout, set totalTimeout to that. If timeout < 0 reset totalTimeout to 0. 
		//This way we could have 5/10 seconds instead of just 5 seconds, without sending the actual full time each update.
		//TLDR: total timeout = highest value recieved since it was below 0 last time.

		public string airlockName = "";
		private string airlockType;

		public AirlockStatusDisplay(Program p, IMyTextPanel panel, string airlockType)
		{
			P = p;
			panels = new IMyTextPanel[] { panel };
			this.airlockType = airlockType;
			GetPanelType();
		}

		public AirlockStatusDisplay(Program p, List<IMyTextPanel> panels, string airlockType)
		{
			P = p;
			this.panels = panels.ToArray();
			this.airlockType = airlockType;
			GetPanelType();
		}

		public void GetPanelType()
		{
			types = new PanelType[panels.Length];
			for (int i = 0; i < panels.Length; i++)
			{
				//Get name
				if (panels[i].GetPublicTitle().Length > 0)
				{
					airlockName = panels[i].GetPublicTitle();
				}

				//Get type
				//Type isn't used anymore (except for font size during init). Don't have more data to show than what fits on the corner display.
				
				//if (panels[i].BlockDefinition.SubtypeId.Contains("Wide"))
				//{
				//	types[i] = PanelType.Wide;
				//}
				//else if (panels[i].BlockDefinition.SubtypeId.Contains("Text"))
				//{
				//	types[i] = PanelType.Text;
				//}
				//else
				if (panels[i].BlockDefinition.SubtypeId.Contains("Corner"))
				{
					//Set font size based on panel type. The angled ones and the flat ones doesn't match perfectly.
					if(panels[i].BlockDefinition.SubtypeId.Contains("Flat"))
					{
						if (panels[i].FontSize == 1f)
						{
							panels[i].FontSize = 1.4f;
						}
					}
					else
					{
						if (panels[i].FontSize == 1f)
						{
							panels[i].FontSize = 1.3f;
						}
					}
					types[i] = PanelType.Corner;
				}
				else
				{
					types[i] = PanelType.Normal;
				}
			}
		}

		public void Update(string ventState, bool error = false)
		{
			string text = "";
			for (int i = 0; i < panels.Length; i++)
			{
				//Only update text if not generated for this update.
				if(text.Length == 0)
				{
					if (error)
					{
						text += " <<< " + P.strings[Str.Error] + " >>>";
					}
					if(airlockName.Length < 1)
					{
						text += " " + airlockType + " \n";
					}
					else
					{
						text += " " + airlockName + " \n";
					}
					text += " " + ventState + " ";
				}
				panels[i].ShowPublicTextOnScreen();
				panels[i].WritePublicText(text);
			}
		}
	}
	#endregion
}
