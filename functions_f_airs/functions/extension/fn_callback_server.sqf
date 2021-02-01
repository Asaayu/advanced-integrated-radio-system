params
[
	["_name", "", [""]],
	["_function", "", [""]],
	["_data", "", [""]]
];

switch (tolower _function) do
{
	case "airs_set_server_address":
	{
		_data params [["_address","",[""]]];
		airs_server_address = compileFinal str _address;
		publicVariable "airs_server_address";
	};
	default
	{
		[format["Unknown server callback function '%1'...", _function]] call AIRS_fnc_log;
	};
};
