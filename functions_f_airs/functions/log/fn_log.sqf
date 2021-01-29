params [["_message", "", [""]], ["_debug", false, [false]]];

diag_log _message;
if _debug then
{
	"airs_server" callExtension format["debug:%1",_message];
}
else
{
	"airs_server" callExtension format["log:%1",_message];
};
true
