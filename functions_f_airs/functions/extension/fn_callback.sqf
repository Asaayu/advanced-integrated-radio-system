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
	case "airs_talking":
	{
		private _data = parseSimpleArray _data;
		["airs_talking", _data] call CBA_fnc_globalEvent;
	};
	default
	{
		[format["Unknown callback function '%1'...", _function]] call AIRS_fnc_log;
	};
};
