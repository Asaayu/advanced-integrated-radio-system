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

if is3DEN exitWith { "airs_server" callExtension "log:Server opened 3den mission, client 'XEH_server_preinit' executed halted."; };
if !isMultiplayer exitWith { "airs_server" callExtension "log:Server opened singleplayer mission, client 'XEH_server_preinit' executed halted."; };
if !isServer exitWith { "airs_server" callExtension "log:Client is not server, client 'XEH_server_preinit' executed halted."; };

// Create the namespace which will contain the player object references
airs_player_namespace = true call CBA_fnc_createNamespace;
publicVariable "airs_player_namespace";

"airs_server" callExtension "mission:1";

// Setup disconnecting from server
["Unload", { "airs_server" callExtension "mission:0"; }] call CBA_fnc_addDisplayHandler;
