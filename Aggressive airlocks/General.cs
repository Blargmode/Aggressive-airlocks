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
	class General
	{
		//Check if the 'inText' has the 'find' text. Accepts several inText. E.g. for checking both CustomName and CustomData.
		//Case insensitive since addition of .ToLower().
		public static bool HasTag(string find, params string[] inText)
		{
			foreach (string text in inText)
			{
				if (ContainsExact(find.ToLower(), text.ToLower())) return true;
			}
			return false;
		}
		//Matches exact. Eg. if match is "Blarg" and text is "Blargmode" it will return false, while string.Contains returns true. But if the text is "Blarg mode" it will return true.
		public static bool ContainsExact(string match, string text)
		{
			return System.Text.RegularExpressions.Regex.IsMatch(text, @"(^|\s)" + match + @"(\s|$)");
		}
		//Converts a float to string in percentage, with optional amount of decimals and a % sign added
		public static string ToPercent(double part, double whole, int decimals = 1)
		{
			double result = (part / whole) * 100;
			return result.ToString("n" + decimals) + "%";
		}
		public static bool TryParseRGB(string[] input, out Color color)
		{
			color = Color.Black;
			int r, g, b;
			if (!Int32.TryParse(input[0], out r)) return false;
			if (!Int32.TryParse(input[1], out g)) return false;
			if (!Int32.TryParse(input[2], out b)) return false;
			color = new Color(r, g, b);
			return true;
		}
	}
	#endregion
}
