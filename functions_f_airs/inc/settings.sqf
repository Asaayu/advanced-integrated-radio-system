// Mod toggle
[
	"airs_master_toggle",
	"CHECKBOX",
	["STR_AIRS_SETTINGS_ENABLE_TITLE","STR_AIRS_SETTINGS_ENABLE_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_MAIN_TITLE"],
	true,
	2,
	{
		params ["_value"];
		systemChat str _value;
	}
] call CBA_fnc_addSetting;
