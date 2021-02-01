params
[
	["_name", "", [""]],
	["_function", "", [""]],
	["_data", "", [""]]
];

switch (tolower _function) do
{
	case "airs_fnc_set_input_device":
	{
		_data call AIRS_fnc_set_input_device;
	};
	case "airs_fnc_set_output_device":
	{
		_data call AIRS_fnc_set_output_device;
	};
	case "airs_player_talking":
	{
		private _data = parseSimpleArray _data;
		["airs_player_talking", _data] call CBA_fnc_globalEvent;
	};
	case "airs_server_connect":
	{
		// Send request to server
		private _data = parseSimpleArray _data;
		(_data + [call CBA_fnc_currentUnit]) remoteExec ["AIRS_fnc_server_connect_client", 2];
	};
	default
	{
		[format["Unknown callback function '%1'...", _function]] call AIRS_fnc_log;
	};
};
