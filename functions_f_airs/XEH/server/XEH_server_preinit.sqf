if (("airs_server" callExtension "info") == "") exitWith
{
	diag_log "!!! Advanced Integrated Radio System (AIRS) server extension was not found. !!!";
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

"airs_server" callExtension "setup_server";

// Create the namespace which will contain the player object references
airs_player_namespace = true call CBA_fnc_createNamespace;
publicVariable "airs_player_namespace";

diag_log "server started";
