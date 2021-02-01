[
	"featureCamera",
	{
		params [["_unit", objnull, [objNull]], ["_mode", "", [""]]];
		switch _mode do
		{
			case "curator":
			{
				// Set the unit's object to the zeus camera object
				airs_player_namespace setVariable [getPlayerUID _unit, curatorCamera, true];
			};
			case "":
			{
				// Set the unit's object back to the unit object, can be either the original unit or remote control unit
				airs_player_namespace setVariable [getPlayerUID _unit, _unit, true];
			};
		};
	},
	true
] call CBA_fnc_addPlayerEventHandler;
