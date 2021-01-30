// Push To talk
[
	"STR_AIRS_MOD_TITLE",
	"airs_push_to_talk",
	"STR_AIRS_KEYBINDS_PUSH_TO_TALK_TITLE",
	{
		"airs_client" callExtension format["set_ptt:%1", 1];
	},
	{
		"airs_client" callExtension format["set_ptt:%1", 0];
	},
	[DIK_CAPSLOCK, [false, false, false]]
] call CBA_fnc_addKeybind;
[
	"STR_AIRS_MOD_TITLE",
	"airs_push_to_talk_radio",
	"STR_AIRS_KEYBINDS_PUSH_TO_TALK_RADIO_TITLE",
	{
		"airs_client" callExtension format["set_ptt_radio:%1", 1];
	},
	{
		"airs_client" callExtension format["set_ptt_radio:%1", 0];
	},
	[DIK_CAPSLOCK, [false, true, false]]
] call CBA_fnc_addKeybind;
[
	"STR_AIRS_MOD_TITLE",
	"airs_push_to_talk_global",
	"STR_AIRS_KEYBINDS_PUSH_TO_TALK_GLOBAL_TITLE",
	{
		"airs_client" callExtension format["set_ptt_global:%1", 1];
	},
	{
		"airs_client" callExtension format["set_ptt_global:%1", 0];
	},
	[DIK_CAPSLOCK, [false, false, true]]
] call CBA_fnc_addKeybind;

// Mute microphone (Toggle)
[
	"STR_AIRS_MOD_TITLE",
	"airs_mute_microphone",
	"STR_AIRS_KEYBINDS_MUTE_MICROPHONE_TOGGLE_TITLE",
	{
		"airs_client" callExtension "toggle_microphone";
	},
	{},
	[DIK_NUMPADSLASH, [false, false, false]]
] call CBA_fnc_addKeybind;

// Mute speaker (Toggle)
[
	"STR_AIRS_MOD_TITLE",
	"airs_mute_speaker",
	"STR_AIRS_KEYBINDS_MUTE_SPEAKER_TOGGLE_TITLE",
	{
		"airs_client" callExtension "toggle_speakers";
	},
	{},
	[DIK_MULTIPLY, [false, false, false]]
] call CBA_fnc_addKeybind;
