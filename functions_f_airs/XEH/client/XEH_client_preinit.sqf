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

addMissionEventHandler ["ExtensionCallback",
{
	params [["_name","",[""]], ["_function","",[""]], ["_data","",[""]]];

	if (_name == "AIRS_VOIP") then
	{
		_this call AIRS_fnc_callback;
	};
}];

"airs_client" callExtension "preinit";
