params [["_device_name","",[""]]];

if (isNil "cba_settings_default") exitWith { ["Attempted to populate devices but 'cba_settings_default' was not defined"] call AIRS_fnc_log; };

private _settings_object = cba_settings_default;
private _settings_variable = "airs_input_device";
private _settings_data = _settings_object getVariable [_settings_variable, []];

if (isNil "cba_settings_default") exitWith { ["Attempted to populate devices but ther settings data array is empty"] call AIRS_fnc_log; };

_settings_data set [3, [[-1],[_device_name],[_device_name]]];

true
