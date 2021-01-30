// This function is run once per game start

// Run test function once to make sure the extension is loaded correctly and avaliable
if (("airs_client" callExtension "info") == "") exitWith
{
	diag_log "!!! Advanced Integrated Radio System (AIRS) extension was not found. !!!";
};

"airs_client" callExtension "setup";

private _audio_classes = configProperties [configFile >> "airs_audio", "true", false];
{
	private _name = configName _x;
	private _filepath = getText (_x);
	"airs_client" callExtension format["set_audio_position:%1:%2",_name,_filepath];
} foreach _audio_classes;
