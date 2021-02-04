params [["_steam_id", "", [""]],["_client_ip", "", [""]],["_object", objnull, [objnull]]];

if (_steam_id == "" || _client_ip == "" || isNull _object) exitWith {false};

"airs_server" callExtension format["add_client:%1",_client_ip];

remoteExec ["AIRS_fnc_client_connected", _object];
true
