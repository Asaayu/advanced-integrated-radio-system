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

// Currently the 3DEN editor is not supported
if is3DEN exitWith { "airs_client" callExtension "log:User opened 3den mission, client 'XEH_server_preinit' executed halted."; };

diag_log "server started";
