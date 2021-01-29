params [["_device_name","",[""]]];

if (isNil "cba_settings_default") exitWith { ["Attempted to populate devices but 'cba_settings_default' was not defined"] call AIRS_fnc_log; };

private _settings_object = cba_settings_default;
private _settings_variable = "airs_input_device";
private _settings_data = _settings_object getVariable [_settings_variable, []];

if (isNil "cba_settings_default") exitWith { ["Attempted to populate devices but ther settings data array is empty"] call AIRS_fnc_log; };

[format["Device name: '%1'",_device_name], true] call AIRS_fnc_log;

if (_device_name == "") then
{
	// Empty the list
	_settings_data set [3, [[-1],["STR_AIRS_SETTINGS_NO_DEVICES_FOUND_TITLE"],["STR_AIRS_SETTINGS_NO_DEVICES_FOUND_TOOLTIP"]]];
}
else
{
	// Append device to settings list
	private _data = +_settings_data#3;
	(_data#0) pushBack parseNumber (_device_name select [0,1]);
	(_data#1) pushBack (_device_name select [1,999]);
	(_data#2) pushBack (_device_name select [1,999]);
	_settings_data set [3, _data];
};

true
