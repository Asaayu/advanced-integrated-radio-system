// This function is run once per game start

// Run test function once to make sure the extension is loaded correctly and avaliable
if (("airs_client" callExtension "info") == "") exitWith
{
	diag_log "!!! Advanced Integrated Radio System (AIRS) extension was not found. !!!";
};

"airs_client" callExtension "setup";
