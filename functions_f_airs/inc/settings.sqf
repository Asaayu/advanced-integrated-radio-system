// MAIN
[
	"airs_playback_volume",
	"SLIDER",
	["STR_AIRS_SETTINGS_OUTPUT_VOLUME_TITLE","STR_AIRS_SETTINGS_OUTPUT_VOLUME_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_MAIN_TITLE"],
	[0, 1, 1, 1],
	2,
	{
		params ["_value"];
		"airs_client" callExtension format["set_output_volume:%1",_value];
	}
] call CBA_fnc_addSetting;
[
	"airs_output_device",
	"LIST",
	["STR_AIRS_SETTINGS_OUTPUT_DEVICE_TITLE","STR_AIRS_SETTINGS_OUTPUT_DEVICE_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_MAIN_TITLE"],
	[[-1],["STR_AIRS_SETTINGS_INPUT_DEVICE_NONE"], 0],
	2,
	{},
	true
] call CBA_fnc_addSetting;
[
	"airs_notification_volume",
	"SLIDER",
	["STR_AIRS_SETTINGS_NOTIFICATION_VOLUME_TITLE","STR_AIRS_SETTINGS_NOTIFICATION_VOLUME_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_MAIN_TITLE"],
	[0, 1, 1, 1],
	2,
	{
		params ["_value"];
		"airs_client" callExtension format["set_notification_volume:%1",_value];
	}
] call CBA_fnc_addSetting;

// MIC
[
	"airs_input_device",
	"LIST",
	["STR_AIRS_SETTINGS_INPUT_DEVICE_TITLE","STR_AIRS_SETTINGS_INPUT_DEVICE_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_MICROPHONE_TITLE"],
	[[-1],["STR_AIRS_SETTINGS_INPUT_DEVICE_NONE"], 0],
	2,
	{},
	true
] call CBA_fnc_addSetting;
[
	"airs_mic_gain",
	"SLIDER",
	["STR_AIRS_SETTINGS_MIC_VOLUME_TITLE","STR_AIRS_SETTINGS_MIC_VOLUME_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_MICROPHONE_TITLE"],
	[0, 3, 1, 2],
	2,
	{
		params ["_value"];
		"airs_client" callExtension format["set_mic_gain:%1",_value];
	}
] call CBA_fnc_addSetting;
[
	"airs_local_playback",
	"CHECKBOX",
	["STR_AIRS_SETTINGS_LOCAL_PLAYBACK_TITLE","STR_AIRS_SETTINGS_LOCAL_PLAYBACK_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_MICROPHONE_TITLE"],
	false,
	2,
	{
		params ["_value"];
		"airs_client" callExtension format["set_local_playback:%1",[0,1] select _value];
	}
] call CBA_fnc_addSetting;

// VOICE
[
	"airs_transmission_mode",
	"LIST",
	["STR_AIRS_SETTINGS_TRANSMISSION_TITLE","STR_AIRS_SETTINGS_TRANSMISSION_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_VOICE_TITLE"],
	[[0, 1, 2], ["STR_AIRS_SETTINGS_TRANSMISSION_PTT", "STR_AIRS_SETTINGS_TRANSMISSION_VAD", "STR_AIRS_SETTINGS_TRANSMISSION_CT"], 0],
	2,
	{
		params ["_value"];
		"airs_client" callExtension format["set_voice_mode:%1",_value];
	}
] call CBA_fnc_addSetting;
[
	"airs_voice_gate",
	"SLIDER",
	["STR_AIRS_SETTINGS_VOLUME_GATE_TITLE","STR_AIRS_SETTINGS_VOLUME_GATE_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_VOICE_TITLE"],
	[0, 32767, 500, 0],
	2,
	{
		params ["_value"];
		"airs_client" callExtension format["set_volume_gate:%1",round _value];
	}
] call CBA_fnc_addSetting;
[
	"airs_audio_click",
	"CHECKBOX",
	["STR_AIRS_SETTINGS_AUDIO_CLICK_TITLE","STR_AIRS_SETTINGS_AUDIO_CLICK_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_VOICE_TITLE"],
	false,
	2,
	{
		params ["_value"];
		"airs_client" callExtension format["set_audio_click:%1",[0,1] select _value];
	}
] call CBA_fnc_addSetting;


// MISC
[
	"airs_randomlip",
	"CHECKBOX",
	["STR_AIRS_SETTINGS_RANDOMLIP_TITLE","STR_AIRS_SETTINGS_RANDOMLIP_TOOLTIP"],
	["STR_AIRS_MOD_TITLE", "STR_AIRS_SETTINGS_OTHER_TITLE"],
	true,
	2,
	{
		params ["_value"];
		"airs_client" callExtension format["set_random_lip:%1",[0,1] select _value];
	}
] call CBA_fnc_addSetting;
