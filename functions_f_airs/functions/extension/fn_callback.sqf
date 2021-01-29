params
[
	["_name", "", [""]],
	["_function", "", [""]],
	["_data", "", [""]]
];

switch (tolower _function) do
{
	case "airs_fnc_populate_devices":
	{
		_data call AIRS_fnc_populate_devices;
	};
	default
	{
		[format["Unknown callback function '%1'...", _function]] call AIRS_fnc_log;
	};
};
