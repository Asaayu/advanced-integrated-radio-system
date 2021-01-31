[
	"airs_player_talking",
	{
		params [["_unit_var","",[""]],["_type",0,[0]],["_active",false,[false]]];

		// Check if the player has random lip enabled in the settings
		if !airs_random_lip exitWith {false};
		if (_unit_var == "") exitWith {false};

		private _unit = airs_player_namespace getVariable [_unit_var, objNull];
		if (isNull _unit) exitWith {false};
		if (side _unit == sideLogic) exitWith {false};
		if !(_unit isKindOf "Man") exitWith {false};

		_unit setRandomLip _active;
	}
] call CBA_fnc_addEventHandler;
