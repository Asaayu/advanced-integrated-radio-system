// DIK key codes
#include "\a3\editor_f\Data\Scripts\dikCodes.h"

// CBA settings
#include "..\..\inc\settings.sqf"
#include "..\..\inc\keybinds.sqf"

// This function is run at the start of every mission

// Run test function once to make sure the extension is loaded correctly and avaliable
if (("airs_client" callExtension "info") == "") exitWith
{
	diag_log "!!! An Advanced Integrated Radio System (AIRS) client extension was not found. !!!";
	if hasInterface then
	{
		[] spawn
		{
			waituntil {!isNull (findDisplay 46)};
			uisleep 1;
			[localize "STR_AIRS_LOADING_ERROR_EXTENSION_DESCRIPTION", localize "STR_AIRS_LOADING_ERROR_EXTENSION_TITLE", localize "STR_DISP_CONTINUE", false, (findDisplay 46)] call BIS_fnc_guiMessage;
		};
	};
};

// Update devices in options menu
"airs_client" callExtension "update_devices";

// Currently the 3DEN editor is not supported
if is3DEN exitWith { "airs_client" callExtension "log:User opened 3den mission, client 'XEH_client_postinit' executed halted."; };
if !isMultiplayer exitWith { "airs_client" callExtension "log:User opened singleplayer mission, client 'XEH_client_postinit' executed halted."; };
if !hasInterface exitWith { "airs_client" callExtension "log:User does not have an interface, client 'XEH_client_postinit' executed halted."; };

addMissionEventHandler ["ExtensionCallback",
{
	params [["_name","",[""]], ["_function","",[""]], ["_data","",[""]]];

	if (_name == "AIRS_VOIP") then
	{
		_this call AIRS_fnc_callback;
	}
	else
	{
		if (_name == "AIRS_VOIP_SERVER" && {isServer}) then
		{
			_this call AIRS_fnc_callback_server;
		}
	};
}];

call AIRS_fnc_disable_channels;

// Eventhandlers
#include "..\..\inc\evh_player_talking.sqf"
#include "..\..\inc\evh_unit_changed.sqf"
#include "..\..\inc\evh_feature_camera.sqf"

[
	{getClientStateNumber == 10},
	{
		// Connect to server
		"airs_client" callExtension format["connect:%1", call airs_server_address];

		// Disable channels again in case mission changed the permissions
		call AIRS_fnc_disable_channels;

		// Setup disconnecting from server
		["Unload", { "airs_client" callExtension "disconnect"; }] call CBA_fnc_addDisplayHandler;
	},
	nil,
	-1
] call CBA_fnc_waitUntilAndExecute;
