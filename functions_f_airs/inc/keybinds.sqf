// Push To talk
[
	localize "STR_AIRS_MOD_TITLE",
	"airs_ptt",
	localize "STR_AIRS_SETTINGS_ENABLE_TITLE",
	{
		systemChat "PTT down";
	},
	{
		systemChat "PTT up";
	},
	[DIK_CAPSLOCK, [false, false, false]]
] call CBA_fnc_addKeybind;
