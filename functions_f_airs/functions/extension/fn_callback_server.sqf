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
		diag_log _data;
		private _data = _data splitString "|";
		_data params [["_address","",[""]],["_port","",[""]]];
		airs_server_address = compileFinal str _address;
		airs_server_port = compileFinal str _port;
		publicVariable "airs_server_address";
		publicVariable "airs_server_port";
	};
	default
	{
		[format["Unknown server callback function '%1'...", _function]] call AIRS_fnc_log;
	};
};
