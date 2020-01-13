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
	#region untouched 
	/*
Blargmode's Aggressive Airlocks

Version: 6.1 (2019-03-30)

Probably the best airlock script on the workshop ;)

___/ Installation \\_________
If you're reading this you have probably installed it already. 
Press 'Ok' and you're done.


___/ The update command \\_________
Important for later. Sending the update command tells the script that you have made changes it should know about.
To do it, type 'update' in the 'Argument' text field in the programmable block, and press Run. This can also be done via for example a button.

 
___/ Tags \\_________
Tags are used to tell the script about what blocks are what. You enter them into the 'Custom Name' field of the block. It does not matter if 
it's first or last or somewhere in between. 
Currently there are 4 tags, default values below:
#AL		- Marks the outer door in Smart- and Group-airlocks.
#Hangar	- Marks the outer doors in Hangars.
#Ignore	- Marks any block that the script should, well, ignore. Useful for example if the Smart airlock isn't smart enough.
#Manual	- Disables auto-closing.


___/ Setup - Regular doors \\_________
Just send the update command to the script. It will find all new doors.


___/ Setup - Tiny airlocks \\_________
Place two doors back to back and send the update command.


___/ Setup - Smart airlocks \\_________
Build 2 doors and 1 air vent in close proximity, and tag the outer door with #AL. Then send the update command.
The script will start at the outer door and look for the closest air vent, as well as the closest untagged door. These three becomes
the airlock. 


___/ Setup - Group airlocks \\_________
Build 2 or more doors and 1 or more air vents. You can also include lights, LCDs, Oxygen Tanks, O2/H2 Generators, and Oxygen farms. Tag the outside doors (all of them)
with #AL and put everything into a group. The group name does not matter. Then send the update command.
The script will create one airlock from every valid group. Included Oxygen tanks, farms, and generators will be automatically managed.


___/ Setup - Simple Group airlocks \\_________
Build 2 or more doors and group them. Name doesn't matter. Tag 1+ doors with #AL, then send the update command.
The resulting airlock has no oxygen capabillities, but prevents tagged and untagged doors from being open simultaniously.
If you tag all doors in the group; it goes into solo mode: Only one door can be opened at a time.


___/ Setup - Hangars \\_________
Exactly the same as Group airlocks, but you use the #Hangar tag instead. The inner dooor isn't required.
You will need buttons both inside and outside of the hangar, as that is how you toggle it. Set up one of the air vents "Depressurize On/Off" 
action in the buttons and you're done.



___/ Additional stuff \\_________

Atmosphere mode:
	All the airlocks with Air vents can detect atmosphere when you head inside.
	Atmosphere mode stops depressurization, preventing oxygen tanks from overfilling.

Naming the group airlock and hangar:
	These airlocks can have LCDs, which will display a name like "Hangar 01". If you edit the Title of an LCD in the group and send the update command,
	all LCDs in the group will show your new name.

Settings: 
	There are a lot of settings in custom data of the programmable block.
	Send the update command for any changes to take effect.

Safety features:
	If an airlock detects it's stuck (de)pressurizing, it will abort and open the door.
	Otherwise you'd be stuck forever.

Hydrogen production:
	Hydrogen production interferes with the airlocks as it tends to fill upp oxygen tanks, preventing depressurization.
	I recommend having separate conveyor systems for your air system and hydrogen system.







































	Compressed code, touchy = breaky ¯\_(˘·˘)_/¯

*/

	#endregion
}
