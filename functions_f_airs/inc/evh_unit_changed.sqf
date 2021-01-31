[
	"unit",
	{
		params [["_old_unit", objnull, [objNull]],["_new_unit", objnull, [objNull]]];

		if (isNull _new_unit) then
		{
			airs_player_namespace setVariable [getPlayerUID _old_unit, _old_unit, true];
		}
		else
		{
			airs_player_namespace setVariable [getPlayerUID _new_unit, _new_unit, true];

			// Reset random lip in case unit was talking
			_old_unit setRandomLip false;
		};
	},
	true
] call CBA_fnc_addPlayerEventHandler;
