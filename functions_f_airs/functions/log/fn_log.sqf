params [["_message", "", [""]], ["_debug", false, [false]]];

diag_log _message;
if _debug then
{
	"airs_client" callExtension format["debug:%1",_message];
}
else
{
	"airs_client" callExtension format["log:%1",_message];
};
true
