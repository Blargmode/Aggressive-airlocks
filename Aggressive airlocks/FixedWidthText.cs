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
	public class FixedWidthText
	{
		//Basically word wrap. Won't split words.
		//W is the widest character. Custom data can fit 36 W, but for general text somewhere arund 70 is better
		//Detailed info can fit 18, but half of the last one is obscured by the scroll-bar;

		private List<string> Text;
		public int Width { get; private set; }

		//Constructor using custom width
		public FixedWidthText(int width)
		{
			Text = new List<string>();
			Width = width;
		}

		//Clears the text
		public void Clear()
		{
			Text.Clear();
		}

		//Adds text to the last line
		public void Append(string t)
		{
			Text[Text.Count - 1] += t;
		}
		//Adds an empty line
		public void AppendLine()
		{
			Text.Add("");
		}
		//Adds a line with text
		public void AppendLine(string t)
		{
			Text.Add(t);
		}
		public void Combine(List<string> input)
		{
			Text.AddRange(input);
		}
		public List<string> GetRaw()
		{
			return Text;
		}
		//Get the text with the width defined on create
		public string GetText()
		{
			return GetText(Width);
		}
		//Get the text with some other line width
		public string GetText(int lineWidth)
		{
			string finalText = "";
			foreach (var line in Text)
			{
				string rest = line;

				if (rest.Length > lineWidth)
				{
					while (rest.Length > lineWidth)
					{
						string part = rest.Substring(0, lineWidth);
						rest = rest.Substring(lineWidth);
						for (int i = part.Length - 1; i > 0; i--)
						{
							if (part[i] == ' ')
							{
								finalText += part.Substring(0, i) + "\n";
								rest = part.Substring(i + 1) + rest;
								break;
							}
						}
					}
				}
				finalText += rest + "\n";
			}
			return finalText;
		}
		//Standalone without creating a class.
		public static string Adjust(string text, int width)
		{
			string rest = text;
			string output = "";

			if (rest.Length > width)
			{
				while (rest.Length > width)
				{
					string part = rest.Substring(0, width);
					rest = rest.Substring(width);
					for (int i = part.Length - 1; i > 0; i--)
					{
						if (part[i] == ' ')
						{
							output += part.Substring(0, i) + "\n";
							rest = part.Substring(i + 1) + rest;
							break;
						}
					}
				}
			}
			output += rest;
			return output;
		}
	}
	#endregion
}
