// Main Settings
// Master mod toggle
[
	"airs_master",
	"CHECKBOX",
	["STR_AIRS_SETTINGS_ENABLE_TITLE","STR_AIRS_SETTINGS_ENABLE_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_MAIN_TITLE"],
	true,
	2,
	{
		params ["_value"];
		profileNamespace setVariable ["airs_master",_value];
		saveProfileNamespace;
	}
] call CBA_fnc_addSetting;

// Transmission mode
[
	"airs_transmission_mode",
	"LIST",
	["STR_AIRS_SETTINGS_TRANSMISSION_TITLE","STR_AIRS_SETTINGS_TRANSMISSION_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_VOICE_TITLE"],
	[[0, 1, 2], ["STR_AIRS_SETTINGS_TRANSMISSION_PTT", "STR_AIRS_SETTINGS_TRANSMISSION_VAD","STR_AIRS_SETTINGS_TRANSMISSION_CT"], 0],
	2,
	{
		params ["_value"];
		profileNamespace setVariable ["airs_transmission_mode",_value];
	}
] call CBA_fnc_addSetting;
